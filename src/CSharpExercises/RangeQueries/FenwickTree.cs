namespace CSharpExercises.RangeQueries;

/// <summary>
/// A Fenwick (binary indexed) tree: prefix sums that also allow updates, both in O(log n). Slot i stores the sum of
/// the last (i &amp; -i) values up to i, so walking i by its lowest set bit visits O(log n) slots.
/// </summary>
public sealed class FenwickTree
{
    private readonly long[] _tree; // 1-based

    public FenwickTree(int size) => _tree = new long[size + 1];

    public FenwickTree(IReadOnlyList<long> values) : this(values.Count)
    {
        // O(n) build: push each slot's total up to its parent once, instead of n separate updates.
        for (var i = 1; i <= values.Count; i++)
        {
            _tree[i] += values[i - 1];
            var parent = i + (i & -i);
            if (parent < _tree.Length) _tree[parent] += _tree[i];
        }
    }

    public int Count => _tree.Length - 1;

    public void Add(int index, long delta)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
        for (var i = index + 1; i < _tree.Length; i += i & -i) _tree[i] += delta;
    }

    /// <summary>Sum of the first <paramref name="count"/> values.</summary>
    public long PrefixSum(int count)
    {
        long sum = 0;
        for (var i = count; i > 0; i -= i & -i) sum += _tree[i];
        return sum;
    }

    /// <summary>Sum of the values in [from, to).</summary>
    public long Sum(int from, int to) => PrefixSum(to) - PrefixSum(from);

    /// <summary>
    /// Counts inversions (pairs out of order) in O(n log n): walk from the right and ask how many smaller values
    /// have already been seen. Values are first compressed to ranks so the tree stays small.
    /// </summary>
    public static long CountInversions(IReadOnlyList<int> values)
    {
        var ranks = values.Distinct().Order().Select((v, i) => (v, i)).ToDictionary(p => p.v, p => p.i);
        var seen = new FenwickTree(ranks.Count);
        long inversions = 0;
        for (var i = values.Count - 1; i >= 0; i--)
        {
            var rank = ranks[values[i]];
            inversions += seen.PrefixSum(rank);
            seen.Add(rank, 1);
        }
        return inversions;
    }
}
