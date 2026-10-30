namespace CSharpExercises.Graphs;

/// <summary>
/// All-pairs shortest paths in O(n³) with three nested loops: after step k, distance[i, j] is the shortest path that
/// only passes through vertices 0..k on the way. Simple and fast for dense graphs of a few hundred vertices.
/// </summary>
public sealed class FloydWarshall
{
    private readonly long?[,] _distance;
    private readonly int[,] _next;

    public FloydWarshall(int vertexCount, IEnumerable<WeightedEdge> edges)
    {
        _distance = new long?[vertexCount, vertexCount];
        _next = new int[vertexCount, vertexCount];

        for (var i = 0; i < vertexCount; i++)
        {
            for (var j = 0; j < vertexCount; j++) _next[i, j] = -1;
            _distance[i, i] = 0;
            _next[i, i] = i;
        }

        foreach (var (from, to, weight) in edges)
        {
            if (_distance[from, to] is { } existing && existing <= weight) continue; // keep the cheapest parallel edge
            _distance[from, to] = weight;
            _next[from, to] = to;
        }

        for (var k = 0; k < vertexCount; k++)
            for (var i = 0; i < vertexCount; i++)
            {
                if (_distance[i, k] is not { } ik) continue;
                for (var j = 0; j < vertexCount; j++)
                {
                    if (_distance[k, j] is not { } kj) continue;
                    if (_distance[i, j] is { } ij && ij <= ik + kj) continue;
                    _distance[i, j] = ik + kj;
                    _next[i, j] = _next[i, k];
                }
            }

        for (var i = 0; i < vertexCount; i++)
            if (_distance[i, i] < 0) HasNegativeCycle = true;
    }

    public bool HasNegativeCycle { get; }

    public long? Distance(int from, int to) => _distance[from, to];

    /// <summary>Follows "next hop" pointers, the same idea as a router's forwarding table.</summary>
    public IReadOnlyList<int> Path(int from, int to)
    {
        if (_next[from, to] == -1) return [];
        var path = new List<int> { from };
        while (from != to)
        {
            from = _next[from, to];
            path.Add(from);
        }
        return path;
    }

    /// <summary>The vertex whose farthest vertex is nearest: the best place for a warehouse or a server.</summary>
    public int? Center()
    {
        var n = _distance.GetLength(0);
        int? best = null;
        var bestEccentricity = long.MaxValue;
        for (var i = 0; i < n; i++)
        {
            long eccentricity = 0;
            for (var j = 0; j < n && eccentricity != long.MaxValue; j++)
                eccentricity = _distance[i, j] is { } d ? Math.Max(eccentricity, d) : long.MaxValue;
            if (eccentricity < bestEccentricity) (best, bestEccentricity) = (i, eccentricity);
        }
        return best;
    }
}
