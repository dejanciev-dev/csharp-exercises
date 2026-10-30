namespace CSharpExercises.Graphs;

/// <summary>
/// Maximum flow with Edmonds–Karp: keep finding the shortest path with spare capacity (BFS) and push as much as it
/// allows. Every push also adds reverse capacity, so a later path can undo a bad earlier choice. By the max-flow
/// min-cut theorem, the final flow equals the capacity of the cheapest set of edges that separates source and sink.
/// </summary>
public sealed class MaxFlow
{
    private readonly long[,] _capacity;
    private readonly long[,] _flow;
    private readonly int _n;

    public MaxFlow(int vertexCount)
    {
        _n = vertexCount;
        _capacity = new long[vertexCount, vertexCount];
        _flow = new long[vertexCount, vertexCount];
    }

    public void AddEdge(int from, int to, long capacity) => _capacity[from, to] += capacity;

    public long Run(int source, int sink)
    {
        long total = 0;
        var parent = new int[_n];

        while (true)
        {
            Array.Fill(parent, -1);
            parent[source] = source;
            var queue = new Queue<int>([source]);
            while (queue.TryDequeue(out var v) && parent[sink] == -1)
            {
                for (var w = 0; w < _n; w++)
                {
                    if (parent[w] != -1 || Residual(v, w) <= 0) continue;
                    parent[w] = v;
                    queue.Enqueue(w);
                }
            }
            if (parent[sink] == -1) return total;

            var push = long.MaxValue;
            for (var v = sink; v != source; v = parent[v]) push = Math.Min(push, Residual(parent[v], v));
            for (var v = sink; v != source; v = parent[v])
            {
                _flow[parent[v], v] += push;
                _flow[v, parent[v]] -= push;
            }
            total += push;
        }
    }

    public long FlowOn(int from, int to) => Math.Max(0, _flow[from, to]);

    /// <summary>After <see cref="Run"/>: the edges from the source-reachable side to the rest, which are all saturated.</summary>
    public IReadOnlyList<(int From, int To)> MinCut(int source)
    {
        var reachable = new bool[_n];
        var queue = new Queue<int>([source]);
        reachable[source] = true;
        while (queue.TryDequeue(out var v))
        {
            for (var w = 0; w < _n; w++)
            {
                if (reachable[w] || Residual(v, w) <= 0) continue;
                reachable[w] = true;
                queue.Enqueue(w);
            }
        }

        var cut = new List<(int, int)>();
        for (var v = 0; v < _n; v++)
            for (var w = 0; w < _n; w++)
                if (reachable[v] && !reachable[w] && _capacity[v, w] > 0) cut.Add((v, w));
        return cut;
    }

    private long Residual(int from, int to) => _capacity[from, to] - _flow[from, to];
}
