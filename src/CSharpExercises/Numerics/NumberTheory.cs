using System.Numerics;

namespace CSharpExercises.Numerics;

/// <summary>Modular arithmetic: the maths behind hashing, random number generators and public-key cryptography.</summary>
public static class NumberTheory
{
    /// <summary>
    /// (b^e) mod m by square-and-multiply: O(log e) multiplications instead of e. Intermediate products use
    /// <see cref="UInt128"/>, because the product of two 64-bit numbers needs up to 128 bits.
    /// </summary>
    public static ulong ModPow(ulong @base, ulong exponent, ulong modulus)
    {
        ArgumentOutOfRangeException.ThrowIfZero(modulus);
        if (modulus == 1) return 0;

        ulong result = 1;
        @base %= modulus;
        while (exponent > 0)
        {
            if ((exponent & 1) == 1) result = (ulong)((UInt128)result * @base % modulus);
            @base = (ulong)((UInt128)@base * @base % modulus);
            exponent >>= 1;
        }
        return result;
    }

    /// <summary>
    /// x such that (a·x) mod m = 1, with the extended Euclidean algorithm. It exists only when a and m share no factor;
    /// this is how an RSA private key is derived from the public one.
    /// </summary>
    public static long ModInverse(long a, long modulus)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(modulus, 2);

        // Invariant: oldR = oldS·a (mod m) and r = s·a (mod m). When r reaches 0, oldR is gcd(a, m).
        long oldR = ((a % modulus) + modulus) % modulus, r = modulus;
        long oldS = 1, s = 0;
        while (r != 0)
        {
            var quotient = oldR / r;
            (oldR, r) = (r, oldR - quotient * r);
            (oldS, s) = (s, oldS - quotient * s);
        }

        if (oldR != 1) throw new ArgumentException($"{a} has no inverse modulo {modulus}: they share the factor {oldR}.");
        return ((oldS % modulus) + modulus) % modulus;
    }

    /// <summary>
    /// Deterministic Miller–Rabin for every 64-bit number. A random-base Miller–Rabin is probabilistic, but testing these
    /// 12 fixed bases has been proven to give the right answer for all n &lt; 3.3·10^24, which covers <see cref="ulong"/>.
    /// Trial division up to √n would take billions of steps for large primes; this takes a few hundred multiplications.
    /// </summary>
    public static bool IsPrime(ulong n)
    {
        if (n < 2) return false;

        ReadOnlySpan<ulong> bases = [2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37];
        foreach (var p in bases)
        {
            if (n == p) return true;
            if (n % p == 0) return false;
        }

        // Write n - 1 as d·2^s with d odd.
        var d = n - 1;
        var s = BitOperations.TrailingZeroCount(d);
        d >>= s;

        foreach (var a in bases)
        {
            var x = ModPow(a, d, n);
            if (x == 1 || x == n - 1) continue;

            var witnessFound = true;
            for (var i = 1; i < s; i++)
            {
                x = (ulong)((UInt128)x * x % n);
                if (x == n - 1)
                {
                    witnessFound = false;
                    break;
                }
            }

            if (witnessFound) return false; // a proves n is composite
        }

        return true;
    }

    /// <summary>
    /// The nth Fibonacci number by "fast doubling": F(2k) = F(k)·(2F(k+1) − F(k)) and F(2k+1) = F(k)² + F(k+1)².
    /// O(log n) steps, so F(10,000), a 2,090-digit number, is instant.
    /// </summary>
    public static BigInteger Fibonacci(int n)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(n);

        BigInteger a = 0, b = 1; // F(k), F(k+1), starting at k = 0
        for (var bit = 31 - BitOperations.LeadingZeroCount((uint)n); bit >= 0; bit--)
        {
            var c = a * (2 * b - a); // F(2k)
            var d = a * a + b * b;   // F(2k+1)
            (a, b) = ((n >> bit & 1) == 0) ? (c, d) : (d, c + d);
        }
        return a;
    }
}
