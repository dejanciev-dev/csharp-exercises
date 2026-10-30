namespace CSharpExercises.NumericalMethods;

public sealed record Root(double Value, int Iterations);

/// <summary>
/// Solving f(x) = 0 numerically. Bisection halves a bracketing interval: slow (one bit per step) but guaranteed.
/// Newton follows the tangent and doubles the correct digits each step near a simple root, but needs the derivative and
/// can fly off. The secant method approximates the derivative from the last two points. Each reports its iterations so
/// the convergence speeds can be compared.
/// </summary>
public static class RootFinding
{
    public static Root Bisection(Func<double, double> f, double low, double high, double tolerance = 1e-12, int maxIterations = 200)
    {
        double fLow = f(low), fHigh = f(high);
        if (Math.Sign(fLow) == Math.Sign(fHigh)) throw new ArgumentException("f(low) and f(high) must have opposite signs.");

        for (var i = 1; i <= maxIterations; i++)
        {
            var mid = low + (high - low) / 2; // avoids overflow of (low + high)
            var fMid = f(mid);
            if (fMid == 0 || (high - low) / 2 < tolerance) return new Root(mid, i);
            if (Math.Sign(fMid) == Math.Sign(fLow)) (low, fLow) = (mid, fMid);
            else high = mid;
        }
        throw new InvalidOperationException("Bisection did not converge.");
    }

    public static Root Newton(Func<double, double> f, Func<double, double> derivative, double guess, double tolerance = 1e-12, int maxIterations = 50)
    {
        var x = guess;
        for (var i = 1; i <= maxIterations; i++)
        {
            var d = derivative(x);
            if (d == 0) throw new InvalidOperationException($"Zero derivative at {x}: the tangent never crosses the axis.");
            var next = x - f(x) / d;
            if (Math.Abs(next - x) < tolerance) return new Root(next, i);
            x = next;
        }
        throw new InvalidOperationException("Newton's method did not converge.");
    }

    public static Root Secant(Func<double, double> f, double x0, double x1, double tolerance = 1e-12, int maxIterations = 100)
    {
        for (var i = 1; i <= maxIterations; i++)
        {
            var (f0, f1) = (f(x0), f(x1));
            if (f1 == f0) throw new InvalidOperationException("Flat secant.");
            var next = x1 - f1 * (x1 - x0) / (f1 - f0);
            if (Math.Abs(next - x1) < tolerance) return new Root(next, i);
            (x0, x1) = (x1, next);
        }
        throw new InvalidOperationException("The secant method did not converge.");
    }

    /// <summary>Square root by Newton on x² - a: the Babylonian method, x ← (x + a/x) / 2.</summary>
    public static double Sqrt(double a)
    {
        if (a < 0) throw new ArgumentOutOfRangeException(nameof(a));
        if (a == 0) return 0;
        return Newton(x => x * x - a, x => 2 * x, Math.Max(a, 1)).Value;
    }
}
