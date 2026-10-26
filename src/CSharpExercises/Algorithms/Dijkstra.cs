using CSharpExercises.Collections;

namespace CSharpExercises.Algorithms;

/// <summary>
/// Dijkstra's shortest-path algorithm on a directed graph with non-negative edge weights,
/// using the hand-written <see cref="MinHeap{T}"/> as its priority queue.
/// For an undirected graph, add each edge in both directions.
/// </summary>
public static class Dijkstra
{
    /// <summary>Distance from <paramref name="source"/> to every reachable vertex.</summary>
    public static Dictionary<T, int> Distances<T>(IEnumerable<(T From, T To, int Weight)> edges, T source)
        where T : notnull =>
        Run(edges, source).Distances;

    /// <summary>Cheapest route from <paramref name="source"/> to <paramref name="target"/>, or null if it is unreachable.</summary>
    public static (int Distance, IReadOnlyList<T> Path)? ShortestPath<T>(
        IEnumerable<(T From, T To, int Weight)> edges, T source, T target)
        where T : notnull
    {
        var (distances, previous) = Run(edges, source);
        if (!distances.TryGetValue(target, out var distance)) return null;

        var path = new List<T> { target };
        while (previous.TryGetValue(path[^1], out var step)) path.Add(step);
        path.Reverse();

        return (distance, path);
    }

    private static (Dictionary<T, int> Distances, Dictionary<T, T> Previous) Run<T>(
        IEnumerable<(T From, T To, int Weight)> edges, T source)
        where T : notnull
    {
        var adjacency = new Dictionary<T, List<(T To, int Weight)>>();
        foreach (var (from, to, weight) in edges)
        {
            if (weight < 0) throw new ArgumentException("Dijkstra's algorithm requires non-negative weights.", nameof(edges));

            if (!adjacency.TryGetValue(from, out var outgoing)) adjacency[from] = outgoing = [];
            outgoing.Add((to, weight));
        }

        var distances = new Dictionary<T, int> { [source] = 0 };
        var previous = new Dictionary<T, T>();
        var settled = new HashSet<T>();
        var queue = new MinHeap<(int Distance, T Vertex)>(
            Comparer<(int Distance, T Vertex)>.Create((a, b) => a.Distance.CompareTo(b.Distance)));
        queue.Push((0, source));

        while (!queue.IsEmpty)
        {
            var (distance, vertex) = queue.Pop();

            // A vertex can be queued several times; only its first (cheapest) pop counts.
            if (!settled.Add(vertex)) continue;
            if (!adjacency.TryGetValue(vertex, out var outgoing)) continue;

            foreach (var (to, weight) in outgoing)
            {
                var candidate = distance + weight;
                if (distances.TryGetValue(to, out var known) && known <= candidate) continue;

                distances[to] = candidate;
                previous[to] = vertex;
                queue.Push((candidate, to));
            }
        }

        return (distances, previous);
    }
}
