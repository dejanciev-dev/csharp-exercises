namespace CSharpExercises.Numerics;

/// <summary>
/// Continued fractions write a number as a0 + 1/(a1 + 1/(a2 + …)). Truncating gives the convergents, which are the best
/// rational approximations: 355/113 for π is accurate to 7 digits. Useful for turning a decimal into a sensible fraction
/// ("0.333333 → 1/3"), choosing gear ratios, or finding a fraction with a bounded denominator (Stern–Brocot style).
/// </summary>
public static class ContinuedFractions
{
    public static IReadOnlyList<long> Expand(double value, int maxTerms = 20, double epsilon = 1e-12)
    {
        var terms = new List<long>();
        for (var i = 0; i < maxTerms; i++)
        {
            var whole = (long)Math.Floor(value);
            terms.Add(whole);
            var fraction = value - whole;
            if (fraction < epsilon) break;
            value = 1 / fraction;
        }
        return terms;
    }

    /// <summary>h(n) = a(n)·h(n-1) + h(n-2), likewise for k: the successive best approximations.</summary>
    public static IEnumerable<(long Numerator, long Denominator)> Convergents(IReadOnlyList<long> terms)
    {
        long h1 = 1, h2 = 0, k1 = 0, k2 = 1;
        foreach (var a in terms)
        {
            (h1, h2) = (checked(a * h1 + h2), h1);
            (k1, k2) = (checked(a * k1 + k2), k1);
            yield return (h1, k1);
        }
    }

    /// <summary>The closest fraction whose denominator doesn't exceed <paramref name="maxDenominator"/>.</summary>
    public static (long Numerator, long Denominator) BestApproximation(double value, long maxDenominator)
    {
        var terms = Expand(value, 40);
        (long N, long D) best = ((long)Math.Round(value), 1);
        long h1 = 1, h2 = 0, k1 = 0, k2 = 1;
        foreach (var a in terms)
        {
            var k = a * k1 + k2;
            if (k > maxDenominator)
            {
                // A semiconvergent (a smaller last term) may still beat the previous convergent.
                var t = (maxDenominator - k2) / k1;
                var semi = (t * h1 + h2, t * k1 + k2);
                if (Math.Abs(value - (double)semi.Item1 / semi.Item2) < Math.Abs(value - (double)best.N / best.D)) best = semi;
                break;
            }
            (h1, h2) = (a * h1 + h2, h1);
            (k1, k2) = (k, k1);
            best = (h1, k1);
        }
        return best;
    }

    /// <summary>A repeating decimal as an exact fraction: 0.1(6) = 1/6.</summary>
    public static (long Numerator, long Denominator) FromRepeatingDecimal(string nonRepeating, string repeating)
    {
        // x = 0.AB(C): x·10^(a+r) - x·10^a = ABC - AB
        var a = nonRepeating.Length;
        var r = repeating.Length;
        var whole = long.Parse(nonRepeating + repeating) - (a == 0 ? 0 : long.Parse(nonRepeating));
        var denominator = (long)Math.Pow(10, a + r) - (long)Math.Pow(10, a);
        var gcd = Gcd(whole, denominator);
        return (whole / gcd, denominator / gcd);
    }

    private static long Gcd(long a, long b) => b == 0 ? Math.Abs(a) : Gcd(b, a % b);
}
