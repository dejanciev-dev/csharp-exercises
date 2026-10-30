using CSharpExercises.Graphs;

namespace CSharpExercises.Tests;

public class GraphAlgorithmsTests
{
    private static IReadOnlyList<int>[] Adjacency(int n, params (int From, int To)[] edges)
    {
        var lists = Enumerable.Range(0, n).Select(_ => new List<int>()).ToArray();
        foreach (var (from, to) in edges) lists[from].Add(to);
        return lists;
    }

    private static IReadOnlyList<int>[] Undirected(int n, params (int A, int B)[] edges) =>
        Adjacency(n, [.. edges, .. edges.Select(e => (e.B, e.A))]);

    [Fact]
    public void Scc_FindsComponentsInReverseTopologicalOrder()
    {
        // 0 → 1 → 2 → 0 is a cycle, 2 → 3, and 3 ↔ 4.
        var graph = Adjacency(5, (0, 1), (1, 2), (2, 0), (2, 3), (3, 4), (4, 3));

        var components = StronglyConnectedComponents.Find(graph);

        Assert.Equal([[3, 4], [0, 1, 2]], components);
        var ids = StronglyConnectedComponents.ComponentIds(graph);
        Assert.Equal(ids[0], ids[2]);
        Assert.NotEqual(ids[0], ids[3]);
    }

    [Fact]
    public void BellmanFord_HandlesNegativeWeights()
    {
        WeightedEdge[] edges = [new(0, 1, 4), new(0, 2, 5), new(1, 3, -3), new(2, 1, -2), new(3, 4, 2)];

        var result = BellmanFord.Run(5, edges, 0);

        Assert.Equal([0L, 3, 5, 0, 2], result.Distances);
        Assert.Equal([0, 2, 1, 3, 4], result.PathTo(4));
        Assert.Null(result.NegativeCycle);
    }

    [Fact]
    public void BellmanFord_DetectsNegativeCycle()
    {
        WeightedEdge[] edges = [new(0, 1, 1), new(1, 2, -1), new(2, 3, -1), new(3, 1, 1), new(3, 4, 1)];

        var cycle = BellmanFord.Run(5, edges, 0).NegativeCycle;

        Assert.NotNull(cycle);
        Assert.Equal(cycle[0], cycle[^1]);
        Assert.Equal(new HashSet<int> { 1, 2, 3 }, cycle.ToHashSet());
    }

    [Fact]
    public void BellmanFord_FindsCurrencyArbitrage()
    {
        // USD → EUR → GBP → USD multiplies to 0.9 · 0.9 · 1.3 = 1.053
        double[,] rates =
        {
            { 1, 0.9, 0.75 },
            { 1.1, 1, 0.9 },
            { 1.3, 1.1, 1 },
        };

        Assert.NotNull(BellmanFord.FindArbitrage(rates));
        Assert.Null(BellmanFord.FindArbitrage(new double[,] { { 1, 0.5 }, { 1.9, 1 } }));
    }

    [Fact]
    public void FloydWarshall_AllPairsWithPaths()
    {
        var fw = new FloydWarshall(4, [new(0, 1, 5), new(0, 3, 10), new(1, 2, 3), new(2, 3, 1)]);

        Assert.Equal(9, fw.Distance(0, 3));
        Assert.Equal([0, 1, 2, 3], fw.Path(0, 3));
        Assert.Null(fw.Distance(3, 0));
        Assert.Empty(fw.Path(3, 0));
        Assert.False(fw.HasNegativeCycle);
    }

    [Fact]
    public void FloydWarshall_FindsTheCenter()
    {
        // A path 0 - 1 - 2 - 3 - 4 (both directions): the middle vertex is closest to the farthest one.
        var edges = Enumerable.Range(0, 4).SelectMany(i => new WeightedEdge[] { new(i, i + 1, 1), new(i + 1, i, 1) });

        Assert.Equal(2, new FloydWarshall(5, edges).Center());
    }

    [Fact]
    public void Bipartite_ColoursEvenCyclesOnly()
    {
        var square = Undirected(4, (0, 1), (1, 2), (2, 3), (3, 0));
        var triangle = Undirected(3, (0, 1), (1, 2), (2, 0));

        Assert.Equal([0, 1, 0, 1], Bipartite.TwoColour(square)!);
        Assert.Null(Bipartite.TwoColour(triangle));
    }

    [Fact]
    public void Bipartite_MaximumMatching()
    {
        // Three applicants, three jobs; a greedy first choice would leave someone out.
        IReadOnlyList<int>[] likes = [[0, 1], [0], [1, 2]];

        var match = Bipartite.MaximumMatching(likes, 3);

        Assert.Equal([1, 0, 2], match);
    }

    [Fact]
    public void MaxFlow_EqualsMinCut()
    {
        var flow = new MaxFlow(6);
        flow.AddEdge(0, 1, 16);
        flow.AddEdge(0, 2, 13);
        flow.AddEdge(1, 2, 10);
        flow.AddEdge(2, 1, 4);
        flow.AddEdge(1, 3, 12);
        flow.AddEdge(3, 2, 9);
        flow.AddEdge(2, 4, 14);
        flow.AddEdge(4, 3, 7);
        flow.AddEdge(3, 5, 20);
        flow.AddEdge(4, 5, 4);

        Assert.Equal(23, flow.Run(0, 5));

        // The saturated edges 12 + 7 + 4 = 23 separate the source from the sink.
        Assert.Equal([(1, 3), (4, 3), (4, 5)], flow.MinCut(0));
        Assert.Equal(12, flow.FlowOn(1, 3));
    }

    [Fact]
    public void AStar_FindsTheCheapestPathAndExpandsLess()
    {
        string[] grid =
        [
            "S.........",
            ".########.",
            ".#......#.",
            ".#.####.#.",
            "...#..9...",
            "####.####.",
            ".........G",
        ];
        var finder = new GridPathfinding(grid);

        var aStar = finder.FindPath((0, 0), (6, 9));
        var expandedByAStar = finder.Expanded;
        var dijkstra = finder.FindPath((0, 0), (6, 9), useHeuristic: false);

        Assert.NotNull(aStar);
        Assert.Equal(dijkstra!.Value.Cost, aStar.Value.Cost);
        Assert.Equal(15, aStar.Value.Cost);
        Assert.Equal((0, 0), aStar.Value.Path[0]);
        Assert.Equal((6, 9), aStar.Value.Path[^1]);
        Assert.True(expandedByAStar < finder.Expanded);
    }

    [Fact]
    public void AStar_ReturnsNullWhenBlocked()
    {
        Assert.Null(new GridPathfinding([".#.", ".#.", ".#."]).FindPath((0, 0), (0, 2)));
    }

    [Fact]
    public void CriticalConnections_BridgesAndCutVertices()
    {
        // Triangle 0-1-2, bridge 1-3, then triangle 3-4-5 and a dangling 5-6.
        (int, int)[] edges = [(0, 1), (1, 2), (2, 0), (1, 3), (3, 4), (4, 5), (5, 3), (5, 6)];

        var (bridges, points) = CriticalConnections.Find(7, edges);

        Assert.Equal([(1, 3), (5, 6)], bridges);
        Assert.Equal([1, 3, 5], points);
    }

    [Fact]
    public void CriticalConnections_ParallelEdgesAreNotBridges()
    {
        var (bridges, _) = CriticalConnections.Find(2, [(0, 1), (0, 1)]);
        Assert.Empty(bridges);
    }
}
