using CSharpExercises.EventSourcing;
using Microsoft.Extensions.Time.Testing;

namespace CSharpExercises.Tests;

public class EventSourcingTests
{
    [Fact]
    public void Store_AppendsWithOptimisticConcurrency()
    {
        var store = new InMemoryEventStore();

        Assert.Equal(2, store.Append("s", InMemoryEventStore.NoStream, ["a", "b"]));
        Assert.Equal(3, store.Append("s", 2, ["c"]));
        var conflict = Assert.Throws<ConcurrencyException>(() => store.Append("s", 2, ["d"]));
        Assert.Equal((2, 3), (conflict.Expected, conflict.Actual));

        store.Append("other", 0, ["x"]);
        Assert.Equal(["c"], store.ReadStream("s", afterVersion: 2).Select(e => e.Data));
        Assert.Equal([1L, 2, 3, 4], store.ReadAll().Select(e => e.Position));
    }

    [Fact]
    public void Aggregate_RebuildsFromItsEvents()
    {
        var store = new InMemoryEventStore();
        var repository = new InventoryRepository(store);
        var id = Guid.NewGuid();

        var item = InventoryItem.Create(id, "Widget");
        item.CheckIn(10);
        item.Remove(3);
        repository.Save(item);

        var loaded = repository.Load(id);
        Assert.Equal(("Widget", 7, 3L), (loaded.Name, loaded.OnHand, loaded.Version));
        Assert.Empty(loaded.UncommittedEvents);
        Assert.Throws<InvalidOperationException>(() => loaded.Remove(8)); // the rule is checked, no event raised
        Assert.Empty(loaded.UncommittedEvents);
    }

    [Fact]
    public void Aggregate_ConcurrentEditsConflict()
    {
        var store = new InMemoryEventStore();
        var repository = new InventoryRepository(store);
        var id = Guid.NewGuid();
        var item = InventoryItem.Create(id, "Widget");
        item.CheckIn(5);
        repository.Save(item);

        var alice = repository.Load(id);
        var bob = repository.Load(id);
        alice.Remove(4);
        bob.Remove(4);
        repository.Save(alice);

        Assert.Throws<ConcurrencyException>(() => repository.Save(bob)); // otherwise stock would go to -3
    }

    [Fact]
    public void Projection_BuildsAReadModelAndCatchesUp()
    {
        var store = new InMemoryEventStore();
        var repository = new InventoryRepository(store);
        var live = new InventoryListProjection();
        live.Subscribe(store);

        var (a, b) = (InventoryItem.Create(Guid.NewGuid(), "Bolts"), InventoryItem.Create(Guid.NewGuid(), "Anchors"));
        a.CheckIn(100);
        b.CheckIn(3);
        repository.Save(a);
        repository.Save(b);
        b.Rename("Anchors XL");
        repository.Save(b);

        Assert.Equal([("Anchors XL", 3), ("Bolts", 100)], live.Rows.Select(r => (r.Name, r.OnHand)));
        Assert.Equal(["Anchors XL"], live.LowStock(10).Select(r => r.Name));

        var rebuilt = new InventoryListProjection();
        rebuilt.CatchUp(store);
        rebuilt.CatchUp(store); // idempotent
        Assert.Equal(live.Rows, rebuilt.Rows);
        Assert.Equal(store.ReadAll().Count, rebuilt.Position);
    }

    [Fact]
    public void Projection_DailyReport()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 30, 9, 0, 0, TimeSpan.Zero));
        var store = new InMemoryEventStore(time);
        var repository = new InventoryRepository(store);
        var item = InventoryItem.Create(Guid.NewGuid(), "Nuts");
        item.CheckIn(50);
        repository.Save(item);
        time.Advance(TimeSpan.FromDays(1));
        item.Remove(20);
        item.Remove(5);
        repository.Save(item);

        var report = StockMovementReport.Build(store);

        Assert.Equal((50, 0), report[new DateOnly(2026, 10, 30)]);
        Assert.Equal((0, 25), report[new DateOnly(2026, 10, 31)]);
    }

    [Fact]
    public void Snapshots_ReplayOnlyRecentEvents()
    {
        var store = new InMemoryEventStore();
        var repository = new SnapshottingInventoryRepository(store, snapshotEvery: 100);
        var id = Guid.NewGuid();
        var item = InventoryItem.Create(id, "Screws");
        repository.Save(item);
        for (var i = 0; i < 250; i++)
        {
            item.CheckIn(2);
            repository.Save(item);
        }

        var loaded = repository.Load(id);

        Assert.Equal(500, loaded.OnHand);
        Assert.Equal(251, loaded.Version);
        Assert.Equal(51, repository.LastReplayCount); // events after the snapshot at version 200

        var plain = new InventoryRepository(store).Load(id);
        Assert.Equal((plain.OnHand, plain.Version), (loaded.OnHand, loaded.Version));
    }

    [Fact]
    public async Task Saga_CompensatesCompletedStepsInReverse()
    {
        var actions = new List<string>();
        SagaStep Step(string name, bool fails = false) => new(
            name,
            _ => fails ? throw new InvalidOperationException("card declined") : Task.Run(() => actions.Add(name)),
            () => Task.Run(() => actions.Add($"undo {name}")));

        var result = await new Saga([Step("reserve stock"), Step("create shipment"), Step("charge card", fails: true), Step("send email")]).RunAsync();

        Assert.False(result.Succeeded);
        Assert.Equal("charge card", result.FailedStep);
        Assert.Equal(["reserve stock", "create shipment", "undo create shipment", "undo reserve stock"], actions);
        Assert.Contains("failed: charge card (card declined)", result.Log);
    }

    [Fact]
    public async Task Saga_SucceedsAndSurvivesFailingCompensations()
    {
        Assert.True((await new Saga([new("only", _ => Task.CompletedTask, () => Task.CompletedTask)]).RunAsync()).Succeeded);

        var result = await new Saga([
            new("a", _ => Task.CompletedTask, () => throw new TimeoutException("refund service down")),
            new("b", _ => Task.CompletedTask, () => Task.CompletedTask),
            new("c", _ => throw new Exception("boom"), () => Task.CompletedTask),
        ]).RunAsync();

        Assert.Equal(["done: a", "done: b", "failed: c (boom)", "compensated: b", "compensation failed: a (refund service down)"], result.Log);
    }

    [Fact]
    public void Upcasting_ConvertsOldEventsStepByStep()
    {
        var store = new InMemoryEventStore();
        var id = Guid.NewGuid();
        store.Append("customer", 0, [new CustomerRegisteredV1(id, "Ada King Lovelace")]);
        store.Append("customer", 1, [new CustomerRegisteredV2(id, "Grace", "Hopper")]);
        store.Append("customer", 2, [new CustomerRegisteredV3(id, "Alan", "Turing", "UK")]);

        var events = Upcaster.ForCustomers().UpcastAll(store.ReadStream("customer")).Cast<CustomerRegisteredV3>().ToList();

        Assert.Equal(new CustomerRegisteredV3(id, "Ada King", "Lovelace", "unknown"), events[0]);
        Assert.Equal(new CustomerRegisteredV3(id, "Grace", "Hopper", "unknown"), events[1]);
        Assert.Equal("UK", events[2].Country);
    }
}
