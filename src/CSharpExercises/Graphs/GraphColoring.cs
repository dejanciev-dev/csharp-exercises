namespace CSharpExercises.Graphs;

/// <summary>
/// Colouring a graph so neighbours differ: scheduling exams so no student has two at once, assigning radio frequencies,
/// allocating registers. Finding the fewest colours is NP-hard, so in practice greedy colouring is used, and order
/// matters: Welsh–Powell colours the highest-degree vertices first and usually needs fewer colours than arbitrary
/// order. For small graphs, backtracking finds the true chromatic number.
/// </summary>
public static class GraphColoring
{
    public static int[] Greedy(IReadOnlyList<IReadOnlyList<int>> adjacency, IEnumerable<int> order)
    {
        var colour = Enumerable.Repeat(-1, adjacency.Count).ToArray();
        foreach (var v in order)
        {
            var used = adjacency[v].Where(n => colour[n] >= 0).Select(n => colour[n]).ToHashSet();
            var c = 0;
            while (used.Contains(c)) c++;
            colour[v] = c;
        }
        return colour;
    }

    public static int[] WelshPowell(IReadOnlyList<IReadOnlyList<int>> adjacency) =>
        Greedy(adjacency, Enumerable.Range(0, adjacency.Count).OrderByDescending(v => adjacency[v].Count));

    /// <summary>The smallest number of colours, by trying k = 1, 2, … with backtracking.</summary>
    public static int ChromaticNumber(IReadOnlyList<IReadOnlyList<int>> adjacency)
    {
        var n = adjacency.Count;
        if (n == 0) return 0;
        var colour = new int[n];

        bool Assign(int v, int k)
        {
            if (v == n) return true;
            for (var c = 1; c <= k; c++)
            {
                if (adjacency[v].Any(u => colour[u] == c)) continue;
                colour[v] = c;
                if (Assign(v + 1, k)) return true;
            }
            colour[v] = 0;
            return false;
        }

        for (var k = 1; ; k++)
            if (Assign(0, k)) return k;
    }

    public static bool IsProper(IReadOnlyList<IReadOnlyList<int>> adjacency, int[] colour) =>
        Enumerable.Range(0, adjacency.Count).All(v => adjacency[v].All(u => colour[u] != colour[v]));
}
