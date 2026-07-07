using Glide.Engine;
using Xunit;

namespace Glide.Tests;

public class ViewportCalculatorTests
{
    private const double W = 1920;
    private const double H = 1080;

    [Fact]
    public void ZoomOneShowsTheFullMonitor()
    {
        var vp = ViewportCalculator.Compute(W, H, 500, 500, 1.0, 0, 0);

        Assert.Equal(0, vp.X, 6);
        Assert.Equal(0, vp.Y, 6);
        Assert.Equal(W, vp.Width, 6);
        Assert.Equal(H, vp.Height, 6);
    }

    [Fact]
    public void ZoomTwoAtCenterShowsCenteredHalfSizeViewport()
    {
        var vp = ViewportCalculator.Compute(W, H, W / 2, H / 2, 2.0, 0, 0);

        Assert.Equal(W / 4, vp.X, 6);
        Assert.Equal(H / 4, vp.Y, 6);
        Assert.Equal(W / 2, vp.Width, 6);
        Assert.Equal(H / 2, vp.Height, 6);
    }

    [Fact]
    public void ContentUnderCursorStaysUnderCursor()
    {
        // The pixel displayed at the cursor must be the desktop pixel at the
        // cursor position (clicks land where you point), for any zoom level.
        double cx = 700, cy = 300;
        foreach (var zoom in new[] { 1.5, 2.0, 4.0, 8.0 })
        {
            var vp = ViewportCalculator.Compute(W, H, cx, cy, zoom, 0, 0);
            var sourceAtCursor = vp.X + cx / zoom;
            Assert.Equal(cx, sourceAtCursor, 6);
        }
    }

    [Fact]
    public void CursorInCornerClampsViewportToEdges()
    {
        var vp = ViewportCalculator.Compute(W, H, 0, 0, 2.0, 0, 0);
        Assert.Equal(0, vp.X, 6);
        Assert.Equal(0, vp.Y, 6);

        var vp2 = ViewportCalculator.Compute(W, H, W, H, 2.0, 0, 0);
        Assert.Equal(W / 2, vp2.X, 6);
        Assert.Equal(H / 2, vp2.Y, 6);
    }

    [Fact]
    public void PanShiftsTheViewport()
    {
        var without = ViewportCalculator.Compute(W, H, W / 2, H / 2, 2.0, 0, 0);
        var with = ViewportCalculator.Compute(W, H, W / 2, H / 2, 2.0, 100, 50);

        Assert.Equal(without.X + 100, with.X, 6);
        Assert.Equal(without.Y + 50, with.Y, 6);
    }

    [Fact]
    public void ExcessivePanIsClampedAndReportedBack()
    {
        var vp = ViewportCalculator.Compute(W, H, W / 2, H / 2, 2.0, 99999, 99999);

        Assert.Equal(W - W / 2, vp.X, 6);
        Assert.Equal(H - H / 2, vp.Y, 6);
        // Effective pan must reproduce the clamped origin exactly.
        Assert.True(vp.EffectivePanX < 99999);
        var replay = ViewportCalculator.Compute(W, H, W / 2, H / 2, 2.0,
            vp.EffectivePanX, vp.EffectivePanY);
        Assert.Equal(vp.X, replay.X, 6);
        Assert.Equal(vp.Y, replay.Y, 6);
    }

    [Fact]
    public void ZoomBelowOneIsTreatedAsOne()
    {
        var vp = ViewportCalculator.Compute(W, H, 100, 100, 0.5, 0, 0);
        Assert.Equal(W, vp.Width, 6);
        Assert.Equal(H, vp.Height, 6);
    }
}
