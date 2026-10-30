using System.Collections;

namespace CSharpExercises.Collections;

/// <summary>
/// A bag (multiset): a set that counts how many times each item was added, like Python's Counter. Multiset union takes
/// the larger count, intersection the smaller, sum adds counts, and difference subtracts without going below zero,
/// which answers questions like "can these letters spell that word?" (a ransom note).
/// </summary>
public sealed class Bag<T> : IEnumerable<KeyValuePair<T, int>> where T : notnull
{
    private readonly Dictionary<T, int> _counts;

    public Bag(IEqualityComparer<T>? comparer = null) => _counts = new Dictionary<T, int>(comparer);

    public Bag(IEnumerable<T> items, IEqualityComparer<T>? comparer = null) : this(comparer)
    {
        foreach (var item in items) Add(item);
    }

    public int this[T item] => _counts.GetValueOrDefault(item);

    /// <summary>Number of items counting repeats.</summary>
    public int Total => _counts.Values.Sum();

    public int DistinctCount => _counts.Count;

    public void Add(T item, int count = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count > 0) _counts[item] = this[item] + count;
    }

    /// <summary>Removes up to <paramref name="count"/> copies and returns how many were actually removed.</summary>
    public int Remove(T item, int count = 1)
    {
        var current = this[item];
        var removed = Math.Min(current, count);
        if (current - removed == 0) _counts.Remove(item);
        else _counts[item] = current - removed;
        return removed;
    }

    public IReadOnlyList<(T Item, int Count)> MostCommon(int n) =>
        _counts.OrderByDescending(p => p.Value).Take(n).Select(p => (p.Key, p.Value)).ToList();

    public Bag<T> Union(Bag<T> other) => Combine(other, Math.Max);
    public Bag<T> Intersect(Bag<T> other) => Combine(other, Math.Min);
    public Bag<T> Sum(Bag<T> other) => Combine(other, (a, b) => a + b);
    public Bag<T> Except(Bag<T> other) => Combine(other, (a, b) => Math.Max(0, a - b));

    /// <summary>Every item occurs at least as often here as in <paramref name="other"/>.</summary>
    public bool Contains(Bag<T> other) => other._counts.All(p => this[p.Key] >= p.Value);

    public IEnumerable<T> Expand() => _counts.SelectMany(p => Enumerable.Repeat(p.Key, p.Value));

    public IEnumerator<KeyValuePair<T, int>> GetEnumerator() => _counts.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private Bag<T> Combine(Bag<T> other, Func<int, int, int> op)
    {
        var result = new Bag<T>(_counts.Comparer);
        foreach (var key in _counts.Keys.Union(other._counts.Keys, _counts.Comparer)) result.Add(key, op(this[key], other[key]));
        return result;
    }
}
