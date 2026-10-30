namespace CSharpExercises.RangeQueries;

/// <summary>
/// A segment tree for any associative operation with an identity (sum, min, max, gcd…): point updates and range
/// queries in O(log n). Stored bottom-up in an array of 2n: leaves at [n, 2n), node i's children at 2i and 2i + 1.
/// </summary>
public sealed class SegmentTree<T>
{
    private readonly T[] _tree;
    private readonly int _n;
    private readonly Func<T, T, T> _combine;
    private readonly T _identity;

    public SegmentTree(IReadOnlyList<T> values, Func<T, T, T> combine, T identity)
    {
        _n = values.Count;
        _combine = combine;
        _identity = identity;
        _tree = new T[2 * _n];
        for (var i = 0; i < _n; i++) _tree[_n + i] = values[i];
        for (var i = _n - 1; i > 0; i--) _tree[i] = combine(_tree[2 * i], _tree[2 * i + 1]);
    }

    public T this[int index] => _tree[_n + index];

    public void Set(int index, T value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _n);
        var i = index + _n;
        _tree[i] = value;
        for (i /= 2; i > 0; i /= 2) _tree[i] = _combine(_tree[2 * i], _tree[2 * i + 1]);
    }

    /// <summary>
    /// Combines the values in [from, to). Both ends climb towards the root; an end that is a right child (or a left
    /// child, for the right end) contributes its node and steps inward. Left and right results are kept separate so
    /// non-commutative operations still combine in order.
    /// </summary>
    public T Query(int from, int to)
    {
        if (from < 0 || to > _n || from > to) throw new ArgumentOutOfRangeException(nameof(from));

        var left = _identity;
        var right = _identity;
        for (int l = from + _n, r = to + _n; l < r; l /= 2, r /= 2)
        {
            if ((l & 1) == 1) left = _combine(left, _tree[l++]);
            if ((r & 1) == 1) right = _combine(_tree[--r], right);
        }
        return _combine(left, right);
    }
}

public static class SegmentTree
{
    public static SegmentTree<long> ForSum(IReadOnlyList<long> values) => new(values, (a, b) => a + b, 0);
    public static SegmentTree<int> ForMin(IReadOnlyList<int> values) => new(values, Math.Min, int.MaxValue);
    public static SegmentTree<int> ForMax(IReadOnlyList<int> values) => new(values, Math.Max, int.MinValue);
}
