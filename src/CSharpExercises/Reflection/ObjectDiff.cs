using System.Collections;
using System.Reflection;

namespace CSharpExercises.Reflection;

public sealed record PropertyChange(string Path, object? Before, object? After);

/// <summary>
/// Comparing two objects property by property and reporting what changed with dotted paths ("Address.City",
/// "Tags[2]"): the core of audit logs ("who changed what"), PATCH generation and readable test failure messages.
/// Collections are compared by index, nested objects recursively, and properties can be excluded (timestamps, ids).
/// </summary>
public static class ObjectDiff
{
    public static IReadOnlyList<PropertyChange> Compare<T>(T before, T after, params string[] ignore)
    {
        var changes = new List<PropertyChange>();
        Walk(before, after, "", changes, ignore.ToHashSet(), 0);
        return changes;
    }

    private static void Walk(object? a, object? b, string path, List<PropertyChange> changes, HashSet<string> ignore, int depth)
    {
        if (ignore.Contains(path)) return;
        if (a is null || b is null || IsSimple(a) || IsSimple(b))
        {
            if (!Equals(a, b)) changes.Add(new PropertyChange(path, a, b));
            return;
        }
        if (depth > 20) throw new InvalidOperationException($"Too deep at '{path}': is there a cycle?");

        if (a is IEnumerable ea && b is IEnumerable eb)
        {
            var (la, lb) = (ea.Cast<object?>().ToList(), eb.Cast<object?>().ToList());
            for (var i = 0; i < Math.Max(la.Count, lb.Count); i++)
                Walk(i < la.Count ? la[i] : null, i < lb.Count ? lb[i] : null, $"{path}[{i}]", changes, ignore, depth + 1);
            return;
        }

        foreach (var property in a.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetIndexParameters().Length == 0))
        {
            var childPath = path.Length == 0 ? property.Name : $"{path}.{property.Name}";
            Walk(property.GetValue(a), property.GetValue(b), childPath, changes, ignore, depth + 1);
        }
    }

    private static bool IsSimple(object value) =>
        value is string or decimal or DateTime or DateTimeOffset or TimeSpan or Guid or Enum || value.GetType().IsPrimitive;

    /// <summary>A human-readable audit line per change.</summary>
    public static IEnumerable<string> Describe(IEnumerable<PropertyChange> changes) =>
        changes.Select(c => $"{c.Path}: {c.Before ?? "(none)"} → {c.After ?? "(none)"}");
}
