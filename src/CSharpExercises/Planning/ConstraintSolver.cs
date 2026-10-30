namespace CSharpExercises.Planning;

/// <summary>
/// A small constraint satisfaction solver: variables with finite domains, and constraints between pairs of variables.
/// Backtracking search with two classic improvements: pick the variable with the fewest remaining values first (MRV,
/// "fail first"), and after each assignment remove now-impossible values from neighbours' domains (forward checking), so
/// dead ends show up immediately. Map colouring, timetabling and puzzles all fit this shape.
/// </summary>
public sealed class ConstraintSolver<TVariable, TValue> where TVariable : notnull
{
    private readonly Dictionary<TVariable, List<TValue>> _domains = new();
    private readonly List<(TVariable A, TVariable B, Func<TValue, TValue, bool> Allowed)> _constraints = [];

    public int Backtracks { get; private set; }

    public ConstraintSolver<TVariable, TValue> Variable(TVariable variable, IEnumerable<TValue> domain)
    {
        _domains[variable] = [.. domain];
        return this;
    }

    public ConstraintSolver<TVariable, TValue> Constraint(TVariable a, TVariable b, Func<TValue, TValue, bool> allowed)
    {
        _constraints.Add((a, b, allowed));
        return this;
    }

    public ConstraintSolver<TVariable, TValue> AllDifferent(TVariable a, TVariable b) =>
        Constraint(a, b, (x, y) => !EqualityComparer<TValue>.Default.Equals(x, y));

    public IReadOnlyDictionary<TVariable, TValue>? Solve()
    {
        Backtracks = 0;
        var domains = _domains.ToDictionary(d => d.Key, d => d.Value.ToList());
        return Search(new Dictionary<TVariable, TValue>(), domains);
    }

    private Dictionary<TVariable, TValue>? Search(Dictionary<TVariable, TValue> assignment, Dictionary<TVariable, List<TValue>> domains)
    {
        if (assignment.Count == domains.Count) return new Dictionary<TVariable, TValue>(assignment);

        // MRV: the unassigned variable with the smallest domain.
        var variable = domains.Keys.Where(v => !assignment.ContainsKey(v)).MinBy(v => domains[v].Count)!;

        foreach (var value in domains[variable].ToList())
        {
            assignment[variable] = value;
            var pruned = ForwardCheck(variable, value, assignment, domains);
            if (pruned is not null)
            {
                var result = Search(assignment, pruned);
                if (result is not null) return result;
            }
            assignment.Remove(variable);
            Backtracks++;
        }
        return null;
    }

    /// <summary>Copies the domains with values that conflict with the new assignment removed; null if one becomes empty.</summary>
    private Dictionary<TVariable, List<TValue>>? ForwardCheck(TVariable variable, TValue value, Dictionary<TVariable, TValue> assignment, Dictionary<TVariable, List<TValue>> domains)
    {
        var next = domains.ToDictionary(d => d.Key, d => d.Key.Equals(variable) ? [value] : d.Value);
        foreach (var (a, b, allowed) in _constraints)
        {
            TVariable other;
            Func<TValue, bool> keep;
            if (a.Equals(variable)) (other, keep) = (b, v => allowed(value, v));
            else if (b.Equals(variable)) (other, keep) = (a, v => allowed(v, value));
            else continue;
            if (assignment.ContainsKey(other) && !other.Equals(variable))
            {
                if (!keep(assignment[other])) return null;
                continue;
            }
            next[other] = next[other].Where(keep).ToList();
            if (next[other].Count == 0) return null;
        }
        return next;
    }
}
