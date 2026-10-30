namespace CSharpExercises.EventSourcing;

public sealed record ItemCreated(Guid Id, string Name);
public sealed record ItemsCheckedIn(Guid Id, int Count);
public sealed record ItemsRemoved(Guid Id, int Count);
public sealed record ItemRenamed(Guid Id, string NewName);
public sealed record ItemDeactivated(Guid Id);

/// <summary>
/// An event-sourced aggregate. Commands check the rules against current state and, if allowed, raise events; state
/// only ever changes in <see cref="When"/>, which is also how the aggregate is rebuilt from history. Keeping
/// decisions and state changes apart means replaying old events never re-runs the business rules.
/// </summary>
public sealed class InventoryItem
{
    private readonly List<object> _uncommitted = [];

    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public int OnHand { get; private set; }
    public bool Active { get; private set; }

    /// <summary>The number of events applied, used as the expected version when saving.</summary>
    public long Version { get; private set; }

    public IReadOnlyList<object> UncommittedEvents => _uncommitted;

    private InventoryItem() { }

    public static InventoryItem Create(Guid id, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A name is required.", nameof(name));
        var item = new InventoryItem();
        item.Raise(new ItemCreated(id, name));
        return item;
    }

    public static InventoryItem FromHistory(IEnumerable<object> history)
    {
        var item = new InventoryItem();
        foreach (var e in history) item.Apply(e);
        return item;
    }

    public void CheckIn(int count)
    {
        EnsureActive();
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
        Raise(new ItemsCheckedIn(Id, count));
    }

    public void Remove(int count)
    {
        EnsureActive();
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (count > OnHand) throw new InvalidOperationException($"Only {OnHand} of '{Name}' on hand, can't remove {count}.");
        Raise(new ItemsRemoved(Id, count));
    }

    public void Rename(string newName)
    {
        EnsureActive();
        if (newName != Name) Raise(new ItemRenamed(Id, newName)); // no change, no event
    }

    public void Deactivate()
    {
        if (OnHand > 0) throw new InvalidOperationException("Remove the remaining stock before deactivating.");
        if (Active) Raise(new ItemDeactivated(Id));
    }

    public void MarkCommitted() => _uncommitted.Clear();

    /// <summary>Restores state from a snapshot so that only later events need replaying.</summary>
    internal static InventoryItem FromSnapshot(InventorySnapshot snapshot) => new()
    {
        Id = snapshot.Id, Name = snapshot.Name, OnHand = snapshot.OnHand, Active = snapshot.Active, Version = snapshot.Version,
    };

    internal void Apply(object e)
    {
        When(e);
        Version++;
    }

    private void Raise(object e)
    {
        Apply(e);
        _uncommitted.Add(e);
    }

    private void When(object e)
    {
        switch (e)
        {
            case ItemCreated c: (Id, Name, Active) = (c.Id, c.Name, true); break;
            case ItemsCheckedIn c: OnHand += c.Count; break;
            case ItemsRemoved r: OnHand -= r.Count; break;
            case ItemRenamed r: Name = r.NewName; break;
            case ItemDeactivated: Active = false; break;
            default: throw new InvalidOperationException($"Unknown event {e.GetType().Name}.");
        }
    }

    private void EnsureActive()
    {
        if (!Active) throw new InvalidOperationException($"'{Name}' is deactivated.");
    }
}

/// <summary>Loads aggregates by replaying their stream and saves new events with optimistic concurrency.</summary>
public class InventoryRepository(InMemoryEventStore store)
{
    protected InMemoryEventStore Store { get; } = store;

    public static string StreamId(Guid id) => $"inventory-{id}";

    public virtual InventoryItem Load(Guid id)
    {
        var events = Store.ReadStream(StreamId(id));
        if (events.Count == 0) throw new KeyNotFoundException($"No inventory item {id}.");
        return InventoryItem.FromHistory(events.Select(e => e.Data));
    }

    public virtual void Save(InventoryItem item)
    {
        var expected = item.Version - item.UncommittedEvents.Count;
        Store.Append(StreamId(item.Id), expected, item.UncommittedEvents);
        item.MarkCommitted();
    }
}
