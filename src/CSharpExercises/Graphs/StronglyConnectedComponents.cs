namespace CSharpExercises.Graphs;

/// <summary>
/// Tarjan's algorithm: one depth-first search finds every strongly connected component (a group where each vertex
/// can reach every other). A vertex whose "lowest reachable discovery time" is its own is the root of a component,
/// and the component is everything above it on the stack.
/// </summary>
public static class StronglyConnectedComponents
{
    /// <param name="adjacency">adjacency[v] lists the vertices v has edges to.</param>
    /// <returns>Components in reverse topological order: no edges lead from a component to a later one.</returns>
    public static List<List<int>> Find(IReadOnlyList<IReadOnlyList<int>> adjacency)
    {
        var n = adjacency.Count;
        var index = new int[n];
        var low = new int[n];
        var onStack = new bool[n];
        Array.Fill(index, -1);

        var stack = new Stack<int>();
        var components = new List<List<int>>();
        var counter = 0;

        void Visit(int v)
        {
            index[v] = low[v] = counter++;
            stack.Push(v);
            onStack[v] = true;

            foreach (var w in adjacency[v])
            {
                if (index[w] == -1)
                {
                    Visit(w);
                    low[v] = Math.Min(low[v], low[w]);
                }
                else if (onStack[w])
                {
                    low[v] = Math.Min(low[v], index[w]);
                }
            }

            if (low[v] != index[v]) return;

            var component = new List<int>();
            int top;
            do
            {
                top = stack.Pop();
                onStack[top] = false;
                component.Add(top);
            } while (top != v);
            component.Sort();
            components.Add(component);
        }

        for (var v = 0; v < n; v++)
            if (index[v] == -1) Visit(v);

        return components;
    }

    /// <summary>
    /// The condensation: each component becomes one vertex, which always leaves a DAG. Returns, per vertex, the id of
    /// its component.
    /// </summary>
    public static int[] ComponentIds(IReadOnlyList<IReadOnlyList<int>> adjacency)
    {
        var ids = new int[adjacency.Count];
        var components = Find(adjacency);
        for (var c = 0; c < components.Count; c++)
            foreach (var v in components[c]) ids[v] = c;
        return ids;
    }
}
