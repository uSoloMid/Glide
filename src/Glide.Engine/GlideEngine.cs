using Glide.Common;
using Glide.Graphics;
using Glide.Settings;

namespace Glide.Engine;

/// <summary>
/// Central coordinator: receives input intents, drives the zoom animator and
/// manages one render session per zoomed monitor.
/// Input callbacks arrive on the hook thread; frames on render threads.
/// </summary>
public sealed class GlideEngine : IDisposable
{
    private readonly object _lock = new();
    private readonly ZoomAnimator _animator = new();
    private readonly ForegroundProcessProvider _foreground = new();
    private readonly Dictionary<string, RenderSession> _sessions = new(StringComparer.OrdinalIgnoreCase);

    private GlideSettings _settings;
    private IReadOnlyList<MonitorInfo> _monitors = [];
    private double _panX;
    private double _panY;
    private bool _panning;
    private int _lastPanX;
    private int _lastPanY;
    private volatile bool _zoomActive;
    private volatile bool _enabled = true;

    public GlideEngine(GlideSettings settings)
    {
        _settings = settings;
        ApplySettings(settings);
    }

    /// <summary>Fast, lock-free check used by the input hook thread.</summary>
    public bool IsZoomActive => _zoomActive;

    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            if (!value)
                ResetZoom(animated: false);
        }
    }

    public void ApplySettings(GlideSettings settings)
    {
        lock (_lock)
        {
            _settings = settings;
            // The follow smoothing derives from the configured animation time.
            _animator.SmoothingTau = settings.AnimationDurationMs / 1000.0 * 0.35;
            if (_animator.Target > settings.MaxZoom)
                _animator.SetTarget(settings.MaxZoom);
        }
    }

    /// <summary>Stops all sessions and resets state (tray "Restart Engine").</summary>
    public void Restart()
    {
        ResetZoom(animated: false);
        Log.Info("Engine restarted");
    }

    // ------------------------------------------------------------------
    // Input intents (hook thread)
    // ------------------------------------------------------------------

    /// <summary>Modifier + wheel. Returns true when the event must be swallowed.</summary>
    public bool HandleZoomTick(double ticks, int x, int y)
    {
        if (!_enabled) return false;
        GlideSettings s;
        lock (_lock)
        {
            s = _settings;
            if (!s.Enabled) return false;

            bool fresh = _sessions.Count == 0;
            if (fresh && ExclusionChecker.IsExcluded(
                    _foreground.GetForegroundProcessName(), s.ExcludedApps))
                return false;

            double factor = Math.Pow(1.0 + 0.12 * s.ZoomSpeed, ticks * s.ScrollSensitivity);
            double target = Math.Clamp(_animator.Target * factor, s.MinZoom, s.MaxZoom);
            _animator.SetTarget(target);

            if (target > 1.0)
                EnsureSessionsLocked(x, y, s);
        }
        return true;
    }

    public void HandleModifierReleased()
    {
        lock (_lock)
        {
            if (_settings.Mode != ZoomMode.Temporary || !_animator.IsZoomed)
                return;
            BeginReturnLocked();
        }
    }

    public void HandleDoubleTap() => ResetZoom(animated: true);

    public bool HandlePanButton(bool down, int x, int y)
    {
        if (!_zoomActive) return false;
        lock (_lock)
        {
            if (!_settings.PanWithMiddleButton) return false;
            _panning = down;
            _lastPanX = x;
            _lastPanY = y;
        }
        return true;
    }

    public void HandleMouseMove(int x, int y)
    {
        if (!_zoomActive) return;
        lock (_lock)
        {
            if (_panning)
            {
                // Dragged content sticks to the cursor (Δpan = -Δcursor).
                _panX -= (x - _lastPanX) * _settings.PanSpeed;
                _panY -= (y - _lastPanY) * _settings.PanSpeed;
                _lastPanX = x;
                _lastPanY = y;
                return;
            }

            if (_settings.MonitorMode == MonitorZoomMode.CursorMonitor)
                SwitchMonitorIfNeededLocked(x, y);
        }
    }

    public void ResetZoom(bool animated)
    {
        lock (_lock)
        {
            if (!_animator.IsZoomed && _sessions.Count == 0)
                return;
            if (animated && _settings.AnimateReturn)
            {
                BeginReturnLocked();
            }
            else
            {
                _animator.ResetInstant();
                _panX = _panY = 0;
                _panning = false;
                foreach (var session in _sessions.Values)
                    session.RequestStop();
            }
        }
    }

    private void BeginReturnLocked()
    {
        if (_settings.AnimateReturn)
            _animator.BeginReturn(_settings.AnimationDurationMs / 1000.0, _settings.Curve);
        else
            _animator.SetTarget(1.0);
    }

    // ------------------------------------------------------------------
    // Frame production (render threads)
    // ------------------------------------------------------------------

    internal FrameState ComputeFrame(double dt, MonitorInfo monitor, bool isPrimary)
    {
        lock (_lock)
        {
            double zoom = isPrimary ? _animator.Update(dt) : _animator.Current;
            var (cx, cy) = MonitorService.GetCursorPosition();

            var bounds = monitor.Bounds;
            double localX = cx - bounds.X;
            double localY = cy - bounds.Y;
            if (!bounds.Contains(cx, cy) && !isPrimary)
            {
                localX = bounds.Width / 2.0;
                localY = bounds.Height / 2.0;
            }

            var vp = ViewportCalculator.Compute(
                bounds.Width, bounds.Height, localX, localY, zoom, _panX, _panY);
            if (isPrimary)
            {
                _panX = vp.EffectivePanX;
                _panY = vp.EffectivePanY;
            }

            bool idle = _animator.IsIdle;
            if (idle)
            {
                _panX = _panY = 0;
                _panning = false;
            }

            var view = new ViewRect(
                (float)(vp.X / bounds.Width), (float)(vp.Y / bounds.Height),
                (float)(vp.Width / bounds.Width), (float)(vp.Height / bounds.Height));
            return new FrameState(view, idle, _settings.VSync, _settings.MaxFps);
        }
    }

    internal void HandleSessionLost()
    {
        lock (_lock)
        {
            _animator.ResetInstant();
            _panX = _panY = 0;
            _panning = false;
            foreach (var session in _sessions.Values)
                session.RequestStop();
        }
    }

    internal void OnSessionExited(RenderSession session)
    {
        lock (_lock)
        {
            if (_sessions.TryGetValue(session.Monitor.DeviceName, out var current)
                && ReferenceEquals(current, session))
            {
                _sessions.Remove(session.Monitor.DeviceName);
            }
            if (_sessions.Count == 0)
            {
                _zoomActive = false;
                _panX = _panY = 0;
                _panning = false;
            }
        }
    }

    // ------------------------------------------------------------------
    // Session management (always called under _lock)
    // ------------------------------------------------------------------

    private void EnsureSessionsLocked(int x, int y, GlideSettings s)
    {
        if (_sessions.Count == 0)
            _monitors = MonitorService.GetMonitors();
        if (_monitors.Count == 0)
            return;

        var cursorMonitor = MonitorService.FromPoint(_monitors, x, y);
        if (cursorMonitor is null)
            return;

        if (s.MonitorMode == MonitorZoomMode.AllMonitors)
        {
            foreach (var monitor in _monitors)
            {
                if (!HasLiveSessionLocked(monitor.DeviceName))
                    StartSessionLocked(monitor, isPrimary: monitor == cursorMonitor);
            }
        }
        else if (!HasLiveSessionLocked(cursorMonitor.DeviceName))
        {
            foreach (var session in _sessions.Values)
                session.RequestStop();
            StartSessionLocked(cursorMonitor, isPrimary: true);
        }
        _zoomActive = true;
    }

    private void SwitchMonitorIfNeededLocked(int x, int y)
    {
        if (!_animator.IsZoomed || _monitors.Count < 2)
            return;

        bool coveredByActive = _sessions.Values.Any(session => session.Monitor.Bounds.Contains(x, y));
        if (coveredByActive)
            return;

        var target = MonitorService.FromPoint(_monitors, x, y);
        if (target is null || HasLiveSessionLocked(target.DeviceName))
            return;

        foreach (var session in _sessions.Values)
            session.RequestStop();
        _panX = _panY = 0;
        StartSessionLocked(target, isPrimary: true);
    }

    private bool HasLiveSessionLocked(string deviceName) =>
        _sessions.TryGetValue(deviceName, out var session) && !session.StopRequested;

    private void StartSessionLocked(MonitorInfo monitor, bool isPrimary)
    {
        _sessions[monitor.DeviceName] = new RenderSession(this, monitor, isPrimary);
        _zoomActive = true;
        Log.Info($"Zoom session started on {monitor.DeviceName} (primary={isPrimary})");
    }

    public void Dispose()
    {
        List<RenderSession> sessions;
        lock (_lock)
        {
            _animator.ResetInstant();
            sessions = [.. _sessions.Values];
            foreach (var session in sessions)
                session.RequestStop();
        }
        foreach (var session in sessions)
            session.Join(TimeSpan.FromSeconds(1));
    }
}
