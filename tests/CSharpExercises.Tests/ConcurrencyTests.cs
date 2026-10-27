using CSharpExercises.Concurrency;

namespace CSharpExercises.Tests;

public class ConcurrencyTests
{
    private const int Threads = 8;
    private const int IncrementsPerThread = 100_000;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private static async Task<bool> CompletesWithin(Task task, TimeSpan time) =>
        await Task.WhenAny(task, Task.Delay(time)) == task;

    private static void HammerFromManyThreads(Action action)
    {
        // The barrier releases every thread at once, so they really do run at the same time.
        using var start = new Barrier(Threads);
        var threads = Enumerable.Range(0, Threads)
            .Select(_ => new Thread(() =>
            {
                start.SignalAndWait();
                for (var i = 0; i < IncrementsPerThread; i++) action();
            }))
            .ToList();

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());
    }

    [Fact]
    public void UnsafeCounter_CanLoseUpdates()
    {
        var counter = new UnsafeCounter();

        HammerFromManyThreads(counter.Increment);

        // Usually far below the expected total on a multi-core machine, but a race is not guaranteed to happen,
        // so the only safe assertion is that it never counts too many.
        Assert.InRange(counter.Value, 1, Threads * IncrementsPerThread);
    }

    [Theory]
    [InlineData(typeof(LockedCounter))]
    [InlineData(typeof(InterlockedCounter))]
    public void ThreadSafeCounters_NeverLoseUpdates(Type counterType)
    {
        var counter = (ICounter)Activator.CreateInstance(counterType)!;

        HammerFromManyThreads(counter.Increment);

        Assert.Equal(Threads * IncrementsPerThread, counter.Value);
    }

    [Fact]
    public void AtomicMax_KeepsTheLargestValueUnderContention()
    {
        var max = 0;

        Parallel.For(0, 100_000, i => Atomic.Max(ref max, i));

        Assert.Equal(99_999, max);
    }

    [Fact]
    public void AtomicMax_IgnoresSmallerValues()
    {
        var max = 10;

        Assert.Equal(10, Atomic.Max(ref max, 3));
        Assert.Equal(15, Atomic.Max(ref max, 15));
        Assert.Equal(15, max);
    }

    [Fact]
    public async Task BoundedBuffer_DeliversEveryItemExactlyOnce()
    {
        var buffer = new BoundedBuffer<int>(capacity: 4);
        var received = new System.Collections.Concurrent.ConcurrentBag<int>();

        var consumers = Enumerable.Range(0, 3)
            .Select(_ => Task.Run(() =>
            {
                foreach (var item in buffer.GetConsumingEnumerable()) received.Add(item);
            }))
            .ToArray();
        var producers = Enumerable.Range(0, 2)
            .Select(p => Task.Run(() =>
            {
                for (var i = 0; i < 1_000; i++) buffer.Add(p * 1_000 + i);
            }))
            .ToArray();

        await Task.WhenAll(producers);
        buffer.CompleteAdding();
        await Task.WhenAll(consumers).WaitAsync(Timeout);

        Assert.Equal(Enumerable.Range(0, 2_000), received.Order());
    }

    [Fact]
    public async Task BoundedBuffer_AddBlocksWhileFull()
    {
        var buffer = new BoundedBuffer<string>(capacity: 1);
        buffer.Add("first");

        var blockedAdd = Task.Run(() => buffer.Add("second"));

        Assert.False(await CompletesWithin(blockedAdd, TimeSpan.FromMilliseconds(100)));

        Assert.True(buffer.TryTake(out var taken));
        Assert.Equal("first", taken);
        await blockedAdd.WaitAsync(Timeout);
        Assert.Equal(1, buffer.Count);
    }

    [Fact]
    public async Task BoundedBuffer_CompleteAddingReleasesWaitingConsumers()
    {
        var buffer = new BoundedBuffer<int>(capacity: 2);
        var waitingTake = Task.Run(() => buffer.TryTake(out _));

        Assert.False(await CompletesWithin(waitingTake, TimeSpan.FromMilliseconds(100)));

        buffer.CompleteAdding();

        Assert.False(await waitingTake.WaitAsync(Timeout));
        Assert.Throws<InvalidOperationException>(() => buffer.Add(1));
    }

    [Fact]
    public void BoundedBuffer_RejectsInvalidCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BoundedBuffer<int>(0));
    }

    [Fact]
    public void OnceCache_RunsFactoryOncePerKeyUnderContention()
    {
        var calls = 0;
        var cache = new OnceCache<string, string>(key =>
        {
            Interlocked.Increment(ref calls);
            Thread.Sleep(50); // keep the window open so other threads arrive while the value is being built
            return key.ToUpperInvariant();
        });

        using var start = new Barrier(Threads);
        var results = new string[Threads];
        var threads = Enumerable.Range(0, Threads)
            .Select(i => new Thread(() =>
            {
                start.SignalAndWait();
                results[i] = cache.GetOrCreate("report");
            }))
            .ToList();
        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        Assert.Equal(1, calls);
        Assert.All(results, r => Assert.Equal("REPORT", r));
    }

    [Fact]
    public void OnceCache_RetriesAfterTheFactoryFails()
    {
        var attempts = 0;
        var cache = new OnceCache<int, string>(key =>
            ++attempts == 1 ? throw new TimeoutException("database busy") : $"value {key}");

        Assert.Throws<TimeoutException>(() => cache.GetOrCreate(1));
        Assert.Equal(0, cache.Count);

        Assert.Equal("value 1", cache.GetOrCreate(1));
        Assert.Equal("value 1", cache.GetOrCreate(1));
        Assert.Equal(2, attempts);
    }

    [Fact]
    public void OnceCache_InvalidateForcesARebuild()
    {
        var version = 0;
        var cache = new OnceCache<string, int>(_ => ++version);

        Assert.Equal(1, cache.GetOrCreate("config"));
        Assert.Equal(1, cache.GetOrCreate("config"));

        Assert.True(cache.Invalidate("config"));
        Assert.Equal(2, cache.GetOrCreate("config"));
    }
}
