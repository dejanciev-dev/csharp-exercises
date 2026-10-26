using CSharpExercises.Algorithms;

namespace CSharpExercises.Tests;

public class DijkstraTests
{
    // One-way roads. The cheapest route A -> E is A -> C -> B -> D -> E (10), not the shorter-looking A -> C -> E (12).
    private static readonly (string From, string To, int Weight)[] Roads =
    [
        ("A", "B", 4),
        ("A", "C", 2),
        ("C", "B", 1),
        ("B", "D", 5),
        ("C", "D", 8),
        ("C", "E", 10),
        ("D", "E", 2)
    ];

    [Fact]
    public void Distances_FromSource()
    {
        var distances = Dijkstra.Distances(Roads, "A");

        Assert.Equal(0, distances["A"]);
        Assert.Equal(2, distances["C"]);
        Assert.Equal(3, distances["B"]); // A -> C -> B beats the direct A -> B
        Assert.Equal(8, distances["D"]);
        Assert.Equal(10, distances["E"]);
    }

    [Fact]
    public void ShortestPath_PrefersCheaperRouteOverFewerEdges()
    {
        var result = Dijkstra.ShortestPath(Roads, "A", "E");

        Assert.NotNull(result);
        Assert.Equal(10, result.Value.Distance);
        Assert.Equal(["A", "C", "B", "D", "E"], result.Value.Path);
    }

    [Fact]
    public void ShortestPath_ToSelf()
    {
        var result = Dijkstra.ShortestPath(Roads, "B", "B");

        Assert.Equal(0, result?.Distance);
        Assert.Equal(["B"], result?.Path);
    }

    [Fact]
    public void ShortestPath_UnreachableReturnsNull()
    {
        Assert.Null(Dijkstra.ShortestPath(Roads, "E", "A")); // edges are one-way
        Assert.Null(Dijkstra.ShortestPath(Roads, "A", "Z"));
    }

    [Fact]
    public void NegativeWeight_Throws() =>
        Assert.Throws<ArgumentException>(() => Dijkstra.Distances([(1, 2, 3), (2, 3, -1)], 1));
}
