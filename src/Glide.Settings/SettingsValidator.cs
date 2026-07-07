namespace Glide.Settings;

/// <summary>Clamps every setting into its legal range (fail-safe on bad JSON edits).</summary>
public static class SettingsValidator
{
    public const double AbsoluteMinZoom = 1.0;
    public const double AbsoluteMaxZoom = 10.0;

    public static GlideSettings Sanitize(GlideSettings s)
    {
        s.MinZoom = Math.Clamp(s.MinZoom, AbsoluteMinZoom, AbsoluteMaxZoom);
        s.MaxZoom = Math.Clamp(s.MaxZoom, AbsoluteMinZoom, AbsoluteMaxZoom);
        if (s.MaxZoom < s.MinZoom)
            (s.MinZoom, s.MaxZoom) = (s.MaxZoom, s.MinZoom);

        s.ZoomSpeed = Math.Clamp(s.ZoomSpeed, 0.1, 3.0);
        s.ScrollSensitivity = Math.Clamp(s.ScrollSensitivity, 0.1, 3.0);
        s.AnimationDurationMs = Math.Clamp(s.AnimationDurationMs, 50, 1000);
        s.PanSpeed = Math.Clamp(s.PanSpeed, 0.1, 3.0);
        s.MaxFps = Math.Clamp(s.MaxFps, 30, 480);

        s.ExcludedApps = s.ExcludedApps
            .Select(a => a?.Trim() ?? string.Empty)
            .Where(a => a.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return s;
    }
}
