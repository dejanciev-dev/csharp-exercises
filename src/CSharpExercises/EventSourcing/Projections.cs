namespace CSharpExercises.EventSourcing;

public sealed record InventoryRow(Guid Id, string Name, int OnHand);

/// <summary>
/// A projection turns the event log into a read model shaped for one screen or query (the "Q" in CQRS). It remembers
/// the last global position it processed, so it can catch up after a restart, ignore events it has already seen, and
/// be thrown away and rebuilt from scratch when its shape changes.
/// </summary>
public sealed class InventoryListProjection
{
    private readonly Dictionary<Guid, InventoryRow> _rows = new();

    public long Position { get; private set; }

    public IReadOnlyList<InventoryRow> Rows => _rows.Values.OrderBy(r => r.Name, StringComparer.Ordinal).ToList();

    public IReadOnlyList<InventoryRow> LowStock(int threshold) => Rows.Where(r => r.OnHand < threshold).ToList();

    public void CatchUp(InMemoryEventStore store)
    {
        foreach (var e in store.ReadAll(Position)) Handle(e);
    }

    /// <summary>Live updates; out-of-order or repeated deliveries are skipped by position.</summary>
    public void Subscribe(InMemoryEventStore store) => store.Appended += Handle;

    public void Handle(StoredEvent stored)
    {
        if (stored.Position <= Position) return; // already applied: makes redelivery harmless
        switch (stored.Data)
        {
            case ItemCreated c: _rows[c.Id] = new InventoryRow(c.Id, c.Name, 0); break;
            case ItemsCheckedIn c: _rows[c.Id] = _rows[c.Id] with { OnHand = _rows[c.Id].OnHand + c.Count }; break;
            case ItemsRemoved r: _rows[r.Id] = _rows[r.Id] with { OnHand = _rows[r.Id].OnHand - r.Count }; break;
            case ItemRenamed r: _rows[r.Id] = _rows[r.Id] with { Name = r.NewName }; break;
            case ItemDeactivated d: _rows.Remove(d.Id); break; // this screen only lists active items
        }
        Position = stored.Position;
    }
}

/// <summary>A second read model from the same events: a daily movement report. Projections are cheap to add.</summary>
public static class StockMovementReport
{
    public static IReadOnlyDictionary<DateOnly, (int In, int Out)> Build(InMemoryEventStore store) =>
        store.ReadAll()
            .Where(e => e.Data is ItemsCheckedIn or ItemsRemoved)
            .GroupBy(e => DateOnly.FromDateTime(e.RecordedAt.UtcDateTime))
            .ToDictionary(
                g => g.Key,
                g => (g.Sum(e => e.Data is ItemsCheckedIn c ? c.Count : 0), g.Sum(e => e.Data is ItemsRemoved r ? r.Count : 0)));
}
