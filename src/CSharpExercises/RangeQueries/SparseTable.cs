using System.Numerics;

namespace CSharpExercises.RangeQueries;

/// <summary>
/// Range minimum queries in O(1) after O(n log n) preprocessing, for data that never changes. Row k holds the
/// minimum of every window of length 2^k; any range is covered by two (overlapping) windows of the same length,
/// and overlap is harmless because min(x, x) = x.
/// </summary>
public sealed class SparseTable
{
    private readonly int[][] _table;

    public SparseTable(IReadOnlyList<int> values)
    {
        var n = values.Count;
        var levels = n == 0 ? 1 : BitOperations.Log2((uint)n) + 1;
        _table = new int[levels][];
        _table[0] = [.. values];

        for (var k = 1; k < levels; k++)
        {
            var half = 1 << (k - 1);
            var previous = _table[k - 1];
            var row = new int[n - (1 << k) + 1];
            for (var i = 0; i < row.Length; i++) row[i] = Math.Min(previous[i], previous[i + half]);
            _table[k] = row;
        }
    }

    /// <summary>Minimum of the values in [from, to).</summary>
    public int Min(int from, int to)
    {
        if (from < 0 || to > _table[0].Length || from >= to) throw new ArgumentOutOfRangeException(nameof(from));
        var k = BitOperations.Log2((uint)(to - from));
        return Math.Min(_table[k][from], _table[k][to - (1 << k)]);
    }
}
