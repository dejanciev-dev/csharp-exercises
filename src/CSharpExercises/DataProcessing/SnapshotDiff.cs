namespace CSharpExercises.DataProcessing;

public enum ChangeKind { Added, Removed, Modified }

public sealed record FieldChange(string Field, string? Before, string? After);

public sealed record RecordChange(string Key, ChangeKind Kind, IReadOnlyList<FieldChange> Fields);

/// <summary>
/// Change data capture by comparing two snapshots of a table (yesterday's export and today's): match records by key,
/// then report added, removed and modified records with the fields that changed. Used to sync systems that can only
/// produce full exports. Ignored fields (like a last-updated timestamp) prevent noise.
/// </summary>
public static class SnapshotDiff
{
    public static IReadOnlyList<RecordChange> Compare(
        IEnumerable<IReadOnlyDictionary<string, string>> before,
        IEnumerable<IReadOnlyDictionary<string, string>> after,
        string keyField,
        IEnumerable<string>? ignoreFields = null)
    {
        var ignored = new HashSet<string>(ignoreFields ?? []) { keyField };
        var old = IndexByKey(before, keyField);
        var current = IndexByKey(after, keyField);
        var changes = new List<RecordChange>();

        foreach (var (key, record) in current)
        {
            if (!old.TryGetValue(key, out var previous))
            {
                changes.Add(new RecordChange(key, ChangeKind.Added, []));
                continue;
            }
            var fields = previous.Keys.Union(record.Keys)
                .Where(f => !ignored.Contains(f))
                .Select(f => new FieldChange(f, previous.GetValueOrDefault(f), record.GetValueOrDefault(f)))
                .Where(c => c.Before != c.After)
                .OrderBy(c => c.Field, StringComparer.Ordinal)
                .ToList();
            if (fields.Count > 0) changes.Add(new RecordChange(key, ChangeKind.Modified, fields));
        }

        changes.AddRange(old.Keys.Where(k => !current.ContainsKey(k)).Select(k => new RecordChange(k, ChangeKind.Removed, [])));
        return changes.OrderBy(c => c.Key, StringComparer.Ordinal).ToList();
    }

    private static Dictionary<string, IReadOnlyDictionary<string, string>> IndexByKey(IEnumerable<IReadOnlyDictionary<string, string>> records, string keyField)
    {
        var index = new Dictionary<string, IReadOnlyDictionary<string, string>>();
        foreach (var record in records)
        {
            var key = record.TryGetValue(keyField, out var k) ? k : throw new ArgumentException($"A record has no '{keyField}'.");
            if (!index.TryAdd(key, record)) throw new ArgumentException($"Duplicate key '{key}' in one snapshot.");
        }
        return index;
    }
}
