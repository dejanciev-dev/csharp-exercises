using System.Numerics;

namespace CSharpExercises.LinearAlgebra;

/// <summary>
/// Exponentiation by squaring works for matrices too: M^n in O(log n) multiplications. Any linear recurrence becomes
/// a matrix power, so the n-th Fibonacci number, or the number of paths of length n in a graph, takes O(log n).
/// Uses <see cref="BigInteger"/> so results never overflow.
/// </summary>
public static class MatrixPower
{
    public static BigInteger[,] Power(BigInteger[,] m, long exponent)
    {
        var n = m.GetLength(0);
        ArgumentOutOfRangeException.ThrowIfNegative(exponent);

        var result = new BigInteger[n, n];
        for (var i = 0; i < n; i++) result[i, i] = 1;

        var basis = m;
        while (exponent > 0)
        {
            if ((exponent & 1) == 1) result = Multiply(result, basis);
            basis = Multiply(basis, basis);
            exponent >>= 1;
        }
        return result;
    }

    /// <summary>[[1, 1], [1, 0]]^n = [[F(n+1), F(n)], [F(n), F(n-1)]].</summary>
    public static BigInteger Fibonacci(long n) => Power(new BigInteger[,] { { 1, 1 }, { 1, 0 } }, n)[0, 1];

    /// <summary>
    /// Entry (i, j) of A^k counts the walks of exactly k steps from i to j, where A is the adjacency matrix.
    /// </summary>
    public static BigInteger CountWalks(bool[,] adjacency, int from, int to, long steps)
    {
        var n = adjacency.GetLength(0);
        var a = new BigInteger[n, n];
        for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++) a[i, j] = adjacency[i, j] ? 1 : 0;
        return Power(a, steps)[from, to];
    }

    /// <summary>
    /// Tribonacci (each term is the sum of the previous three) to show the pattern generalises: the companion
    /// matrix shifts the window of the last three values by one.
    /// </summary>
    public static BigInteger Tribonacci(long n)
    {
        if (n < 2) return 0;
        if (n == 2) return 1;
        var m = Power(new BigInteger[,] { { 1, 1, 1 }, { 1, 0, 0 }, { 0, 1, 0 } }, n - 2);
        return m[0, 0]; // starting vector (T2, T1, T0) = (1, 0, 0)
    }

    private static BigInteger[,] Multiply(BigInteger[,] a, BigInteger[,] b)
    {
        var n = a.GetLength(0);
        var result = new BigInteger[n, n];
        for (var i = 0; i < n; i++)
            for (var k = 0; k < n; k++)
            {
                if (a[i, k].IsZero) continue;
                for (var j = 0; j < n; j++) result[i, j] += a[i, k] * b[k, j];
            }
        return result;
    }
}
