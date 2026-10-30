using System.Numerics;

namespace CSharpExercises.Combinatorics;

/// <summary>
/// Integer partitions: ways to write n as a sum of positive integers, order ignoring (4 = 3+1 = 2+2 = 2+1+1 = …).
/// Counting uses the coin-change DP with every number as a coin; listing generates them in descending order.
/// Stirling numbers of the second kind count ways to split n labelled items into k non-empty groups.
/// </summary>
public static class Partitions
{
    public static BigInteger Count(int n)
    {
        var ways = new BigInteger[n + 1];
        ways[0] = 1;
        for (var part = 1; part <= n; part++)
            for (var total = part; total <= n; total++)
                ways[total] += ways[total - part];
        return ways[n];
    }

    /// <summary>All partitions, each with its parts in non-increasing order, largest first.</summary>
    public static IEnumerable<int[]> All(int n)
    {
        IEnumerable<int[]> From(int remaining, int maxPart)
        {
            if (remaining == 0) { yield return []; yield break; }
            for (var part = Math.Min(remaining, maxPart); part >= 1; part--)
                foreach (var rest in From(remaining - part, part))
                    yield return [part, .. rest];
        }
        return From(n, n);
    }

    /// <summary>S(n, k) = k·S(n-1, k) + S(n-1, k-1): item n either joins one of k groups or starts its own.</summary>
    public static BigInteger Stirling2(int n, int k)
    {
        var row = new BigInteger[k + 1];
        row[0] = 1;
        for (var i = 1; i <= n; i++)
        {
            for (var j = Math.Min(i, k); j >= 1; j--)
                row[j] = j * row[j] + row[j - 1];
            row[0] = 0; // S(i, 0) = 0 once there is at least one item
        }
        return row[k];
    }

    /// <summary>Bell numbers: all ways to split n labelled items into groups of any number.</summary>
    public static BigInteger Bell(int n) => Enumerable.Range(0, n + 1).Aggregate(BigInteger.Zero, (sum, k) => sum + Stirling2(n, k));
}
