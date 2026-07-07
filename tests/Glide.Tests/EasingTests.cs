using Glide.Common;
using Xunit;

namespace Glide.Tests;

public class EasingTests
{
    public static TheoryData<EasingCurve> AllCurves()
    {
        var data = new TheoryData<EasingCurve>();
        foreach (var curve in Enum.GetValues<EasingCurve>())
            data.Add(curve);
        return data;
    }

    [Theory]
    [MemberData(nameof(AllCurves))]
    public void StartsAtZeroAndEndsAtOne(EasingCurve curve)
    {
        // Arrange & Act
        var start = Easing.Evaluate(curve, 0.0);
        var end = Easing.Evaluate(curve, 1.0);

        // Assert
        Assert.Equal(0.0, start, 3);
        Assert.Equal(1.0, end, 3);
    }

    [Theory]
    [MemberData(nameof(AllCurves))]
    public void OutputStaysWithinUnitRange(EasingCurve curve)
    {
        for (double t = -0.5; t <= 1.5; t += 0.01)
        {
            var value = Easing.Evaluate(curve, t);
            Assert.InRange(value, 0.0, 1.0);
        }
    }

    [Fact]
    public void LinearIsIdentity()
    {
        Assert.Equal(0.25, Easing.Evaluate(EasingCurve.Linear, 0.25), 6);
        Assert.Equal(0.75, Easing.Evaluate(EasingCurve.Linear, 0.75), 6);
    }

    [Fact]
    public void ClampsInputOutsideRange()
    {
        Assert.Equal(0.0, Easing.Evaluate(EasingCurve.Cubic, -3.0), 6);
        Assert.Equal(1.0, Easing.Evaluate(EasingCurve.Cubic, 7.0), 6);
    }
}
