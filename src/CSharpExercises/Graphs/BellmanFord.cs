namespace CSharpExercises.Graphs;

public readonly record struct WeightedEdge(int From, int To, long Weight);

/// <summary>
/// Bellman–Ford handles negative edge weights, which break Dijkstra. Relax every edge n - 1 times (a shortest path
/// has at most n - 1 edges); if a further pass still improves something, a negative cycle is reachable.
/// </summary>
public static class BellmanFord
{
    public sealed record Result(long?[] Distances, int[] Previous, IReadOnlyList<int>? NegativeCycle)
    {
        public IReadOnlyList<int> PathTo(int target)
        {
            if (Distances[target] is null) return [];
            var path = new List<int>();
            for (var v = target; v != -1; v = Previous[v]) path.Add(v);
            path.Reverse();
            return path;
        }
    }

    public static Result Run(int vertexCount, IReadOnlyList<WeightedEdge> edges, int source)
    {
        var distances = new long?[vertexCount];
        var previous = new int[vertexCount];
        Array.Fill(previous, -1);
        distances[source] = 0;

        int changed = -1;
        for (var pass = 0; pass < vertexCount; pass++)
        {
            changed = -1;
            foreach (var (from, to, weight) in edges)
            {
                if (distances[from] is not { } d || (distances[to] is { } current && current <= d + weight)) continue;
                distances[to] = d + weight;
                previous[to] = from;
                changed = to;
            }
            if (changed == -1) break; // settled early: nothing more to relax
        }

        return new Result(distances, previous, changed == -1 ? null : TraceCycle(previous, changed, vertexCount));
    }

    /// <summary>
    /// The vertex updated in pass n may only hang off a cycle, so first walk back n steps to be certain we're on it,
    /// then go round once.
    /// </summary>
    private static List<int> TraceCycle(int[] previous, int start, int vertexCount)
    {
        var v = start;
        for (var i = 0; i < vertexCount; i++) v = previous[v];

        var cycle = new List<int> { v };
        for (var u = previous[v]; u != v; u = previous[u]) cycle.Add(u);
        cycle.Add(v);
        cycle.Reverse();
        return cycle;
    }

    /// <summary>
    /// Currency arbitrage: with weights -log(rate), a cycle whose rates multiply to more than 1 is a negative cycle.
    /// </summary>
    public static IReadOnlyList<int>? FindArbitrage(double[,] rates)
    {
        var n = rates.GetLength(0);
        const double Scale = 1e9; // keep integer weights while preserving enough precision
        var edges = new List<WeightedEdge>();
        for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++)
                if (i != j && rates[i, j] > 0)
                    edges.Add(new WeightedEdge(i, j, (long)Math.Round(-Math.Log(rates[i, j]) * Scale)));

        // A virtual source connected to everyone, so a cycle anywhere is reachable.
        for (var i = 0; i < n; i++) edges.Add(new WeightedEdge(n, i, 0));
        return Run(n + 1, edges, n).NegativeCycle;
    }
}
