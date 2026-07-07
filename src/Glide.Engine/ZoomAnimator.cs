using Glide.Common;

namespace Glide.Engine;

/// <summary>
/// Drives the zoom value over time. Two modes:
/// - Follow: exponential smoothing toward the wheel target (touchpad feel).
/// - Return: a timed, eased animation back to 100%.
/// Not thread-safe by itself; the engine serializes access.
/// </summary>
public sealed class ZoomAnimator
{
    private const double SnapEpsilon = 0.0005;

    private double _current = 1.0;
    private double _target = 1.0;

    private bool _returning;
    private double _returnFrom;
    private double _returnElapsed;
    private double _returnDuration = 0.25;
    private EasingCurve _returnCurve = EasingCurve.Natural;

    /// <summary>Time constant (seconds) of the follow smoothing.</summary>
    public double SmoothingTau { get; set; } = 0.09;

    public double Current => _current;
    public double Target => _target;

    public bool IsIdle => !_returning
        && Math.Abs(_current - _target) < SnapEpsilon
        && _target <= 1.0 + SnapEpsilon;

    public bool IsZoomed => _current > 1.0 + SnapEpsilon || _target > 1.0 + SnapEpsilon;

    public void SetTarget(double zoom)
    {
        _returning = false;
        _target = Math.Max(zoom, 1.0);
    }

    /// <summary>Starts an eased animation from the current zoom back to 100%.</summary>
    public void BeginReturn(double durationSeconds, EasingCurve curve)
    {
        if (!IsZoomed) return;
        _returning = true;
        _returnFrom = _current;
        _returnElapsed = 0.0;
        _returnDuration = Math.Max(durationSeconds, 0.01);
        _returnCurve = curve;
        _target = 1.0;
    }

    /// <summary>Jumps straight to 100% with no animation.</summary>
    public void ResetInstant()
    {
        _returning = false;
        _current = 1.0;
        _target = 1.0;
    }

    /// <summary>Advances the animation by <paramref name="dt"/> seconds and returns the new zoom.</summary>
    public double Update(double dt)
    {
        if (dt <= 0.0) return _current;

        if (_returning)
        {
            _returnElapsed += dt;
            double t = _returnElapsed / _returnDuration;
            if (t >= 1.0)
            {
                _returning = false;
                _current = 1.0;
            }
            else
            {
                double eased = Easing.Evaluate(_returnCurve, t);
                _current = _returnFrom + (1.0 - _returnFrom) * eased;
            }
            return _current;
        }

        double alpha = 1.0 - Math.Exp(-dt / Math.Max(SmoothingTau, 0.005));
        _current += (_target - _current) * alpha;
        if (Math.Abs(_current - _target) < SnapEpsilon)
            _current = _target;
        return _current;
    }
}
