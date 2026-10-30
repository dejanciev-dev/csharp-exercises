using CSharpExercises.Graphs;

namespace CSharpExercises.Tests;

public class MoreGraphsTests
{
    [Fact]
    public void Eulerian_ReconstructsAnItinerary()
    {
        (string, string)[] tickets = [("JFK", "SFO"), ("JFK", "ATL"), ("SFO", "ATL"), ("ATL", "JFK"), ("ATL", "SFO")];

        var route = EulerianPath.Find(tickets);

        Assert.Equal(["JFK", "ATL", "JFK", "SFO", "ATL", "SFO"], route);
    }

    [Fact]
    public void Eulerian_RejectsImpossibleGraphs()
    {
        Assert.Null(EulerianPath.Find([("a", "b"), ("a", "c")]));             // two vertices with no way in
        Assert.Null(EulerianPath.Find([("a", "b"), ("b", "a"), ("c", "d"), ("d", "c")])); // two separate cycles
        Assert.Equal(["x", "y", "x"], EulerianPath.Find([("x", "y"), ("y", "x")]));
    }

    [Fact]
    public void Prim_MatchesKnownMinimumSpanningTree()
    {
        WeightedEdge[] edges = [new(0, 1, 4), new(0, 7, 8), new(1, 2, 8), new(1, 7, 11), new(2, 3, 7), new(2, 8, 2), new(2, 5, 4),
            new(3, 4, 9), new(3, 5, 14), new(4, 5, 10), new(5, 6, 2), new(6, 7, 1), new(6, 8, 6), new(7, 8, 7)];

        var (weight, chosen) = PrimMst.Build(9, edges)!.Value;

        Assert.Equal(37, weight);
        Assert.Equal(8, chosen.Count);
        Assert.Null(PrimMst.Build(3, [new(0, 1, 1)])); // vertex 2 is unreachable
    }

    private static IReadOnlyList<int>[] Undirected(int n, params (int, int)[] edges)
    {
        var lists = Enumerable.Range(0, n).Select(_ => new List<int>()).ToArray();
        foreach (var (a, b) in edges) { lists[a].Add(b); lists[b].Add(a); }
        return lists;
    }

    [Fact]
    public void Coloring_GreedyIsProperAndOrderMatters()
    {
        // A "crown" graph: bipartite, but a bad order makes greedy use n/2 colours.
        var crown = Undirected(8, (0, 5), (0, 7), (2, 1), (2, 7), (4, 1), (4, 3), (6, 1), (6, 3), (6, 5), (0, 3), (2, 5), (4, 7));

        var badOrder = GraphColoring.Greedy(crown, [0, 1, 2, 3, 4, 5, 6, 7]);
        var welshPowell = GraphColoring.WelshPowell(crown);

        Assert.True(GraphColoring.IsProper(crown, badOrder));
        Assert.True(GraphColoring.IsProper(crown, welshPowell));
        Assert.Equal(4, badOrder.Distinct().Count());
        Assert.Equal(2, GraphColoring.ChromaticNumber(crown));
    }

    [Fact]
    public void Coloring_ChromaticNumbers()
    {
        var triangle = Undirected(3, (0, 1), (1, 2), (2, 0));
        var petersen = Undirected(10, (0, 1), (1, 2), (2, 3), (3, 4), (4, 0), (0, 5), (1, 6), (2, 7), (3, 8), (4, 9), (5, 7), (7, 9), (9, 6), (6, 8), (8, 5));

        Assert.Equal(3, GraphColoring.ChromaticNumber(triangle));
        Assert.Equal(3, GraphColoring.ChromaticNumber(petersen));
        Assert.Equal(1, GraphColoring.ChromaticNumber(Undirected(4)));
    }

    [Fact]
    public void Closure_ReachabilityAndReduction()
    {
        (int, int)[] edges = [(0, 1), (1, 2), (0, 2), (2, 3), (0, 3)];

        var closure = new TransitiveClosure(5, edges);

        Assert.True(closure.CanReach(0, 3));
        Assert.False(closure.CanReach(3, 0));
        Assert.Equal([1, 2, 3], closure.ReachableFrom(0));
        Assert.Empty(closure.ReachableFrom(4));
        Assert.False(closure.HasCycle);
        Assert.Equal([(0, 1), (1, 2), (2, 3)], TransitiveClosure.Reduction(5, edges));
        Assert.True(new TransitiveClosure(2, [(0, 1), (1, 0)]).HasCycle);
    }

    [Fact]
    public void Lca_MatchesNaiveWalkUp()
    {
        var random = new Random(1);
        var parent = new int[500];
        parent[0] = -1;
        for (var v = 1; v < parent.Length; v++) parent[v] = random.Next(v);
        var lca = new LowestCommonAncestor(parent);

        int Naive(int a, int b)
        {
            var ancestors = new HashSet<int>();
            for (var v = a; v != -1; v = parent[v]) ancestors.Add(v);
            for (var v = b; ; v = parent[v]) if (ancestors.Contains(v)) return v;
        }

        for (var i = 0; i < 1000; i++)
        {
            var (a, b) = (random.Next(500), random.Next(500));
            Assert.Equal(Naive(a, b), lca.Query(a, b));
        }
        Assert.Equal(0, lca.Distance(7, 7));
    }

    [Fact]
    public void Tree_DiameterAndCenter()
    {
        //   0 —1— 1 —2— 2 —3— 3
        //         |
        //         5
        //         |
        //         4
        var tree = new TreeMetrics(5, [(0, 1, 1), (1, 2, 2), (2, 3, 3), (1, 4, 5)]);

        var (length, path) = tree.Diameter();

        Assert.Equal(10, length); // 4 → 1 → 2 → 3
        Assert.Equal([4, 1, 2, 3], path);
        Assert.Equal((1, 5L), tree.Center());
        Assert.Throws<ArgumentException>(() => new TreeMetrics(3, [(0, 1, 1)]));
    }
}
