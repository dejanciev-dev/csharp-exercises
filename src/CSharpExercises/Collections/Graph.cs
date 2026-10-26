namespace CSharpExercises.Collections;

/// <summary>
/// A graph stored as an adjacency list, with breadth-first and depth-first traversal,
/// unweighted shortest path and topological sort.
/// </summary>
public class Graph<T>(bool directed = false) where T : notnull
{
    private readonly Dictionary<T, List<T>> _adjacency = new();

    public bool IsDirected { get; } = directed;

    public IEnumerable<T> Vertices => _adjacency.Keys;

    public void AddVertex(T vertex) => _adjacency.TryAdd(vertex, []);

    public void AddEdge(T from, T to)
    {
        AddVertex(from);
        AddVertex(to);

        if (!_adjacency[from].Contains(to)) _adjacency[from].Add(to);
        if (!IsDirected && !_adjacency[to].Contains(from)) _adjacency[to].Add(from);
    }

    public IReadOnlyList<T> Neighbors(T vertex) =>
        _adjacency.TryGetValue(vertex, out var neighbors) ? neighbors : [];

    /// <summary>Visits vertices level by level, nearest first, using a queue.</summary>
    public IEnumerable<T> BreadthFirst(T start)
    {
        if (!_adjacency.ContainsKey(start)) yield break;

        var visited = new HashSet<T> { start };
        var queue = new Queue<T>([start]);
        while (queue.TryDequeue(out var vertex))
        {
            yield return vertex;
            foreach (var next in _adjacency[vertex])
            {
                if (visited.Add(next)) queue.Enqueue(next);
            }
        }
    }

    /// <summary>Follows each branch as deep as possible before backtracking, using a stack instead of recursion.</summary>
    public IEnumerable<T> DepthFirst(T start)
    {
        if (!_adjacency.ContainsKey(start)) yield break;

        var visited = new HashSet<T>();
        var stack = new Stack<T>();
        stack.Push(start);
        while (!stack.IsEmpty)
        {
            var vertex = stack.Pop();
            if (!visited.Add(vertex)) continue;

            yield return vertex;

            // Push in reverse so neighbors are visited in the order they were added.
            var neighbors = _adjacency[vertex];
            for (var i = neighbors.Count - 1; i >= 0; i--)
            {
                if (!visited.Contains(neighbors[i])) stack.Push(neighbors[i]);
            }
        }
    }

    /// <summary>Fewest-edges path found with BFS, or null when <paramref name="to"/> is unreachable.</summary>
    public IReadOnlyList<T>? ShortestPath(T from, T to)
    {
        if (!_adjacency.ContainsKey(from) || !_adjacency.ContainsKey(to)) return null;

        var previous = new Dictionary<T, T>();
        var visited = new HashSet<T> { from };
        var queue = new Queue<T>([from]);
        while (queue.TryDequeue(out var vertex))
        {
            if (EqualityComparer<T>.Default.Equals(vertex, to))
            {
                var path = new List<T> { to };
                while (previous.TryGetValue(path[^1], out var step)) path.Add(step);
                path.Reverse();
                return path;
            }

            foreach (var next in _adjacency[vertex])
            {
                if (!visited.Add(next)) continue;
                previous[next] = vertex;
                queue.Enqueue(next);
            }
        }

        return null;
    }

    /// <summary>
    /// Orders vertices so every edge points forward (Kahn's algorithm), e.g. to schedule tasks after their dependencies.
    /// </summary>
    public IReadOnlyList<T> TopologicalSort()
    {
        if (!IsDirected) throw new InvalidOperationException("Topological sort requires a directed graph.");

        var inDegree = _adjacency.Keys.ToDictionary(v => v, _ => 0);
        foreach (var to in _adjacency.Values.SelectMany(n => n)) inDegree[to]++;

        var queue = new Queue<T>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var order = new List<T>();
        while (queue.TryDequeue(out var vertex))
        {
            order.Add(vertex);
            foreach (var next in _adjacency[vertex])
            {
                if (--inDegree[next] == 0) queue.Enqueue(next);
            }
        }

        return order.Count == _adjacency.Count ? order : throw new InvalidOperationException("Graph contains a cycle.");
    }
}
