namespace CSharpExercises.Numerics;

/// <summary>
/// Divisor functions and the number families defined by them. Listing divisors only needs to search up to √n (they come
/// in pairs). For all numbers up to N at once, a divisor-sum sieve adds each d to all its multiples in O(N log N), which
/// finds perfect numbers (σ(n) = 2n), abundant numbers and amicable pairs (220 and 284) quickly.
/// </summary>
public static class Divisors
{
    public static IReadOnlyList<long> Of(long n)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(n, 1);
        var small = new List<long>();
        var large = new List<long>();
        for (long d = 1; d * d <= n; d++)
        {
            if (n % d != 0) continue;
            small.Add(d);
            if (d != n / d) large.Add(n / d);
        }
        large.Reverse();
        return [.. small, .. large];
    }

    /// <summary>Number of divisors from the prime factorisation: Π (exponent + 1).</summary>
    public static int Count(long n)
    {
        var count = 1;
        for (long p = 2; p * p <= n; p++)
        {
            var exponent = 0;
            while (n % p == 0) { n /= p; exponent++; }
            count *= exponent + 1;
        }
        return n > 1 ? count * 2 : count;
    }

    /// <summary>σ(n) - n for every n up to <paramref name="limit"/>: the sum of proper divisors.</summary>
    public static long[] ProperDivisorSums(int limit)
    {
        var sums = new long[limit + 1];
        for (var d = 1; d <= limit / 2; d++)
            for (var multiple = 2 * d; multiple <= limit; multiple += d) sums[multiple] += d;
        return sums;
    }

    public static IEnumerable<int> PerfectNumbers(int limit)
    {
        var sums = ProperDivisorSums(limit);
        return Enumerable.Range(2, limit - 1).Where(n => sums[n] == n);
    }

    public static IEnumerable<(int A, int B)> AmicablePairs(int limit)
    {
        var sums = ProperDivisorSums(limit);
        for (var a = 2; a <= limit; a++)
        {
            var b = sums[a];
            if (b > a && b <= limit && sums[b] == a) yield return (a, (int)b);
        }
    }

    public static string Classify(long n)
    {
        var sum = Of(n).Sum() - n;
        return sum == n ? "perfect" : sum > n ? "abundant" : "deficient";
    }
}
