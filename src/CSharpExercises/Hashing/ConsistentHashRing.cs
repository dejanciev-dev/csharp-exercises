namespace CSharpExercises.Hashing;

/// <summary>
/// Consistent hashing: picks which server (cache node, database shard) owns a key, so that adding or removing a server
/// moves only the keys that have to move. With <c>hash(key) % serverCount</c>, going from 4 to 5 servers moves about
/// 80% of all keys; here it moves about 20%, all of them to the new server.
/// </summary>
/// <remarks>
/// Servers and keys are hashed onto the same circle of 64-bit numbers, and a key belongs to the first server clockwise
/// from it. Each server is placed many times ("virtual nodes"), which evens out the share of the circle each one gets.
/// </remarks>
public sealed class ConsistentHashRing<TNode> where TNode : notnull
{
    private readonly int _virtualNodes;
    private readonly Func<TNode, string> _nameOf;
    private readonly HashSet<TNode> _nodes = [];
    private ulong[] _points = [];
    private TNode[] _owners = [];

    public ConsistentHashRing(int virtualNodes = 160, Func<TNode, string>? nameOf = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(virtualNodes);
        _virtualNodes = virtualNodes;
        _nameOf = nameOf ?? (node => node.ToString()!);
    }

    public IReadOnlyCollection<TNode> Nodes => _nodes;

    public bool Add(TNode node)
    {
        if (!_nodes.Add(node)) return false;
        Rebuild();
        return true;
    }

    public bool Remove(TNode node)
    {
        if (!_nodes.Remove(node)) return false;
        Rebuild();
        return true;
    }

    public TNode GetNode(string key)
    {
        if (_points.Length == 0) throw new InvalidOperationException("The ring has no nodes.");

        // Binary search for the first point at or after the key's hash; past the end wraps round to the start.
        var index = Array.BinarySearch(_points, StableHash.Of(key));
        if (index < 0) index = ~index;
        return _owners[index == _points.Length ? 0 : index];
    }

    // Adding or removing a node is rare compared to lookups, so rebuild sorted arrays then and keep lookups allocation-free.
    private void Rebuild()
    {
        var points = _nodes
            .SelectMany(node => Enumerable.Range(0, _virtualNodes).Select(i => (Point: StableHash.Of($"{_nameOf(node)}#{i}"), Node: node)))
            .OrderBy(p => p.Point)
            .ToArray();

        _points = [.. points.Select(p => p.Point)];
        _owners = [.. points.Select(p => p.Node)];
    }
}
