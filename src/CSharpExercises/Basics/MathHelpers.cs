namespace CSharpExercises.Basics;

/// <summary>
/// Small number-theory exercises: primes, factorials, Fibonacci and GCD.
/// </summary>
public static class MathHelpers
{
    public static bool IsPrime(int n)
    {
        if (n < 2) return false;
        if (n % 2 == 0) return n == 2;

        for (var i = 3; i <= n / i; i += 2)
        {
            if (n % i == 0) return false;
        }

        return true;
    }

    public static long Factorial(int n)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(n);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(n, 20); // 21! overflows long

        long result = 1;
        for (var i = 2; i <= n; i++) result *= i;
        return result;
    }

    public static IEnumerable<long> Fibonacci()
    {
        (long a, long b) = (0, 1);
        while (true)
        {
            yield return a;
            (a, b) = (b, a + b);
        }
    }

    public static int Gcd(int a, int b)
    {
        (a, b) = (Math.Abs(a), Math.Abs(b));
        while (b != 0) (a, b) = (b, a % b);
        return a;
    }
}
