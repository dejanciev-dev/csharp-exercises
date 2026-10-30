namespace CSharpExercises.EventSourcing;

public sealed record InventorySnapshot(Guid Id, string Name, int OnHand, bool Active, long Version);

/// <summary>
/// Long-lived aggregates accumulate thousands of events, and replaying all of them on every load gets slow. A snapshot
/// stores the state at some version; loading then starts from the latest snapshot and replays only the events after
/// it. Snapshots are a cache: the events stay the source of truth, and a snapshot can always be rebuilt.
/// </summary>
public sealed class SnapshottingInventoryRepository(InMemoryEventStore store, int snapshotEvery) : InventoryRepository(store)
{
    private readonly Dictionary<Guid, InventorySnapshot> _snapshots = new();

    /// <summary>How many events the last load replayed, to show the effect.</summary>
    public int LastReplayCount { get; private set; }

    public override InventoryItem Load(Guid id)
    {
        if (!_snapshots.TryGetValue(id, out var snapshot)) return Count(base.Load(id), Store.Version(StreamId(id)));

        var item = InventoryItem.FromSnapshot(snapshot);
        var later = Store.ReadStream(StreamId(id), afterVersion: snapshot.Version);
        foreach (var e in later) item.Apply(e.Data);
        return Count(item, later.Count);
    }

    public override void Save(InventoryItem item)
    {
        var before = item.Version - item.UncommittedEvents.Count;
        base.Save(item);
        if (item.Version / snapshotEvery > before / snapshotEvery) // crossed a multiple of snapshotEvery
            _snapshots[item.Id] = new InventorySnapshot(item.Id, item.Name, item.OnHand, item.Active, item.Version);
    }

    private InventoryItem Count(InventoryItem item, long replayed)
    {
        LastReplayCount = (int)replayed;
        return item;
    }
}
