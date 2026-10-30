namespace CSharpExercises.Combinatorics;

/// <summary>
/// Inclusion–exclusion: to count things with at least one of several properties, add the single counts, subtract the
/// pairwise overlaps, add back the triple overlaps, and so on. Iterating subsets as bit masks keeps it compact.
/// </summary>
public static class InclusionExclusion
{
    /// <summary>How many numbers in 1..limit are divisible by at least one of the divisors.</summary>
    public static long CountDivisible(long limit, IReadOnlyList<long> divisors)
    {
        if (divisors.Count > 20) throw new ArgumentException("Too many divisors for 2^n subsets.", nameof(divisors));
        long total = 0;
        for (var mask = 1; mask < 1 << divisors.Count; mask++)
        {
            long lcm = 1;
            var bits = 0;
            for (var i = 0; i < divisors.Count && lcm <= limit; i++)
            {
                if ((mask & (1 << i)) == 0) continue;
                lcm = Lcm(lcm, divisors[i]);
                bits++;
            }
            if (lcm > limit) continue;
            total += (bits % 2 == 1 ? 1 : -1) * (limit / lcm);
        }
        return total;
    }

    /// <summary>
    /// Surjections: ways to hand n distinct items to k people so everyone gets at least one.
    /// Σ (-1)^i · C(k, i) · (k - i)^n, subtracting the assignments that leave somebody out.
    /// </summary>
    public static System.Numerics.BigInteger Surjections(int n, int k)
    {
        System.Numerics.BigInteger total = 0;
        for (var i = 0; i <= k; i++)
        {
            var term = Binomial.Choose(k, i) * System.Numerics.BigInteger.Pow(k - i, n);
            total += i % 2 == 0 ? term : -term;
        }
        return total;
    }

    /// <summary>Euler's totient by inclusion–exclusion over the prime factors: numbers below n sharing no factor with it.</summary>
    public static long Totient(long n)
    {
        var primes = new List<long>();
        var m = n;
        for (long p = 2; p * p <= m; p++)
        {
            if (m % p != 0) continue;
            primes.Add(p);
            while (m % p == 0) m /= p;
        }
        if (m > 1) primes.Add(m);
        return n - CountDivisible(n, primes);
    }

    private static long Lcm(long a, long b) => a / Gcd(a, b) * b;
    private static long Gcd(long a, long b) => b == 0 ? a : Gcd(b, a % b);
}
