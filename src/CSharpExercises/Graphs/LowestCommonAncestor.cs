using System.Numerics;

namespace CSharpExercises.Graphs;

/// <summary>
/// Lowest common ancestor queries on a rooted tree (org charts, file systems, taxonomies, version history) with binary
/// lifting: store each node's 1st, 2nd, 4th, 8th… ancestor, then lift the deeper node to the same depth and lift both
/// in decreasing jumps while their ancestors differ. O(n log n) setup, O(log n) per query. Distances between nodes
/// follow from depths.
/// </summary>
public sealed class LowestCommonAncestor
{
    private readonly int[][] _up; // _up[k][v] = the 2^k-th ancestor of v (the root is its own ancestor)
    private readonly int[] _depth;

    /// <param name="parent">parent[v] for every node, and -1 for the root.</param>
    public LowestCommonAncestor(IReadOnlyList<int> parent)
    {
        var n = parent.Count;
        var levels = Math.Max(1, BitOperations.Log2((uint)Math.Max(n, 1)) + 1);
        _depth = new int[n];
        var root = Enumerable.Range(0, n).Single(v => parent[v] == -1);

        _up = new int[levels][];
        _up[0] = parent.Select((p, v) => p == -1 ? v : p).ToArray();
        for (var k = 1; k < levels; k++) _up[k] = Enumerable.Range(0, n).Select(v => _up[k - 1][_up[k - 1][v]]).ToArray();

        // Depths by walking down from the root (children lists avoid deep recursion).
        var children = Enumerable.Range(0, n).Select(_ => new List<int>()).ToArray();
        for (var v = 0; v < n; v++) if (parent[v] >= 0) children[parent[v]].Add(v);
        var queue = new Queue<int>([root]);
        while (queue.TryDequeue(out var v))
            foreach (var c in children[v])
            {
                _depth[c] = _depth[v] + 1;
                queue.Enqueue(c);
            }
    }

    public int Depth(int v) => _depth[v];

    public int Query(int a, int b)
    {
        if (_depth[a] < _depth[b]) (a, b) = (b, a);
        for (var k = _up.Length - 1; k >= 0; k--)
            if (_depth[a] - (1 << k) >= _depth[b]) a = _up[k][a]; // lift a to b's depth
        if (a == b) return a;

        for (var k = _up.Length - 1; k >= 0; k--)
            if (_up[k][a] != _up[k][b]) (a, b) = (_up[k][a], _up[k][b]);
        return _up[0][a];
    }

    public int Distance(int a, int b) => _depth[a] + _depth[b] - 2 * _depth[Query(a, b)];
}
