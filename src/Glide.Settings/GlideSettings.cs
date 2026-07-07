using Glide.Common;

namespace Glide.Settings;

/// <summary>Modifier that must be held while scrolling to zoom.</summary>
public enum ModifierKey
{
    Ctrl,
    Alt,
    Shift,
    Win,
    XButton1,
    XButton2,
}

/// <summary>Whether zoom snaps back when the modifier is released.</summary>
public enum ZoomMode
{
    /// <summary>Zoom returns to 100% when the modifier key is released.</summary>
    Temporary,

    /// <summary>Zoom stays until the user resets it.</summary>
    Persistent,
}

/// <summary>Which monitors get zoomed.</summary>
public enum MonitorZoomMode
{
    /// <summary>Only the monitor under the cursor.</summary>
    CursorMonitor,

    /// <summary>Every connected monitor.</summary>
    AllMonitors,
}

/// <summary>All user-configurable options. Persisted as JSON.</summary>
public sealed class GlideSettings
{
    public bool Enabled { get; set; } = true;

    // Activation
    public ModifierKey Modifier { get; set; } = ModifierKey.Ctrl;
    public ZoomMode Mode { get; set; } = ZoomMode.Temporary;
    public bool DoubleTapReset { get; set; } = true;

    // Zoom range (1.0 = 100%, 10.0 = 1000%)
    public double MinZoom { get; set; } = 1.0;
    public double MaxZoom { get; set; } = 10.0;
    public double ZoomSpeed { get; set; } = 1.0;          // 0.1 .. 3.0
    public double ScrollSensitivity { get; set; } = 1.0;  // 0.1 .. 3.0

    // Animation
    public int AnimationDurationMs { get; set; } = 250;   // 50 .. 1000
    public EasingCurve Curve { get; set; } = EasingCurve.Natural;
    public bool AnimateReturn { get; set; } = true;

    // Pan
    public bool PanWithMiddleButton { get; set; } = true;
    public double PanSpeed { get; set; } = 1.0;           // 0.1 .. 3.0

    // Monitors / rendering
    public MonitorZoomMode MonitorMode { get; set; } = MonitorZoomMode.CursorMonitor;
    public bool VSync { get; set; } = true;
    public int MaxFps { get; set; } = 240;                // used when VSync is off

    // System integration
    public bool StartWithWindows { get; set; }

    /// <summary>Process names (e.g. "valorant.exe") where Glide never activates.</summary>
    public List<string> ExcludedApps { get; set; } = [];

    public GlideSettings Clone() => new()
    {
        Enabled = Enabled,
        Modifier = Modifier,
        Mode = Mode,
        DoubleTapReset = DoubleTapReset,
        MinZoom = MinZoom,
        MaxZoom = MaxZoom,
        ZoomSpeed = ZoomSpeed,
        ScrollSensitivity = ScrollSensitivity,
        AnimationDurationMs = AnimationDurationMs,
        Curve = Curve,
        AnimateReturn = AnimateReturn,
        PanWithMiddleButton = PanWithMiddleButton,
        PanSpeed = PanSpeed,
        MonitorMode = MonitorMode,
        VSync = VSync,
        MaxFps = MaxFps,
        StartWithWindows = StartWithWindows,
        ExcludedApps = [.. ExcludedApps],
    };
}
