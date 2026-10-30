namespace CSharpExercises.Algorithms;

/// <summary>
/// Longest strictly increasing subsequence in O(n log n) with "patience sorting": tails[k] is the smallest possible
/// last value of an increasing subsequence of length k + 1. Each value either extends the longest one or replaces
/// a tail with a smaller value, found by binary search.
/// </summary>
public static class LongestIncreasingSubsequence
{
    public static IReadOnlyList<int> Find(IReadOnlyList<int> values)
    {
        var tails = new List<int>();       // indices into values
        var previous = new int[values.Count];

        for (var i = 0; i < values.Count; i++)
        {
            int low = 0, high = tails.Count;
            while (low < high)
            {
                var mid = (low + high) / 2;
                if (values[tails[mid]] < values[i]) low = mid + 1;
                else high = mid;
            }

            previous[i] = low > 0 ? tails[low - 1] : -1;
            if (low == tails.Count) tails.Add(i);
            else tails[low] = i;
        }

        var result = new int[tails.Count];
        for (int k = tails.Count - 1, i = tails.Count > 0 ? tails[^1] : -1; k >= 0; k--, i = previous[i])
            result[k] = values[i];
        return result;
    }

    /// <summary>The O(n²) DP that the fast version replaces.</summary>
    public static int LengthQuadratic(IReadOnlyList<int> values)
    {
        var length = new int[values.Count];
        for (var i = 0; i < values.Count; i++)
        {
            length[i] = 1;
            for (var j = 0; j < i; j++)
                if (values[j] < values[i]) length[i] = Math.Max(length[i], length[j] + 1);
        }
        return length.Length == 0 ? 0 : length.Max();
    }

    /// <summary>
    /// Russian-doll envelopes: how many can nest? Sort by width, and for equal widths by height descending so two
    /// envelopes of the same width can't both be picked; then it's LIS on the heights.
    /// </summary>
    public static int MaxNestedEnvelopes(IEnumerable<(int Width, int Height)> envelopes)
    {
        var heights = envelopes.OrderBy(e => e.Width).ThenByDescending(e => e.Height).Select(e => e.Height).ToArray();
        return Find(heights).Count;
    }
}
