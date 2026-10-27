namespace CSharpExercises.Collections;

/// <summary>
/// Disjoint-set forest: keeps track of which items are in the same group as groups are merged.
/// With union by size and path compression, each operation takes nearly constant time in practice
/// (the inverse Ackermann function, which is below 5 for any input that fits in memory).
/// </summary>
public sealed class UnionFind<T> where T : notnull
{
    private readonly Dictionary<T, T> _parent = new();
    private readonly Dictionary<T, int> _size = new();

    /// <summary>How many separate groups there are.</summary>
    public int Groups { get; private set; }

    public int Count => _parent.Count;

    /// <summary>Adds <paramref name="item"/> as a group of its own. Returns false if it was already known.</summary>
    public bool Add(T item)
    {
        if (!_parent.TryAdd(item, item)) return false;
        _size[item] = 1;
        Groups++;
        return true;
    }

    /// <summary>The representative of the item's group. Two items are connected if they have the same root.</summary>
    public T Find(T item)
    {
        if (!_parent.ContainsKey(item)) throw new KeyNotFoundException($"'{item}' has not been added.");

        var root = item;
        while (!EqualityComparer<T>.Default.Equals(_parent[root], root)) root = _parent[root];

        // Path compression: point every node on the way straight at the root, so the next Find is one step.
        while (!EqualityComparer<T>.Default.Equals(item, root))
        {
            var next = _parent[item];
            _parent[item] = root;
            item = next;
        }

        return root;
    }

    /// <summary>Merges the groups of <paramref name="a"/> and <paramref name="b"/>. Returns false if they were already together.</summary>
    public bool Union(T a, T b)
    {
        Add(a);
        Add(b);

        var rootA = Find(a);
        var rootB = Find(b);
        if (EqualityComparer<T>.Default.Equals(rootA, rootB)) return false;

        // Union by size: hang the smaller tree under the larger, so trees stay shallow.
        if (_size[rootA] < _size[rootB]) (rootA, rootB) = (rootB, rootA);
        _parent[rootB] = rootA;
        _size[rootA] += _size[rootB];
        Groups--;
        return true;
    }

    public bool Connected(T a, T b) =>
        _parent.ContainsKey(a) && _parent.ContainsKey(b) && EqualityComparer<T>.Default.Equals(Find(a), Find(b));

    public int SizeOfGroup(T item) => _size[Find(item)];
}
