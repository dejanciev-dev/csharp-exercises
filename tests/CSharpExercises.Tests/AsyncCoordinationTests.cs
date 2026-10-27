using CSharpExercises.Async;
using CSharpExercises.Concurrency;

namespace CSharpExercises.Tests;

public class AsyncCoordinationTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private static async Task<bool> CompletesWithin(Task task, TimeSpan time) =>
        await Task.WhenAny(task, Task.Delay(time)) == task;

    [Fact]
    public async Task AsyncLock_LetsOneCallerInAtATimeAcrossAwaits()
    {
        var gate = new AsyncLock();
        var inside = 0;
        var maxInside = 0;

        async Task Work()
        {
            using (await gate.LockAsync())
            {
                var now = Interlocked.Increment(ref inside);
                maxInside = Math.Max(maxInside, now);
                await Task.Delay(1); // the reason a plain lock cannot be used here
                Interlocked.Decrement(ref inside);
            }
        }

        await Task.WhenAll(Enumerable.Range(0, 30).Select(_ => Task.Run(Work)));

        Assert.Equal(1, maxInside);
    }

    [Fact]
    public async Task AsyncLock_WaitingCanBeCancelled()
    {
        var gate = new AsyncLock();
        using var held = await gate.LockAsync();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => gate.LockAsync(cts.Token));
    }

    [Fact]
    public async Task AsyncLock_DisposingTwiceReleasesOnce()
    {
        var gate = new AsyncLock();
        var first = await gate.LockAsync();
        first.Dispose();
        first.Dispose();

        using var second = await gate.LockAsync();
        var third = gate.LockAsync();

        Assert.False(await CompletesWithin(third, TimeSpan.FromMilliseconds(50))); // still exclusive
        second.Dispose();
        (await third.WaitAsync(Timeout)).Dispose();
    }

    [Fact]
    public async Task ResetEvent_ReleasesAllWaitersWhenSet()
    {
        var ready = new AsyncManualResetEvent();
        var waiters = Enumerable.Range(0, 10).Select(_ => ready.WaitAsync()).ToList();

        Assert.False(await CompletesWithin(Task.WhenAll(waiters), TimeSpan.FromMilliseconds(50)));

        ready.Set();

        await Task.WhenAll(waiters).WaitAsync(Timeout);
        Assert.True(ready.IsSet);
        await ready.WaitAsync().WaitAsync(Timeout); // later waiters pass straight through
    }

    [Fact]
    public async Task ResetEvent_ResetClosesTheGateAgain()
    {
        var ready = new AsyncManualResetEvent();
        ready.Set();
        ready.Reset();

        Assert.False(ready.IsSet);
        var waiter = ready.WaitAsync();
        Assert.False(await CompletesWithin(waiter, TimeSpan.FromMilliseconds(50)));

        ready.Set();
        await waiter.WaitAsync(Timeout);
    }

    [Fact]
    public async Task ResetEvent_WaitCanBeCancelled()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new AsyncManualResetEvent().WaitAsync(cts.Token));
    }

    [Fact]
    public async Task AsyncLazy_RunsTheFactoryOnceForConcurrentCallers()
    {
        var calls = 0;
        var lazy = new AsyncLazy<string>(async () =>
        {
            Interlocked.Increment(ref calls);
            await Task.Delay(20);
            return "connected";
        });

        Assert.False(lazy.IsValueCreated);
        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => lazy.GetValueAsync()));

        Assert.Equal(1, calls);
        Assert.All(results, r => Assert.Equal("connected", r));
        Assert.True(lazy.IsValueCreated);
    }

    [Fact]
    public async Task AsyncLazy_RetriesAfterAFailure()
    {
        var attempts = 0;
        var lazy = new AsyncLazy<int>(() => ++attempts == 1 ? throw new TimeoutException() : Task.FromResult(attempts));

        await Assert.ThrowsAsync<TimeoutException>(lazy.GetValueAsync);
        Assert.Equal(2, await lazy.GetValueAsync());
        Assert.Equal(2, await lazy.GetValueAsync());
    }

    [Fact]
    public async Task AsyncLazy_CanCacheTheFailureInstead()
    {
        var attempts = 0;
        var lazy = new AsyncLazy<int>(() => { attempts++; throw new TimeoutException(); }, retryOnFailure: false);

        await Assert.ThrowsAsync<TimeoutException>(lazy.GetValueAsync);
        await Assert.ThrowsAsync<TimeoutException>(lazy.GetValueAsync);
        Assert.Equal(1, attempts);
    }

    [Fact]
    public void LockFreeStack_IsLastInFirstOut()
    {
        var stack = new LockFreeStack<int>();
        stack.Push(1);
        stack.Push(2);

        Assert.True(stack.TryPop(out var top));
        Assert.Equal(2, top);
        Assert.True(stack.TryPop(out _));
        Assert.False(stack.TryPop(out _));
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void LockFreeStack_LosesNothingUnderContention()
    {
        const int threads = 8, perThread = 50_000;
        var stack = new LockFreeStack<int>();
        var popped = new System.Collections.Concurrent.ConcurrentBag<int>();

        // Every thread pushes its own range and pops as much as it pushes, all at the same time.
        Parallel.For(0, threads, new ParallelOptions { MaxDegreeOfParallelism = threads }, t =>
        {
            for (var i = 0; i < perThread; i++)
            {
                stack.Push(t * perThread + i);
                if (i % 2 == 1 && stack.TryPop(out var a)) popped.Add(a);
            }
        });
        while (stack.TryPop(out var rest)) popped.Add(rest);

        Assert.Equal(threads * perThread, popped.Count);
        Assert.Equal(threads * perThread, popped.Distinct().Count());
    }
}
