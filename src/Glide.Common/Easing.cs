namespace Glide.Common;

/// <summary>Evaluates easing curves. Input and output are normalized to [0, 1].</summary>
public static class Easing
{
    public static double Evaluate(EasingCurve curve, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        double result = curve switch
        {
            EasingCurve.Linear => t,
            EasingCurve.EaseIn => t * t,
            EasingCurve.EaseOut => 1.0 - (1.0 - t) * (1.0 - t),
            EasingCurve.EaseInOut => EaseInOutQuad(t),
            EasingCurve.Cubic => EaseInOutCubic(t),
            EasingCurve.Quartic => EaseInOutQuart(t),
            EasingCurve.Exponential => EaseInOutExpo(t),
            EasingCurve.Spring => SpringOut(t),
            EasingCurve.Natural => SmoothStep(t),
            _ => t,
        };
        return Math.Clamp(result, 0.0, 1.0);
    }

    private static double EaseInOutQuad(double t) =>
        t < 0.5 ? 2.0 * t * t : 1.0 - Math.Pow(-2.0 * t + 2.0, 2.0) / 2.0;

    private static double EaseInOutCubic(double t) =>
        t < 0.5 ? 4.0 * t * t * t : 1.0 - Math.Pow(-2.0 * t + 2.0, 3.0) / 2.0;

    private static double EaseInOutQuart(double t) =>
        t < 0.5 ? 8.0 * t * t * t * t : 1.0 - Math.Pow(-2.0 * t + 2.0, 4.0) / 2.0;

    private static double EaseInOutExpo(double t)
    {
        if (t <= 0.0) return 0.0;
        if (t >= 1.0) return 1.0;
        return t < 0.5
            ? Math.Pow(2.0, 20.0 * t - 10.0) / 2.0
            : (2.0 - Math.Pow(2.0, -20.0 * t + 10.0)) / 2.0;
    }

    private static double SpringOut(double t)
    {
        if (t >= 1.0) return 1.0;
        return 1.0 - Math.Exp(-6.5 * t) * Math.Cos(9.0 * t);
    }

    /// <summary>Cubic smoothstep — the default "feels native" curve.</summary>
    private static double SmoothStep(double t) => t * t * (3.0 - 2.0 * t);
}
