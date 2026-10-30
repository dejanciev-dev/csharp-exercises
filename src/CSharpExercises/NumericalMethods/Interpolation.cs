namespace CSharpExercises.NumericalMethods;

/// <summary>
/// Estimating values between known points. Piecewise linear is robust. One polynomial through all points (Lagrange)
/// is exact at the points but oscillates wildly between them as the count grows (Runge's phenomenon). A natural cubic
/// spline joins cubic pieces with matching first and second derivatives: smooth, and well-behaved.
/// </summary>
public static class Interpolation
{
    public static double Linear(IReadOnlyList<(double X, double Y)> points, double x)
    {
        var sorted = points.OrderBy(p => p.X).ToArray();
        if (x <= sorted[0].X) return sorted[0].Y;
        if (x >= sorted[^1].X) return sorted[^1].Y; // clamp instead of extrapolating
        var i = Array.FindIndex(sorted, p => p.X >= x);
        var (a, b) = (sorted[i - 1], sorted[i]);
        return a.Y + (b.Y - a.Y) * (x - a.X) / (b.X - a.X);
    }

    public static double Lagrange(IReadOnlyList<(double X, double Y)> points, double x)
    {
        var sum = 0.0;
        for (var i = 0; i < points.Count; i++)
        {
            var term = points[i].Y;
            for (var j = 0; j < points.Count; j++)
                if (i != j) term *= (x - points[j].X) / (points[i].X - points[j].X);
            sum += term;
        }
        return sum;
    }

    /// <summary>
    /// Builds a natural cubic spline (second derivative zero at both ends). The second derivatives at the knots solve a
    /// tridiagonal system, done in O(n) with the Thomas algorithm.
    /// </summary>
    public static Func<double, double> CubicSpline(IReadOnlyList<(double X, double Y)> points)
    {
        var p = points.OrderBy(q => q.X).ToArray();
        var n = p.Length;
        if (n < 3) throw new ArgumentException("Need at least three points.", nameof(points));

        var h = Enumerable.Range(0, n - 1).Select(i => p[i + 1].X - p[i].X).ToArray();
        if (h.Any(d => d <= 0)) throw new ArgumentException("X values must be distinct.", nameof(points));

        // Tridiagonal system for the second derivatives m[1..n-2]; m[0] = m[n-1] = 0.
        var m = new double[n];
        var c = new double[n];
        var d = new double[n];
        for (var i = 1; i < n - 1; i++)
        {
            var rhs = 6 * ((p[i + 1].Y - p[i].Y) / h[i] - (p[i].Y - p[i - 1].Y) / h[i - 1]);
            var diag = 2 * (h[i - 1] + h[i]) - h[i - 1] * c[i - 1];
            c[i] = h[i] / diag;
            d[i] = (rhs - h[i - 1] * d[i - 1]) / diag;
        }
        for (var i = n - 2; i >= 1; i--) m[i] = d[i] - c[i] * m[i + 1];

        return x =>
        {
            var i = Math.Clamp(Array.FindLastIndex(p, q => q.X <= x), 0, n - 2);
            var t = x - p[i].X;
            var hi = h[i];
            var a = (m[i + 1] - m[i]) / (6 * hi);
            var b = m[i] / 2;
            var slope = (p[i + 1].Y - p[i].Y) / hi - hi * (2 * m[i] + m[i + 1]) / 6;
            return p[i].Y + slope * t + b * t * t + a * t * t * t;
        };
    }
}
