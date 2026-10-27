namespace CSharpExercises.Numerics;

/// <summary>
/// <see cref="double"/> stores binary fractions, so most decimal fractions (0.1, 0.2, 0.3) are approximations, and
/// every operation can round. The consequences, and what to do about them.
/// </summary>
public static class FloatingPoint
{
    /// <summary>
    /// Compares with a tolerance instead of <c>==</c>. The relative tolerance scales with the size of the numbers;
    /// the absolute one handles values near zero, where any relative tolerance is too strict.
    /// </summary>
    public static bool NearlyEqual(double a, double b, double relativeTolerance = 1e-9, double absoluteTolerance = 1e-12)
    {
        if (a == b) return true; // also covers equal infinities
        if (double.IsNaN(a) || double.IsNaN(b) || double.IsInfinity(a) || double.IsInfinity(b)) return false;

        var difference = Math.Abs(a - b);
        return difference <= absoluteTolerance || difference <= relativeTolerance * Math.Max(Math.Abs(a), Math.Abs(b));
    }

    /// <summary>Adds in order. Each addition rounds, and over millions of values the errors add up.</summary>
    public static double NaiveSum(IEnumerable<double> values)
    {
        var sum = 0.0;
        foreach (var value in values) sum += value;
        return sum;
    }

    /// <summary>
    /// Kahan (compensated) summation: keeps the low-order bits that each addition rounds away in a separate
    /// "compensation" variable and feeds them back in. The error stays roughly constant instead of growing
    /// with the number of values.
    /// </summary>
    public static double KahanSum(IEnumerable<double> values)
    {
        var sum = 0.0;
        var compensation = 0.0;
        foreach (var value in values)
        {
            var y = value - compensation;
            var t = sum + y;
            compensation = (t - sum) - y; // algebraically zero; in floating point, exactly what was lost
            sum = t;
        }
        return sum;
    }

    /// <summary>
    /// How many representable doubles lie between <paramref name="a"/> and <paramref name="b"/> ("units in the last place").
    /// Works because positive doubles sort the same way as their bit patterns read as integers.
    /// </summary>
    public static long UlpDistance(double a, double b)
    {
        if (double.IsNaN(a) || double.IsNaN(b)) throw new ArgumentException("NaN has no distance.");

        static long Ordered(double x)
        {
            var bits = BitConverter.DoubleToInt64Bits(x);
            return bits < 0 ? long.MinValue - bits : bits; // map negative numbers so ordering is continuous through zero
        }

        return Math.Abs(Ordered(a) - Ordered(b));
    }
}
