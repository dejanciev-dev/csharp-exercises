namespace CSharpExercises.Graphs;

/// <summary>
/// Measures of a weighted tree (a network of cables, a phylogenetic tree). The diameter (longest path) takes two
/// searches: the farthest node from anywhere is one end of a longest path, and the farthest node from that is the
/// other end. The centre (the node minimising its farthest distance) lies on that path, good for placing a server.
/// </summary>
public sealed class TreeMetrics
{
    private readonly List<(int To, long Weight)>[] _adjacency;

    public TreeMetrics(int vertexCount, IEnumerable<(int A, int B, long Weight)> edges)
    {
        _adjacency = Enumerable.Range(0, vertexCount).Select(_ => new List<(int, long)>()).ToArray();
        var count = 0;
        foreach (var (a, b, w) in edges)
        {
            _adjacency[a].Add((b, w));
            _adjacency[b].Add((a, w));
            count++;
        }
        if (count != vertexCount - 1) throw new ArgumentException("A tree with n vertices has n - 1 edges.", nameof(edges));
    }

    public (long Length, IReadOnlyList<int> Path) Diameter()
    {
        var (end1, _, _) = Farthest(0);
        var (end2, length, previous) = Farthest(end1);
        var path = new List<int>();
        for (var v = end2; v != -1; v = previous[v]) path.Add(v);
        return (length, path);
    }

    /// <summary>The vertex whose greatest distance to any other vertex is smallest.</summary>
    public (int Vertex, long Eccentricity) Center()
    {
        var (_, path) = Diameter();
        var (_, _, _, fromStart) = Distances(path[0]);
        var (_, _, _, fromEnd) = Distances(path[^1]);
        // Every vertex's farthest point is one of the diameter's ends.
        return path.Select(v => (v, Math.Max(fromStart[v], fromEnd[v]))).MinBy(p => p.Item2);
    }

    private (int Vertex, long Distance, int[] Previous) Farthest(int start)
    {
        var (vertex, distance, previous, _) = Distances(start);
        return (vertex, distance, previous);
    }

    private (int Vertex, long Distance, int[] Previous, long[] All) Distances(int start)
    {
        var distance = Enumerable.Repeat(-1L, _adjacency.Length).ToArray();
        var previous = Enumerable.Repeat(-1, _adjacency.Length).ToArray();
        distance[start] = 0;
        var stack = new Stack<int>([start]); // a tree has one path between any two vertices, so any traversal works
        while (stack.TryPop(out var v))
            foreach (var (to, w) in _adjacency[v])
            {
                if (distance[to] >= 0) continue;
                distance[to] = distance[v] + w;
                previous[to] = v;
                stack.Push(to);
            }
        var farthest = Array.IndexOf(distance, distance.Max());
        return (farthest, distance[farthest], previous, distance);
    }
}
