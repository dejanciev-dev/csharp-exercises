using CSharpExercises.Distributed;

namespace CSharpExercises.Tests;

public class DistributedTests
{
    [Fact]
    public void Lamport_OrdersCausallyRelatedEvents()
    {
        var a = new LamportClock("a");
        var b = new LamportClock("b");

        a.Tick();                       // a: 1
        var sent = a.Send();            // a: 2
        b.Tick();                       // b: 1
        var received = b.Receive(sent); // b: max(1, 2) + 1 = 3

        Assert.Equal(2, sent);
        Assert.Equal(3, received);
        Assert.True(sent < received);
        Assert.True(LamportClock.Compare((3, "a"), (3, "b")) < 0); // tie broken by node id
    }

    [Fact]
    public void VectorClock_DetectsConcurrency()
    {
        var start = VectorClock.Empty.Increment("a");
        var onA = start.Increment("a");
        var onB = start.Merge(VectorClock.Empty, "b");
        var merged = onA.Merge(onB, "a");

        Assert.Equal(Causality.Before, start.CompareTo(onA));
        Assert.Equal(Causality.Concurrent, onA.CompareTo(onB));
        Assert.Equal(Causality.After, merged.CompareTo(onB));
        Assert.Equal("{a:3, b:1}", merged.ToString());
    }

    [Fact]
    public void VectorClock_StoreKeepsSiblingsForConflicts()
    {
        var store = new VersionedStore();
        var v1 = VectorClock.Empty.Increment("a");
        store.Put("cart", "milk", v1);

        var alice = v1.Increment("a");
        var bob = v1.Increment("b");
        store.Put("cart", "milk,eggs", alice);
        store.Put("cart", "milk,bread", bob);
        Assert.Equal(["milk,eggs", "milk,bread"], store.Get("cart").Select(v => v.Value)); // concurrent: keep both

        var resolved = alice.Merge(bob, "a");
        store.Put("cart", "milk,eggs,bread", resolved);
        Assert.Equal(["milk,eggs,bread"], store.Get("cart").Select(v => v.Value));

        store.Put("cart", "stale", v1);
        Assert.Single(store.Get("cart"));
    }

    [Fact]
    public void Crdt_CountersConvergeInAnyOrder()
    {
        var a = GCounter.Empty.Increment("a", 3);
        var b = GCounter.Empty.Increment("b", 5);
        var c = GCounter.Empty.Increment("c").Increment("c");

        Assert.Equal(10, a.Merge(b).Merge(c).Value);
        Assert.Equal(a.Merge(b).Merge(c), c.Merge(a).Merge(b).Merge(b)); // commutative, associative, idempotent

        var likes = PNCounter.Empty.Add("a", 10).Merge(PNCounter.Empty.Add("b", -3));
        Assert.Equal(7, likes.Value);
    }

    [Fact]
    public void Crdt_LastWriterWins()
    {
        var x = new LwwRegister<string>("draft", 1, "a");
        var y = x.Set("final", 5, "b");
        var z = x.Set("older edit", 3, "c");

        Assert.Equal("final", y.Merge(z).Value);
        Assert.Equal("final", z.Merge(y).Value);
        Assert.Equal("b wins", new LwwRegister<string>("a wins", 7, "a").Merge(new("b wins", 7, "b")).Value); // tie: higher replica id
    }

    [Fact]
    public void Crdt_ObservedRemoveSetLetsConcurrentAddsWin()
    {
        var shared = OrSet<string>.Empty.Add("milk").Add("eggs");

        var phone = shared.Remove("milk");
        var laptop = shared.Add("milk").Add("bread"); // re-added milk without seeing the removal

        var merged = phone.Merge(laptop);
        Assert.True(merged.Items.SetEquals(["milk", "eggs", "bread"]));
        Assert.Equal(merged.Items, laptop.Merge(phone).Items);
        Assert.False(phone.Contains("milk"));
    }

    [Fact]
    public void Merkle_FindsChangedBlocksQuickly()
    {
        var blocks = Enumerable.Range(0, 1024).Select(i => $"row {i}").ToArray();
        var original = new MerkleTree(blocks);
        blocks[300] = "changed";
        blocks[901] = "changed too";
        var modified = new MerkleTree(blocks);

        Assert.NotEqual(original.Root, modified.Root);
        Assert.Equal([300, 901], original.Diff(modified, out var comparisons));
        Assert.True(comparisons < 50); // instead of 1024
        Assert.Empty(original.Diff(new MerkleTree(Enumerable.Range(0, 1024).Select(i => $"row {i}").ToArray()), out _));
    }

    [Fact]
    public void Merkle_ProvesInclusion()
    {
        var tree = new MerkleTree(["tx1", "tx2", "tx3", "tx4", "tx5"]);

        var proof = tree.Proof(2);

        Assert.True(MerkleTree.Verify("tx3", proof, tree.Root));
        Assert.False(MerkleTree.Verify("tx3-forged", proof, tree.Root));
        Assert.True(MerkleTree.Verify("tx5", tree.Proof(4), tree.Root)); // the odd leaf out
        Assert.Equal(3, proof.Count); // log2(5) rounded up
    }

    [Fact]
    public void Snowflake_IdsAreUniqueSortedAndDecodable()
    {
        var epoch = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var time = new ManualClock { Now = new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero) };
        var generator = new SnowflakeId(workerId: 7, epoch, time);

        var ids = Enumerable.Range(0, 100).Select(_ => generator.Next()).ToList();
        time.Now += TimeSpan.FromMilliseconds(1);
        ids.Add(generator.Next());

        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.Equal(ids.Order(), ids);
        Assert.Equal((time.GetUtcNow(), 7, 0), generator.Decode(ids[^1]));
        Assert.Equal(99, generator.Decode(ids[99]).Sequence);

        time.Now -= TimeSpan.FromSeconds(1); // e.g. an NTP correction
        Assert.Throws<InvalidOperationException>(() => generator.Next());
    }

    [Fact]
    public async Task TwoPhaseCommit_CommitsOnlyWhenEveryoneAgrees()
    {
        var from = new AccountParticipant("from", 100, -30);
        var to = new AccountParticipant("to", 0, +30);
        var coordinator = new TwoPhaseCommitCoordinator(TimeSpan.FromSeconds(5));

        Assert.Equal(TransactionOutcome.Committed, await coordinator.ExecuteAsync([from, to]));
        Assert.Equal((70m, 30m), (from.Balance, to.Balance));
        Assert.Equal(["begin", "commit", "end"], coordinator.Log);
    }

    [Fact]
    public async Task TwoPhaseCommit_AbortsOnNoVoteOrTimeout()
    {
        var poor = new AccountParticipant("poor", 10, -30);
        var rich = new AccountParticipant("rich", 0, +30);
        Assert.Equal(TransactionOutcome.Aborted, await new TwoPhaseCommitCoordinator(TimeSpan.FromSeconds(5)).ExecuteAsync([poor, rich]));
        Assert.Equal((10m, 0m), (poor.Balance, rich.Balance));
        Assert.False(rich.HasPendingChange); // its prepared change was rolled back

        var slow = new AccountParticipant("slow", 100, -1, prepareDelay: TimeSpan.FromSeconds(30));
        var fast = new AccountParticipant("fast", 0, +1);
        Assert.Equal(TransactionOutcome.Aborted, await new TwoPhaseCommitCoordinator(TimeSpan.FromMilliseconds(50)).ExecuteAsync([slow, fast]));
        Assert.Equal(0m, fast.Balance);
    }
}

/// <summary>FakeTimeProvider refuses to go backwards, but real clocks sometimes do.</summary>
internal sealed class ManualClock : TimeProvider
{
    public DateTimeOffset Now { get; set; }
    public override DateTimeOffset GetUtcNow() => Now;
}
