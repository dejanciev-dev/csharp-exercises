namespace CSharpExercises.NumericalMethods;

/// <summary>
/// Numerical integration. The trapezoid rule joins samples with straight lines (error ∝ h²); Simpson's rule fits
/// parabolas through pairs of intervals (error ∝ h⁴), so it's far more accurate for the same number of samples.
/// Adaptive Simpson spends samples only where the function is hard, by splitting an interval until both halves agree
/// with the whole.
/// </summary>
public static class Integration
{
    public static double Trapezoid(Func<double, double> f, double a, double b, int intervals)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(intervals, 1);
        var h = (b - a) / intervals;
        var sum = (f(a) + f(b)) / 2;
        for (var i = 1; i < intervals; i++) sum += f(a + i * h);
        return sum * h;
    }

    public static double Simpson(Func<double, double> f, double a, double b, int intervals)
    {
        if (intervals < 2 || intervals % 2 != 0) throw new ArgumentException("Simpson's rule needs an even number of intervals.", nameof(intervals));
        var h = (b - a) / intervals;
        var sum = f(a) + f(b);
        for (var i = 1; i < intervals; i++) sum += f(a + i * h) * (i % 2 == 1 ? 4 : 2);
        return sum * h / 3;
    }

    public static (double Value, int Evaluations) AdaptiveSimpson(Func<double, double> f, double a, double b, double tolerance = 1e-10)
    {
        var evaluations = 0;
        double F(double x) { evaluations++; return f(x); }

        double Recurse(double a, double b, double fa, double fm, double fb, double whole, double tol, int depth)
        {
            var m = (a + b) / 2;
            double lm = (a + m) / 2, rm = (m + b) / 2;
            double flm = F(lm), frm = F(rm);
            var left = (m - a) / 6 * (fa + 4 * flm + fm);
            var right = (b - m) / 6 * (fm + 4 * frm + fb);
            var delta = left + right - whole;
            if (depth > 40 || Math.Abs(delta) <= 15 * tol) return left + right + delta / 15; // Richardson extrapolation
            return Recurse(a, m, fa, flm, fm, left, tol / 2, depth + 1) + Recurse(m, b, fm, frm, fb, right, tol / 2, depth + 1);
        }

        double fa = F(a), fb = F(b), fm = F((a + b) / 2);
        var value = Recurse(a, b, fa, fm, fb, (b - a) / 6 * (fa + 4 * fm + fb), tolerance, 0);
        return (value, evaluations);
    }

    /// <summary>Monte Carlo integration: average f at random points. Error ∝ 1/√n regardless of dimension.</summary>
    public static double MonteCarlo(Func<double, double> f, double a, double b, int samples, Random random) =>
        (b - a) * Enumerable.Range(0, samples).Average(_ => f(a + (b - a) * random.NextDouble()));
}
