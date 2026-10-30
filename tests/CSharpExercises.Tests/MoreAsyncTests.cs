using CSharpExercises.Async;
using Microsoft.Extensions.Time.Testing;

namespace CSharpExercises.Tests;

public class MoreAsyncTests
{
    [Fact]
    public async Task Batcher_FlushesBySize()
    {
        var batches = new List<int>();
        await using var batcher = new AsyncBatcher<int>(b => { lock (batches) batches.Add(b.Count); return Task.CompletedTask; }, maxSize: 10, maxDelay: TimeSpan.FromSeconds(30));

        await Task.WhenAll(Enumerable.Range(0, 35).Select(batcher.AddAsync).Take(30)); // three full batches

        Assert.Equal([10, 10, 10], batches);
    }

    [Fact]
    public async Task Batcher_FlushesByTimeAndOnDispose()
    {
        var flushed = new List<int>();
        var batcher = new AsyncBatcher<int>(b => { lock (flushed) flushed.AddRange(b); return Task.CompletedTask; }, maxSize: 100, maxDelay: TimeSpan.FromMilliseconds(50));

        await batcher.AddAsync(1).WaitAsync(TimeSpan.FromSeconds(5)); // flushed by the timer, not by size
        var pending = batcher.AddAsync(2);
        await batcher.DisposeAsync();
        await pending;

        Assert.Equal([1, 2], flushed);
        Assert.Throws<InvalidOperationException>(() => { _ = batcher.AddAsync(3); }); // throws synchronously
    }

    [Fact]
    public async Task Batcher_ReportsFlushFailuresToEveryCaller()
    {
        await using var batcher = new AsyncBatcher<int>(_ => throw new IOException("db down"), maxSize: 2, maxDelay: TimeSpan.FromSeconds(30));

        var a = batcher.AddAsync(1);
        var b = batcher.AddAsync(2);

        await Assert.ThrowsAsync<IOException>(() => a);
        await Assert.ThrowsAsync<IOException>(() => b);
    }

    [Fact]
    public async Task KeyedLock_SerialisesPerKeyButNotAcrossKeys()
    {
        var locks = new KeyedLock<string>();
        var concurrentPerKey = new Dictionary<string, int> { ["a"] = 0, ["b"] = 0 };
        var maxPerKey = 0;
        var maxOverall = 0;
        var overall = 0;

        async Task Work(string key)
        {
            using (await locks.AcquireAsync(key))
            {
                lock (concurrentPerKey)
                {
                    maxPerKey = Math.Max(maxPerKey, ++concurrentPerKey[key]);
                    maxOverall = Math.Max(maxOverall, ++overall);
                }
                await Task.Delay(5);
                lock (concurrentPerKey) { concurrentPerKey[key]--; overall--; }
            }
        }

        await Task.WhenAll(Enumerable.Range(0, 20).Select(i => Work(i % 2 == 0 ? "a" : "b")));

        Assert.Equal(1, maxPerKey);
        Assert.Equal(2, maxOverall);
        Assert.Equal(0, locks.ActiveKeys); // entries cleaned up
    }

    [Fact]
    public async Task KeyedLock_CancellationWhileWaiting()
    {
        var locks = new KeyedLock<int>();
        var held = await locks.AcquireAsync(1);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => locks.AcquireAsync(1, cts.Token));
        held.Dispose();
        held.Dispose(); // idempotent

        using (await locks.AcquireAsync(1)) Assert.Equal(1, locks.ActiveKeys);
        Assert.Equal(0, locks.ActiveKeys);
    }

    [Fact]
    public async Task PeriodicJob_RunsOnEachTickAndSurvivesFailures()
    {
        var time = new FakeTimeProvider();
        var calls = 0;
        await using var job = new PeriodicJob(_ =>
        {
            if (Interlocked.Increment(ref calls) == 2) throw new InvalidOperationException("flaky");
            return Task.CompletedTask;
        }, TimeSpan.FromMinutes(1), time);
        job.Start();

        for (var i = 0; i < 3; i++)
        {
            time.Advance(TimeSpan.FromMinutes(1));
            await WaitUntil(() => job.Runs == i + 1);
        }

        Assert.Equal(3, job.Runs);
        Assert.Equal(1, job.Failures);
        Assert.Equal("flaky", job.LastError!.Message);
    }

    [Fact]
    public async Task Adapters_TurnCallbacksIntoTasks()
    {
        var downloader = new LegacyDownloader();

        Assert.Equal("content of a", await downloader.DownloadAsync("a"));
        await Assert.ThrowsAsync<HttpRequestException>(() => downloader.DownloadAsync("bad"));

        var results = await Task.WhenAll(downloader.DownloadAsync("x"), downloader.DownloadAsync("y"));
        Assert.Equal(["content of x", "content of y"], results);

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => downloader.DownloadAsync("slow", cts.Token));
    }

    [Fact]
    public async Task Adapters_AwaitAnEventAndATimer()
    {
        var downloader = new LegacyDownloader();
        var next = CallbackAdapters.NextEventAsync<string>(h => downloader.Completed += h, h => downloader.Completed -= h);
        downloader.Start("e");
        Assert.Equal("content of e", await next.WaitAsync(TimeSpan.FromSeconds(5)));

        var time = new FakeTimeProvider();
        var delay = CallbackAdapters.DelayAsync(TimeSpan.FromHours(1), time);
        Assert.False(delay.IsCompleted);
        time.Advance(TimeSpan.FromHours(1));
        await delay.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Progress_ReportsAndThrottles()
    {
        var files = new[] { ("a", new byte[5000]), ("b", new byte[3000]) };
        var all = new List<CopyProgress>();
        var throttled = new List<CopyProgress>();
        var time = new FakeTimeProvider();
        var throttle = new ThrottledProgress<CopyProgress>(new SynchronousProgress<CopyProgress>(throttled.Add), TimeSpan.FromSeconds(1), time, p => p.FilesDone == p.FilesTotal);
        var destination = new Dictionary<string, byte[]>();

        await ProgressReporting.CopyFilesAsync(files, destination, new SynchronousProgress<CopyProgress>(p => { all.Add(p); throttle.Report(p); }));

        Assert.Equal(2, destination.Count);
        Assert.Equal(100, all[^1].Percent);
        Assert.True(all.Count > 8);
        Assert.Equal(2, throttled.Count); // the first report, then nothing until the final one (time stood still)
        Assert.Equal(100, throttled[^1].Percent);
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException();
            await Task.Delay(5);
        }
    }
}
