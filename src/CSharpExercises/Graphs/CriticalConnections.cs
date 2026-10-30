namespace CSharpExercises.Graphs;

/// <summary>
/// Bridges (edges whose removal disconnects the network) and articulation points (vertices whose removal does) in
/// one DFS. low[v] is the earliest discovery time reachable from v's subtree using one back edge; if a child can't
/// climb above v, then v (or the edge to the child) is the only way in.
/// </summary>
public static class CriticalConnections
{
    public static (IReadOnlyList<(int, int)> Bridges, IReadOnlyList<int> ArticulationPoints) Find(
        int vertexCount, IEnumerable<(int A, int B)> edges)
    {
        var adjacency = Enumerable.Range(0, vertexCount).Select(_ => new List<(int To, int EdgeId)>()).ToArray();
        var edgeId = 0;
        foreach (var (a, b) in edges)
        {
            adjacency[a].Add((b, edgeId));
            adjacency[b].Add((a, edgeId));
            edgeId++;
        }

        var discovered = new int[vertexCount];
        var low = new int[vertexCount];
        Array.Fill(discovered, -1);
        var bridges = new List<(int, int)>();
        var articulation = new SortedSet<int>();
        var time = 0;

        void Visit(int v, int parentEdge)
        {
            discovered[v] = low[v] = time++;
            var children = 0;

            foreach (var (w, id) in adjacency[v])
            {
                if (id == parentEdge) continue; // skip only the edge we came in on, so parallel edges count as cycles
                if (discovered[w] != -1)
                {
                    low[v] = Math.Min(low[v], discovered[w]);
                    continue;
                }

                children++;
                Visit(w, id);
                low[v] = Math.Min(low[v], low[w]);

                if (low[w] > discovered[v]) bridges.Add((Math.Min(v, w), Math.Max(v, w)));
                if (parentEdge != -1 && low[w] >= discovered[v]) articulation.Add(v);
            }

            // The DFS root is a cut vertex only if it has two independent subtrees.
            if (parentEdge == -1 && children > 1) articulation.Add(v);
        }

        for (var v = 0; v < vertexCount; v++)
            if (discovered[v] == -1) Visit(v, -1);

        bridges.Sort();
        return (bridges, [.. articulation]);
    }
}
