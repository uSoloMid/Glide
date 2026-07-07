using Glide.Common;
using Glide.Engine;
using Xunit;

namespace Glide.Tests;

public class ZoomAnimatorTests
{
    private static void Advance(ZoomAnimator animator, double seconds, double step = 1.0 / 120.0)
    {
        for (double t = 0; t < seconds; t += step)
            animator.Update(step);
    }

    [Fact]
    public void StartsIdleAtOneHundredPercent()
    {
        var animator = new ZoomAnimator();
        Assert.Equal(1.0, animator.Current, 6);
        Assert.True(animator.IsIdle);
        Assert.False(animator.IsZoomed);
    }

    [Fact]
    public void FollowsTargetSmoothlyAndConverges()
    {
        var animator = new ZoomAnimator { SmoothingTau = 0.08 };
        animator.SetTarget(3.0);

        animator.Update(1.0 / 120.0);
        Assert.InRange(animator.Current, 1.0001, 2.9999); // moving, not jumping

        Advance(animator, 2.0);
        Assert.Equal(3.0, animator.Current, 3);
        Assert.True(animator.IsZoomed);
        Assert.False(animator.IsIdle);
    }

    [Fact]
    public void ReturnAnimationReachesExactlyOne()
    {
        var animator = new ZoomAnimator { SmoothingTau = 0.05 };
        animator.SetTarget(4.0);
        Advance(animator, 1.0);

        animator.BeginReturn(0.3, EasingCurve.Natural);
        Advance(animator, 0.5);

        Assert.Equal(1.0, animator.Current, 6);
        Assert.True(animator.IsIdle);
    }

    [Fact]
    public void ScrollingDuringReturnCancelsIt()
    {
        var animator = new ZoomAnimator();
        animator.SetTarget(4.0);
        Advance(animator, 1.0);
        animator.BeginReturn(0.5, EasingCurve.Natural);
        Advance(animator, 0.1);

        animator.SetTarget(2.5);
        Advance(animator, 2.0);

        Assert.Equal(2.5, animator.Current, 3);
    }

    [Fact]
    public void ResetInstantJumpsToOne()
    {
        var animator = new ZoomAnimator();
        animator.SetTarget(5.0);
        Advance(animator, 1.0);

        animator.ResetInstant();

        Assert.Equal(1.0, animator.Current, 6);
        Assert.True(animator.IsIdle);
    }

    [Fact]
    public void BeginReturnWhenNotZoomedIsNoOp()
    {
        var animator = new ZoomAnimator();
        animator.BeginReturn(0.3, EasingCurve.Natural);
        Assert.True(animator.IsIdle);
    }
}
