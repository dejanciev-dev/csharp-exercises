using System.Collections.Immutable;

namespace CSharpExercises.Distributed;

/// <summary>
/// Conflict-free replicated data types: replicas accept writes independently (offline, in different data centres) and
/// still converge, because merge is commutative, associative and idempotent: merging in any order, any number of
/// times, gives the same result. No coordination, no conflicts.
/// </summary>
public interface ICrdt<T> where T : ICrdt<T>
{
    T Merge(T other);
}

/// <summary>Grow-only counter: one slot per replica, each replica only increments its own, merge takes the maximum.</summary>
public sealed record GCounter(ImmutableDictionary<string, long> Slots) : ICrdt<GCounter>
{
    public static GCounter Empty { get; } = new(ImmutableDictionary<string, long>.Empty);
    public long Value => Slots.Values.Sum();
    public GCounter Increment(string replica, long by = 1) =>
        by < 0 ? throw new ArgumentOutOfRangeException(nameof(by)) : new(Slots.SetItem(replica, Slots.GetValueOrDefault(replica) + by));
    public GCounter Merge(GCounter other) =>
        new(other.Slots.Aggregate(Slots, (acc, s) => acc.SetItem(s.Key, Math.Max(acc.GetValueOrDefault(s.Key), s.Value))));

    // Records compare an ImmutableDictionary by reference; replicas are equal when their slots are.
    public bool Equals(GCounter? other) =>
        other is not null && Slots.Count == other.Slots.Count && Slots.All(s => other.Slots.TryGetValue(s.Key, out var v) && v == s.Value);

    public override int GetHashCode() => Value.GetHashCode();
}

/// <summary>Counter that can go down: two G-counters, one for increments and one for decrements.</summary>
public sealed record PNCounter(GCounter Increments, GCounter Decrements) : ICrdt<PNCounter>
{
    public static PNCounter Empty { get; } = new(GCounter.Empty, GCounter.Empty);
    public long Value => Increments.Value - Decrements.Value;
    public PNCounter Add(string replica, long delta) =>
        delta >= 0 ? this with { Increments = Increments.Increment(replica, delta) } : this with { Decrements = Decrements.Increment(replica, -delta) };
    public PNCounter Merge(PNCounter other) => new(Increments.Merge(other.Increments), Decrements.Merge(other.Decrements));
}

/// <summary>Last-writer-wins register: the value with the highest (timestamp, replica) wins.</summary>
public sealed record LwwRegister<T>(T Value, long Timestamp, string Replica) : ICrdt<LwwRegister<T>>
{
    public LwwRegister<T> Set(T value, long timestamp, string replica) => Merge(new LwwRegister<T>(value, timestamp, replica));
    public LwwRegister<T> Merge(LwwRegister<T> other) =>
        (other.Timestamp, other.Replica).CompareTo((Timestamp, Replica)) > 0 ? other : this;
}

/// <summary>
/// Observed-remove set: every add gets a unique tag, and a remove deletes only the tags it has seen. An add that was
/// concurrent with a remove survives ("add wins"), which is what users of a shared shopping list expect.
/// </summary>
public sealed record OrSet<T>(ImmutableHashSet<(T Item, Guid Tag)> Added, ImmutableHashSet<Guid> Removed) : ICrdt<OrSet<T>>
{
    public static OrSet<T> Empty { get; } = new(ImmutableHashSet<(T, Guid)>.Empty, ImmutableHashSet<Guid>.Empty);

    public IReadOnlySet<T> Items => Added.Where(a => !Removed.Contains(a.Tag)).Select(a => a.Item).ToHashSet();

    public bool Contains(T item) => Items.Contains(item);

    public OrSet<T> Add(T item) => this with { Added = Added.Add((item, Guid.NewGuid())) };

    public OrSet<T> Remove(T item) =>
        this with { Removed = Removed.Union(Added.Where(a => EqualityComparer<T>.Default.Equals(a.Item, item)).Select(a => a.Tag)) };

    public OrSet<T> Merge(OrSet<T> other) => new(Added.Union(other.Added), Removed.Union(other.Removed));
}
