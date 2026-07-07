namespace Glide.Engine;

/// <summary>Result of a viewport computation, in monitor-local pixels.</summary>
public readonly record struct ZoomViewport(
    double X, double Y, double Width, double Height,
    double EffectivePanX, double EffectivePanY);

/// <summary>
/// Pure math for the cursor-centered viewport.
///
/// Model: a screen point p displays desktop content at (origin + p / zoom).
/// The origin is anchored so the content under the cursor is exactly the
/// content at the cursor's physical position (clicks always land where you
/// point), shifted by an explicit pan offset accumulated while dragging.
/// </summary>
public static class ViewportCalculator
{
    public static ZoomViewport Compute(
        double monitorWidth, double monitorHeight,
        double cursorX, double cursorY,
        double zoom, double panX, double panY)
    {
        if (zoom < 1.0) zoom = 1.0;

        double width = monitorWidth / zoom;
        double height = monitorHeight / zoom;

        double anchorX = Math.Clamp(cursorX, 0.0, monitorWidth);
        double anchorY = Math.Clamp(cursorY, 0.0, monitorHeight);

        double scale = 1.0 - 1.0 / zoom;
        double originX = anchorX * scale + panX;
        double originY = anchorY * scale + panY;

        originX = Math.Clamp(originX, 0.0, monitorWidth - width);
        originY = Math.Clamp(originY, 0.0, monitorHeight - height);

        // Report the pan that survived clamping so it cannot drift past edges.
        double effectivePanX = originX - anchorX * scale;
        double effectivePanY = originY - anchorY * scale;

        return new ZoomViewport(originX, originY, width, height, effectivePanX, effectivePanY);
    }
}
