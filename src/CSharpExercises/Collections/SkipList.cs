using System.Collections;

namespace CSharpExercises.Collections;

/// <summary>
/// A skip list: a sorted linked list with extra "express lanes". Each node gets a random height; searches start on
/// the top lane and drop down when they would overshoot. O(log n) expected for search, insert and delete, with much
/// simpler code than a balanced tree, and no rebalancing (Redis uses skip lists for sorted sets).
/// </summary>
public sealed class SkipList<T> : IEnumerable<T>
{
    private const int MaxLevel = 32;

    private sealed class Node(T value, int height)
    {
        public readonly T Value = value;
        public readonly Node?[] Next = new Node?[height];
    }

    private readonly Node _head = new(default!, MaxLevel);
    private readonly IComparer<T> _comparer;
    private readonly Random _random;
    private int _level = 1;

    public SkipList(IComparer<T>? comparer = null, Random? random = null)
    {
        _comparer = comparer ?? Comparer<T>.Default;
        _random = random ?? new Random();
    }

    public int Count { get; private set; }

    public bool Contains(T value)
    {
        var node = FindPredecessors(value, null).Next[0];
        return node is not null && _comparer.Compare(node.Value, value) == 0;
    }

    /// <summary>Adds the value unless it's already present.</summary>
    public bool Add(T value)
    {
        var update = new Node[MaxLevel];
        var next = FindPredecessors(value, update).Next[0];
        if (next is not null && _comparer.Compare(next.Value, value) == 0) return false;

        var height = RandomHeight();
        if (height > _level)
        {
            for (var i = _level; i < height; i++) update[i] = _head;
            _level = height;
        }

        var node = new Node(value, height);
        for (var i = 0; i < height; i++)
        {
            node.Next[i] = update[i].Next[i];
            update[i].Next[i] = node;
        }
        Count++;
        return true;
    }

    public bool Remove(T value)
    {
        var update = new Node[MaxLevel];
        var node = FindPredecessors(value, update).Next[0];
        if (node is null || _comparer.Compare(node.Value, value) != 0) return false;

        for (var i = 0; i < node.Next.Length; i++) update[i].Next[i] = node.Next[i];
        while (_level > 1 && _head.Next[_level - 1] is null) _level--;
        Count--;
        return true;
    }

    /// <summary>All values in [from, to], found by one search and then a walk along the bottom lane.</summary>
    public IEnumerable<T> Range(T from, T to)
    {
        for (var node = FindPredecessors(from, null).Next[0]; node is not null && _comparer.Compare(node.Value, to) <= 0; node = node.Next[0])
            yield return node.Value;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (var node = _head.Next[0]; node is not null; node = node.Next[0]) yield return node.Value;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>The number of lanes in use, which stays around log2(n).</summary>
    public int Levels => _level;

    /// <summary>Walks down the lanes; the last node before <paramref name="value"/> on each lane goes in <paramref name="update"/>.</summary>
    private Node FindPredecessors(T value, Node[]? update)
    {
        var node = _head;
        for (var i = _level - 1; i >= 0; i--)
        {
            while (node.Next[i] is { } next && _comparer.Compare(next.Value, value) < 0) node = next;
            if (update is not null) update[i] = node;
        }
        return node;
    }

    /// <summary>Each extra level with probability 1/2, so lanes thin out geometrically.</summary>
    private int RandomHeight()
    {
        var height = 1;
        while (height < MaxLevel && _random.Next(2) == 0) height++;
        return height;
    }
}
