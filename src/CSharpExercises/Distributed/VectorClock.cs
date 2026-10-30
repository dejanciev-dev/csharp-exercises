using System.Collections.Immutable;

namespace CSharpExercises.Distributed;

public enum Causality { Equal, Before, After, Concurrent }

/// <summary>
/// Vector clocks capture causality exactly: each node keeps a counter per node. a happened before b if every counter
/// in a is ≤ the one in b (and they differ); if neither dominates, the events are concurrent: a genuine conflict that
/// a database such as Dynamo or Riak must keep both versions of ("siblings") for the application to resolve.
/// </summary>
public sealed record VectorClock(ImmutableSortedDictionary<string, long> Counters)
{
    public static VectorClock Empty { get; } = new(ImmutableSortedDictionary<string, long>.Empty);

    public long this[string node] => Counters.GetValueOrDefault(node);

    public VectorClock Increment(string node) => new(Counters.SetItem(node, this[node] + 1));

    /// <summary>On receiving: take the maximum of each counter, then count the receive event.</summary>
    public VectorClock Merge(VectorClock other, string receivingNode) =>
        new VectorClock(Counters.Keys.Union(other.Counters.Keys)
            .Aggregate(ImmutableSortedDictionary<string, long>.Empty, (acc, k) => acc.SetItem(k, Math.Max(this[k], other[k]))))
        .Increment(receivingNode);

    public Causality CompareTo(VectorClock other)
    {
        var keys = Counters.Keys.Union(other.Counters.Keys).ToList();
        var less = keys.Any(k => this[k] < other[k]);
        var greater = keys.Any(k => this[k] > other[k]);
        return (less, greater) switch
        {
            (false, false) => Causality.Equal,
            (true, false) => Causality.Before,
            (false, true) => Causality.After,
            _ => Causality.Concurrent,
        };
    }

    public override string ToString() => "{" + string.Join(", ", Counters.Select(c => $"{c.Key}:{c.Value}")) + "}";
}

/// <summary>A key-value store replica that keeps concurrent writes as siblings instead of silently losing one.</summary>
public sealed class VersionedStore
{
    private readonly Dictionary<string, List<(string Value, VectorClock Clock)>> _data = new();

    public IReadOnlyList<(string Value, VectorClock Clock)> Get(string key) => _data.GetValueOrDefault(key) ?? [];

    /// <summary>Stores a write, dropping every version it supersedes and keeping the concurrent ones.</summary>
    public void Put(string key, string value, VectorClock clock)
    {
        var versions = _data.GetValueOrDefault(key) ?? [];
        if (versions.Any(v => v.Clock.CompareTo(clock) is Causality.After or Causality.Equal)) return; // stale write
        versions.RemoveAll(v => v.Clock.CompareTo(clock) == Causality.Before);
        versions.Add((value, clock));
        _data[key] = versions;
    }
}
