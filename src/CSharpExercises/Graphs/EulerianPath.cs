namespace CSharpExercises.Graphs;

/// <summary>
/// An Eulerian path uses every edge exactly once (Euler's bridges of Königsberg, drawing a figure without lifting the
/// pen). It exists in a directed graph when at most one vertex has one more outgoing than incoming edge (the start) and
/// at most one the reverse (the end), and all edges are connected. Hierholzer's algorithm builds it in O(E) by walking
/// until stuck and splicing in detours. Classic use: reconstructing a flight itinerary from tickets.
/// </summary>
public static class EulerianPath
{
    public static IReadOnlyList<string>? Find(IEnumerable<(string From, string To)> edges)
    {
        var list = edges.ToList();
        if (list.Count == 0) return [];

        var outgoing = new Dictionary<string, List<string>>();
        var balance = new Dictionary<string, int>();
        foreach (var (from, to) in list)
        {
            if (!outgoing.TryGetValue(from, out var targets)) outgoing[from] = targets = [];
            targets.Add(to);
            balance[from] = balance.GetValueOrDefault(from) + 1;
            balance[to] = balance.GetValueOrDefault(to) - 1;
        }

        var starts = balance.Where(b => b.Value == 1).Select(b => b.Key).ToList();
        var ends = balance.Count(b => b.Value == -1);
        if (balance.Values.Any(v => Math.Abs(v) > 1) || starts.Count > 1 || ends > 1 || starts.Count != ends) return null;

        // Sorting the targets in reverse lets us pop the alphabetically smallest first: the lexically smallest path.
        foreach (var targets in outgoing.Values) targets.Sort((a, b) => string.CompareOrdinal(b, a));

        var start = starts.Count == 1 ? starts[0] : list.Select(e => e.From).Min(StringComparer.Ordinal)!;
        var stack = new Stack<string>([start]);
        var path = new List<string>();
        while (stack.TryPeek(out var vertex))
        {
            if (outgoing.TryGetValue(vertex, out var targets) && targets.Count > 0)
            {
                stack.Push(targets[^1]);
                targets.RemoveAt(targets.Count - 1);
            }
            else path.Add(stack.Pop()); // stuck: this vertex is final in the remaining walk
        }

        path.Reverse();
        return path.Count == list.Count + 1 ? path : null; // fewer vertices means some edges were unreachable
    }
}
