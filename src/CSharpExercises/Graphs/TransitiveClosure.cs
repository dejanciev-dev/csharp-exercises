using System.Collections;

namespace CSharpExercises.Graphs;

/// <summary>
/// Which vertices can reach which (dependency graphs, "is this role inherited by that one", "can this module see that
/// one")? Warshall's algorithm is Floyd–Warshall with booleans; storing each row as a <see cref="BitArray"/> turns the
/// inner loop into one OR of whole rows, 64 vertices per machine word. The transitive reduction removes edges implied
/// by others, giving the minimal graph to draw.
/// </summary>
public sealed class TransitiveClosure
{
    private readonly BitArray[] _reach;

    public TransitiveClosure(int vertexCount, IEnumerable<(int From, int To)> edges)
    {
        _reach = Enumerable.Range(0, vertexCount).Select(_ => new BitArray(vertexCount)).ToArray();
        foreach (var (from, to) in edges) _reach[from][to] = true;

        for (var k = 0; k < vertexCount; k++)
            for (var i = 0; i < vertexCount; i++)
                if (_reach[i][k]) _reach[i].Or(_reach[k]); // i reaches everything k reaches
    }

    public bool CanReach(int from, int to) => _reach[from][to];

    public IReadOnlyList<int> ReachableFrom(int vertex) =>
        Enumerable.Range(0, _reach.Length).Where(v => _reach[vertex][v]).ToList();

    public bool HasCycle => Enumerable.Range(0, _reach.Length).Any(v => _reach[v][v]);

    /// <summary>For a DAG: keep edge u → v only if no other successor of u also reaches v.</summary>
    public static IReadOnlyList<(int From, int To)> Reduction(int vertexCount, IReadOnlyCollection<(int From, int To)> edges)
    {
        var closure = new TransitiveClosure(vertexCount, edges);
        if (closure.HasCycle) throw new InvalidOperationException("The transitive reduction is only unique for acyclic graphs.");
        return edges.Where(e => !edges.Any(other => other.From == e.From && other.To != e.To && closure.CanReach(other.To, e.To))).ToList();
    }
}
