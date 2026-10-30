using System.Numerics;

namespace CSharpExercises.Combinatorics;

/// <summary>
/// "n choose k" three ways. Factorials overflow long at 21!, so the exact version multiplies and divides alternately
/// (every partial product is itself a binomial coefficient, so the division is always exact). Pascal's triangle
/// needs only additions. Modulo a prime, Lucas' theorem handles n far larger than any table.
/// </summary>
public static class Binomial
{
    public static BigInteger Choose(int n, int k)
    {
        if (k < 0 || k > n) return 0;
        k = Math.Min(k, n - k); // C(n, k) = C(n, n - k): fewer steps
        BigInteger result = 1;
        for (var i = 1; i <= k; i++) result = result * (n - k + i) / i;
        return result;
    }

    public static long[][] PascalTriangle(int rows)
    {
        var triangle = new long[rows][];
        for (var n = 0; n < rows; n++)
        {
            triangle[n] = new long[n + 1];
            triangle[n][0] = triangle[n][n] = 1;
            for (var k = 1; k < n; k++) triangle[n][k] = triangle[n - 1][k - 1] + triangle[n - 1][k];
        }
        return triangle;
    }

    /// <summary>C(n, k) mod a small prime p: multiply the binomials of the base-p digits of n and k.</summary>
    public static long ChooseModPrime(long n, long k, int p)
    {
        long result = 1;
        while (n > 0 || k > 0)
        {
            var (ni, ki) = (n % p, k % p);
            if (ki > ni) return 0;
            result = result * SmallChooseMod(ni, ki, p) % p;
            (n, k) = (n / p, k / p);
        }
        return result;
    }

    /// <summary>Paths through a grid moving only right or down: choose which of the steps go right.</summary>
    public static BigInteger LatticePaths(int width, int height) => Choose(width + height, width);

    private static long SmallChooseMod(long n, long k, int p)
    {
        long numerator = 1, denominator = 1;
        for (var i = 0; i < k; i++)
        {
            numerator = numerator * ((n - i) % p) % p;
            denominator = denominator * ((i + 1) % p) % p;
        }
        return numerator * (long)BigInteger.ModPow(denominator, p - 2, p) % p; // Fermat inverse
    }
}
