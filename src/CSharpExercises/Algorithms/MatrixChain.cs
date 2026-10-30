namespace CSharpExercises.Algorithms;

/// <summary>
/// Matrix chain multiplication: (AB)C and A(BC) give the same result but can differ in cost by orders of magnitude.
/// Interval DP tries every place to split each sub-chain, smallest chains first. The same shape of DP solves
/// optimal binary search trees, polygon triangulation and "burst balloons".
/// </summary>
public static class MatrixChain
{
    /// <param name="dimensions">Matrix i is dimensions[i] × dimensions[i + 1].</param>
    public static (long Multiplications, string Parenthesization) Optimal(IReadOnlyList<int> dimensions)
    {
        var n = dimensions.Count - 1;
        if (n < 1) throw new ArgumentException("Need at least one matrix.", nameof(dimensions));

        var cost = new long[n, n];
        var split = new int[n, n];

        for (var length = 2; length <= n; length++)
        {
            for (var i = 0; i + length - 1 < n; i++)
            {
                var j = i + length - 1;
                cost[i, j] = long.MaxValue;
                for (var k = i; k < j; k++)
                {
                    var c = cost[i, k] + cost[k + 1, j] + (long)dimensions[i] * dimensions[k + 1] * dimensions[j + 1];
                    if (c < cost[i, j]) (cost[i, j], split[i, j]) = (c, k);
                }
            }
        }

        string Format(int i, int j) =>
            i == j ? ((char)('A' + i)).ToString() : $"({Format(i, split[i, j])}{Format(split[i, j] + 1, j)})";

        return (cost[0, n - 1], Format(0, n - 1));
    }

    /// <summary>The cost of multiplying strictly left to right, for comparison.</summary>
    public static long LeftToRight(IReadOnlyList<int> dimensions)
    {
        long total = 0;
        for (var k = 1; k < dimensions.Count - 1; k++) total += (long)dimensions[0] * dimensions[k] * dimensions[k + 1];
        return total;
    }
}
