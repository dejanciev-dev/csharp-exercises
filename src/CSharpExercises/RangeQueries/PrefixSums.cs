namespace CSharpExercises.RangeQueries;

/// <summary>
/// Precompute running totals once in O(n), then answer any "sum of a range" in O(1) as a difference of two totals.
/// The 2D version does the same for rectangles with inclusion–exclusion.
/// </summary>
public sealed class PrefixSums
{
    private readonly long[] _prefix; // _prefix[i] = sum of the first i values, so _prefix[0] = 0 and there are no edge cases

    public PrefixSums(IReadOnlyList<int> values)
    {
        _prefix = new long[values.Count + 1];
        for (var i = 0; i < values.Count; i++) _prefix[i + 1] = _prefix[i] + values[i];
    }

    /// <summary>Sum of the values in [from, to).</summary>
    public long Sum(int from, int to)
    {
        if (from < 0 || to > _prefix.Length - 1 || from > to) throw new ArgumentOutOfRangeException(nameof(from));
        return _prefix[to] - _prefix[from];
    }

    /// <summary>
    /// How many contiguous subarrays add up to <paramref name="target"/>: a subarray ending here sums to target when
    /// an earlier prefix equals (current prefix - target). Works with negative numbers, unlike a sliding window.
    /// </summary>
    public static int CountSubarraysWithSum(IReadOnlyList<int> values, long target)
    {
        var seen = new Dictionary<long, int> { [0] = 1 };
        long prefix = 0;
        var count = 0;
        foreach (var value in values)
        {
            prefix += value;
            count += seen.GetValueOrDefault(prefix - target);
            seen[prefix] = seen.GetValueOrDefault(prefix) + 1;
        }
        return count;
    }
}

public sealed class PrefixSums2D
{
    private readonly long[,] _prefix;

    public PrefixSums2D(int[,] grid)
    {
        var rows = grid.GetLength(0);
        var cols = grid.GetLength(1);
        _prefix = new long[rows + 1, cols + 1];
        for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++)
                _prefix[r + 1, c + 1] = grid[r, c] + _prefix[r, c + 1] + _prefix[r + 1, c] - _prefix[r, c];
    }

    /// <summary>Sum of the rectangle with rows [top, bottom) and columns [left, right).</summary>
    public long Sum(int top, int left, int bottom, int right) =>
        _prefix[bottom, right] - _prefix[top, right] - _prefix[bottom, left] + _prefix[top, left];
}
