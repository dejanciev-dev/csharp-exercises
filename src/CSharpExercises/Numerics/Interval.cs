using System.Globalization;

namespace CSharpExercises.Numerics;

/// <summary>
/// Interval arithmetic: compute with ranges instead of single values, so the result is guaranteed to contain the true
/// answer. Useful for measurements with tolerances ("10 ± 0.1 cm"), for bounding floating-point error, and for proving
/// properties of functions over a range. Multiplication takes the min and max of all endpoint products; division by an
/// interval containing zero is undefined.
/// </summary>
public readonly record struct Interval(double Low, double High)
{
    public static Interval Of(double value, double tolerance) => new(value - tolerance, value + tolerance);

    public double Width => High - Low;
    public double Mid => (Low + High) / 2;
    public bool Contains(double x) => x >= Low && x <= High;
    public bool Contains(Interval other) => other.Low >= Low && other.High <= High;

    public static Interval operator +(Interval a, Interval b) => new(a.Low + b.Low, a.High + b.High);
    public static Interval operator -(Interval a, Interval b) => new(a.Low - b.High, a.High - b.Low); // not Low - Low!

    public static Interval operator *(Interval a, Interval b)
    {
        double[] products = [a.Low * b.Low, a.Low * b.High, a.High * b.Low, a.High * b.High];
        return new(products.Min(), products.Max());
    }

    public static Interval operator /(Interval a, Interval b) =>
        b.Contains(0) ? throw new DivideByZeroException("The divisor interval contains zero.") : a * new Interval(1 / b.High, 1 / b.Low);

    /// <summary>x² is not x·x for intervals: [-1, 2]² is [0, 4], but [-1, 2]·[-1, 2] is [-2, 4] (the dependency problem).</summary>
    public Interval Square() =>
        Contains(0) ? new(0, Math.Max(Low * Low, High * High)) : new(Math.Min(Low * Low, High * High), Math.Max(Low * Low, High * High));

    public Interval Sqrt() => Low < 0 ? throw new ArgumentOutOfRangeException(nameof(Low)) : new(Math.Sqrt(Low), Math.Sqrt(High));

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"[{Low:0.####}, {High:0.####}]");
}
