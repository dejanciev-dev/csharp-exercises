using CSharpExercises.Collections;

namespace CSharpExercises.Algorithms;

/// <summary>
/// Kruskal's algorithm: the cheapest set of edges that connects every vertex of an undirected graph,
/// e.g. the least cable needed to network a set of buildings.
/// </summary>
public static class MinimumSpanningTree
{
    /// <summary>
    /// Takes edges cheapest first and keeps each one that joins two parts not yet connected.
    /// <see cref="UnionFind{T}"/> answers "already connected?" in near-constant time, so sorting the edges dominates: O(E log E).
    /// Returns null if the graph is not connected, since no spanning tree exists then.
    /// </summary>
    public static (int TotalWeight, IReadOnlyList<(T From, T To, int Weight)> Edges)? Kruskal<T>(
        IEnumerable<T> vertices, IEnumerable<(T From, T To, int Weight)> edges)
        where T : notnull
    {
        var components = new UnionFind<T>();
        foreach (var vertex in vertices) components.Add(vertex);

        List<(T From, T To, int Weight)> chosen = [];
        foreach (var edge in edges.OrderBy(e => e.Weight))
        {
            // Union returns false when both ends are already connected: this edge would make a cycle.
            if (components.Union(edge.From, edge.To)) chosen.Add(edge);
            if (components.Groups == 1) break;
        }

        return components.Groups <= 1 ? (chosen.Sum(e => e.Weight), chosen) : null;
    }
}
