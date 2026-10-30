using CSharpExercises.Concurrency;

namespace CSharpExercises.Tests;

public class MoreConcurrencyTests
{
    [Fact]
    public async Task ReadWriteCache_RunsTheFactoryOncePerKey()
    {
        using var cache = new ReadWriteCache<int, string>();
        var start = new ManualResetEventSlim();

        var tasks = Enumerable.Range(0, 16).Select(_ => Task.Run(() =>
        {
            start.Wait();
            for (var i = 0; i < 1000; i++) cache.GetOrAdd(i % 50, k => $"value {k}");
        })).ToArray();
        start.Set();
        await Task.WhenAll(tasks);

        Assert.Equal(50, cache.Count);
        Assert.Equal(50, cache.FactoryCalls);
        Assert.True(cache.TryGet(7, out var seven) && seven == "value 7");
        cache.Set(7, "changed");
        Assert.True(cache.TryGet(7, out seven) && seven == "changed");
    }

    [Fact]
    public async Task Actor_ProcessesMessagesOneAtATime()
    {
        await using var account = new AccountActor();

        await Task.WhenAll(Enumerable.Range(0, 1000).Select(_ => Task.Run(() => account.Post(new AccountMessage.Deposit(1)))));
        var balance = await account.AskAsync<decimal>(new AccountMessage.GetBalance());

        Assert.Equal(1000m, balance); // no lost updates, and no lock in the actor
    }

    [Fact]
    public async Task Actor_RepliesWithErrorsAndKeepsRunning()
    {
        var account = new AccountActor();
        account.Post(new AccountMessage.Deposit(50));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => account.AskAsync<decimal>(new AccountMessage.Withdraw(80)));
        Assert.Contains("Insufficient funds", error.Message);
        Assert.Equal(20m, await account.AskAsync<decimal>(new AccountMessage.Withdraw(30)));

        account.Post(new AccountMessage.Deposit(1));
        await account.DisposeAsync(); // drains the queue first
        Assert.Equal(4, account.Processed);
        Assert.Throws<InvalidOperationException>(() => account.Post(new AccountMessage.Deposit(1)));
    }

    [Fact]
    public void ParallelAggregation_AllStrategiesAgree()
    {
        var random = new Random(1);
        var values = Enumerable.Range(0, 200_000).Select(_ => random.Next(1000)).ToArray();
        var expected = ParallelAggregation.SumSequential(values);

        Assert.Equal(expected, ParallelAggregation.SumWithSharedLock(values));
        Assert.Equal(expected, ParallelAggregation.SumWithThreadLocals(values));
        Assert.Equal(expected, ParallelAggregation.SumWithPlinq(values));

        var histogram = ParallelAggregation.Histogram(values, 10, 999);
        Assert.Equal(values.Length, histogram.Sum());
        Assert.Equal(values.Count(v => v < 100), histogram[0]);
    }

    [Fact]
    public void ParallelAggregation_CountsWords()
    {
        var counts = ParallelAggregation.WordCounts(Enumerable.Repeat("the quick fox and The lazy dog", 1000));

        Assert.Equal(2000, counts["the"]);
        Assert.Equal(1000, counts["fox"]);
    }

    [Fact]
    public void Barrier_ParallelSimulationMatchesSequential()
    {
        var rod = new double[101];
        rod[0] = 100; // hot end
        rod[^1] = 0;  // cold end

        var parallel = PhasedComputation.SimulateHeat(rod, steps: 500, workers: 4);
        var sequential = PhasedComputation.SimulateHeatSequential(rod, steps: 500);

        Assert.Equal(sequential, parallel); // bit-for-bit: same arithmetic, phases kept in step by the barrier
        Assert.True(parallel[10] > parallel[50] && parallel[50] > parallel[90]);
    }

    [Fact]
    public void Countdown_WaitsForEveryJob()
    {
        var done = new bool[50];

        var completed = PhasedComputation.RunAndWait(50, i => done[i] = true);

        Assert.Equal(50, completed);
        Assert.All(done, Assert.True);
        var ran = 0;
        var error = Assert.Throws<AggregateException>(() => PhasedComputation.RunAndWait(50, i =>
        {
            if (i == 7) throw new InvalidOperationException("boom");
            Interlocked.Increment(ref ran);
        }));
        Assert.Equal("boom", error.InnerExceptions.Single().Message);
        Assert.Equal(49, ran); // the other jobs still completed before the failure was reported
    }

    [Fact]
    public async Task StripedMap_LosesNoUpdatesUnderContention()
    {
        var map = new StripedMap<string, int>(stripes: 8);
        await Task.WhenAll(Enumerable.Range(0, 8).Select(t => Task.Run(() =>
        {
            for (var i = 0; i < 10_000; i++) map.AddOrUpdate($"key{i % 100}", 1, v => v + 1);
        })));

        Assert.Equal(100, map.Count);
        Assert.True(map.TryGet("key42", out var count));
        Assert.Equal(800, count); // 8 threads × 100 updates each
        Assert.Equal(8, Enumerable.Range(0, 1000).Select(i => map.StripeOf($"k{i}")).Distinct().Count()); // keys spread over all stripes
    }
}
