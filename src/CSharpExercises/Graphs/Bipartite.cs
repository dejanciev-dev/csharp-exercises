namespace CSharpExercises.Graphs;

/// <summary>
/// A graph is bipartite (two-colourable) exactly when it has no odd cycle. Colour by BFS layers; an edge between two
/// vertices of the same colour proves an odd cycle. Useful for "split these people into two teams with no rivals
/// on the same team".
/// </summary>
public static class Bipartite
{
    /// <returns>The colour (0 or 1) of each vertex, or null when the graph is not bipartite.</returns>
    public static int[]? TwoColour(IReadOnlyList<IReadOnlyList<int>> adjacency)
    {
        var colour = new int[adjacency.Count];
        Array.Fill(colour, -1);
        var queue = new Queue<int>();

        for (var start = 0; start < adjacency.Count; start++)
        {
            if (colour[start] != -1) continue;
            colour[start] = 0;
            queue.Enqueue(start);

            while (queue.TryDequeue(out var v))
            {
                foreach (var w in adjacency[v])
                {
                    if (colour[w] == -1)
                    {
                        colour[w] = 1 - colour[v];
                        queue.Enqueue(w);
                    }
                    else if (colour[w] == colour[v])
                    {
                        return null;
                    }
                }
            }
        }
        return colour;
    }

    /// <summary>
    /// Maximum bipartite matching with augmenting paths (Kuhn's algorithm): each left vertex tries a free right
    /// vertex, or evicts the current owner if that owner can move elsewhere.
    /// </summary>
    /// <param name="likes">likes[l] lists the right-side vertices that left vertex l can be matched with.</param>
    /// <returns>For each left vertex, its matched right vertex or -1.</returns>
    public static int[] MaximumMatching(IReadOnlyList<IReadOnlyList<int>> likes, int rightCount)
    {
        var ownerOfRight = new int[rightCount];
        Array.Fill(ownerOfRight, -1);

        bool TryAssign(int left, bool[] visited)
        {
            foreach (var right in likes[left])
            {
                if (visited[right]) continue;
                visited[right] = true;
                if (ownerOfRight[right] == -1 || TryAssign(ownerOfRight[right], visited))
                {
                    ownerOfRight[right] = left;
                    return true;
                }
            }
            return false;
        }

        for (var left = 0; left < likes.Count; left++) TryAssign(left, new bool[rightCount]);

        var match = new int[likes.Count];
        Array.Fill(match, -1);
        for (var right = 0; right < rightCount; right++)
            if (ownerOfRight[right] != -1) match[ownerOfRight[right]] = right;
        return match;
    }
}
