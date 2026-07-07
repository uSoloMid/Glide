using System.Diagnostics;
using Glide.Common;
using Glide.Graphics;

namespace Glide.Engine;

/// <summary>
/// One render thread per zoomed monitor: owns the overlay window, the desktop
/// duplication and the swap chain, and asks the engine what to draw each frame.
/// </summary>
internal sealed class RenderSession
{
    private const int AcquireTimeoutMs = 8;
    private const int PrimeTimeoutMs = 700;

    private readonly GlideEngine _engine;
    private readonly Thread _thread;
    private volatile bool _stop;

    public MonitorInfo Monitor { get; }
    public bool IsPrimary { get; }

    public RenderSession(GlideEngine engine, MonitorInfo monitor, bool isPrimary)
    {
        _engine = engine;
        Monitor = monitor;
        IsPrimary = isPrimary;
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = $"Glide.Render[{monitor.DeviceName}]",
            Priority = ThreadPriority.AboveNormal,
        };
        _thread.Start();
    }

    public bool StopRequested => _stop;

    public void RequestStop() => _stop = true;

    public void Join(TimeSpan timeout) => _thread.Join(timeout);

    private void Run()
    {
        GraphicsStack? gfx = null;
        try
        {
            gfx = new GraphicsStack(Monitor.DeviceName, Monitor.Bounds);
            if (!PrimeFirstFrame(gfx))
            {
                Log.Error($"No desktop frame arrived for {Monitor.DeviceName}; aborting session");
                _engine.HandleSessionLost();
                return;
            }
            gfx.Window.Show();
            RenderLoop(gfx);
        }
        catch (DuplicationLostException ex)
        {
            Log.Info($"Duplication lost on {Monitor.DeviceName}: {ex.Message}");
            _engine.HandleSessionLost();
        }
        catch (Exception ex)
        {
            Log.Error($"Render session crashed on {Monitor.DeviceName}", ex);
            _engine.HandleSessionLost();
        }
        finally
        {
            gfx?.Dispose();
            _engine.OnSessionExited(this);
        }
    }

    /// <summary>Waits for the first captured frame so the overlay never flashes black.</summary>
    private bool PrimeFirstFrame(GraphicsStack gfx)
    {
        var deadline = Environment.TickCount64 + PrimeTimeoutMs;
        while (!_stop && Environment.TickCount64 < deadline)
        {
            gfx.Window.Pump();
            if (gfx.Duplicator.TryAcquireFrame(50))
                return true;
        }
        return gfx.Duplicator.HasFrame;
    }

    private void RenderLoop(GraphicsStack gfx)
    {
        long last = Stopwatch.GetTimestamp();
        while (!_stop)
        {
            gfx.Window.Pump();
            gfx.Duplicator.TryAcquireFrame(AcquireTimeoutMs);

            long now = Stopwatch.GetTimestamp();
            double dt = (now - last) / (double)Stopwatch.Frequency;
            last = now;

            var frame = _engine.ComputeFrame(dt, Monitor, IsPrimary);
            gfx.Renderer.Render(gfx.Duplicator.ShaderView, frame.View, frame.VSync);

            if (frame.Idle)
                break;

            if (!frame.VSync)
                LimitFps(frame.MaxFps, now);
        }
    }

    private static void LimitFps(int maxFps, long frameStart)
    {
        double budgetMs = 1000.0 / Math.Max(maxFps, 30);
        double elapsedMs = (Stopwatch.GetTimestamp() - frameStart) * 1000.0 / Stopwatch.Frequency;
        int sleep = (int)(budgetMs - elapsedMs);
        if (sleep > 0)
            Thread.Sleep(sleep);
    }
}
