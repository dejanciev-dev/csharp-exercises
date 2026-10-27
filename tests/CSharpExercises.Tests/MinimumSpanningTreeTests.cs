using CSharpExercises.Algorithms;

namespace CSharpExercises.Tests;

public class MinimumSpanningTreeTests
{
    // Laying cable between buildings: each edge is the cost of a possible connection.
    private static readonly (string From, string To, int Weight)[] Campus =
    [
        ("Library", "Lab", 7), ("Library", "Gym", 5), ("Lab", "Gym", 9), ("Lab", "Hall", 8),
        ("Lab", "Cafe", 7), ("Gym", "Hall", 15), ("Gym", "Office", 6), ("Hall", "Office", 8),
        ("Hall", "Cafe", 5), ("Office", "Cafe", 9), ("Office", "Dorm", 11), ("Cafe", "Dorm", 9),
    ];

    private static readonly string[] Buildings = ["Library", "Lab", "Gym", "Hall", "Office", "Cafe", "Dorm"];

    [Fact]
    public void Kruskal_FindsTheCheapestNetwork()
    {
        var result = MinimumSpanningTree.Kruskal(Buildings, Campus);

        Assert.NotNull(result);
        Assert.Equal(39, result.Value.TotalWeight);
        Assert.Equal(Buildings.Length - 1, result.Value.Edges.Count); // a tree on n vertices has n - 1 edges
        Assert.DoesNotContain(("Gym", "Hall", 15), result.Value.Edges);
    }

    [Fact]
    public void Kruskal_DisconnectedGraphHasNoSpanningTree()
    {
        Assert.Null(MinimumSpanningTree.Kruskal(["a", "b", "c"], [("a", "b", 1)]));
    }

    [Fact]
    public void Kruskal_SingleVertexNeedsNoEdges()
    {
        var result = MinimumSpanningTree.Kruskal(["only"], []);

        Assert.Equal(0, result!.Value.TotalWeight);
        Assert.Empty(result.Value.Edges);
    }
}
