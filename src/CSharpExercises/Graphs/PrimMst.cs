namespace CSharpExercises.Graphs;

/// <summary>
/// Prim's minimum spanning tree: grow one tree from a start vertex, always adding the cheapest edge that connects it to
/// a new vertex. Kruskal (in Algorithms/) sorts all edges and merges forests with union-find; Prim works vertex by
/// vertex with a priority queue and suits dense graphs. Both must give the same total weight.
/// </summary>
public static class PrimMst
{
    public static (long Weight, IReadOnlyList<WeightedEdge> Edges)? Build(int vertexCount, IEnumerable<WeightedEdge> edges)
    {
        var adjacency = Enumerable.Range(0, vertexCount).Select(_ => new List<WeightedEdge>()).ToArray();
        foreach (var e in edges)
        {
            adjacency[e.From].Add(e);
            adjacency[e.To].Add(new WeightedEdge(e.To, e.From, e.Weight)); // undirected
        }

        var inTree = new bool[vertexCount];
        var chosen = new List<WeightedEdge>();
        var frontier = new PriorityQueue<WeightedEdge, long>();
        long total = 0;

        void AddVertex(int v)
        {
            inTree[v] = true;
            foreach (var e in adjacency[v])
                if (!inTree[e.To]) frontier.Enqueue(e, e.Weight);
        }

        AddVertex(0);
        while (frontier.TryDequeue(out var edge, out _) && chosen.Count < vertexCount - 1)
        {
            if (inTree[edge.To]) continue; // both ends already connected: would form a cycle
            chosen.Add(edge);
            total += edge.Weight;
            AddVertex(edge.To);
        }

        return chosen.Count == vertexCount - 1 ? (total, chosen) : null; // null: the graph is disconnected
    }
}
