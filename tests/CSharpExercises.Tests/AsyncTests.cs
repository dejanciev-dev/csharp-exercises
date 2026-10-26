using CSharpExercises.Async;

namespace CSharpExercises.Tests;

public class AsyncTests
{
    /// <summary>Fake web server that records how many requests run at the same time.</summary>
    private sealed class FakeServer(TimeSpan latency)
    {
        private int _active;
        private int _maxConcurrent;

        public int MaxConcurrent => _maxConcurrent;

        public async Task<string> FetchAsync(string url, CancellationToken cancellationToken)
        {
            var active = Interlocked.Increment(ref _active);
            InterlockedMax(ref _maxConcurrent, active);
            try
            {
                await Task.Delay(latency, cancellationToken);
                return url.Contains("broken") ? throw new HttpRequestException($"{url} failed") : $"<{url}>";
            }
            finally
            {
                Interlocked.Decrement(ref _active);
            }
        }

        private static void InterlockedMax(ref int target, int value)
        {
            int current;
            while (value > (current = target) && Interlocked.CompareExchange(ref target, value, current) != current) { }
        }
    }

    private static async Task<List<T>> CollectAsync<T>(IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (var item in source) list.Add(item);
        return list;
    }

    [Fact]
    public async Task DownloadAll_RunsRequestsConcurrently()
    {
        var server = new FakeServer(TimeSpan.FromMilliseconds(50));
        var downloader = new ParallelDownloader(server.FetchAsync);

        var result = await downloader.DownloadAllAsync(["a", "b", "c", "d", "e", "a"]);

        Assert.Equal(5, result.Count);
        Assert.Equal("<c>", result["c"]);
        Assert.Equal(5, server.MaxConcurrent);
    }

    [Fact]
    public async Task DownloadThrottled_NeverExceedsLimit()
    {
        var server = new FakeServer(TimeSpan.FromMilliseconds(20));
        var downloader = new ParallelDownloader(server.FetchAsync);

        var result = await downloader.DownloadThrottledAsync(Enumerable.Range(1, 8).Select(i => $"page{i}"), maxConcurrency: 2);

        Assert.Equal(8, result.Count);
        Assert.Equal(2, server.MaxConcurrent);
    }

    [Fact]
    public async Task FirstToRespond_ReturnsFastestAndCancelsTheRest()
    {
        var slowCancelled = new TaskCompletionSource();
        var downloader = new ParallelDownloader(async (url, ct) =>
        {
            if (url == "fast") return "fast response";

            ct.Register(() => slowCancelled.TrySetResult());
            await Task.Delay(Timeout.Infinite, ct);
            return "slow response";
        });

        var result = await downloader.FirstToRespondAsync(["slow", "fast"]);

        Assert.Equal("fast response", result);
        await slowCancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task FirstToRespond_SkipsFailedMirrors()
    {
        var downloader = new ParallelDownloader(new FakeServer(TimeSpan.FromMilliseconds(10)).FetchAsync);

        Assert.Equal("<backup>", await downloader.FirstToRespondAsync(["broken-1", "backup"]));
    }

    [Fact]
    public async Task FirstToRespond_AllFail_ThrowsAggregate()
    {
        var downloader = new ParallelDownloader(new FakeServer(TimeSpan.Zero).FetchAsync);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => downloader.FirstToRespondAsync(["broken-1", "broken-2"]));
        Assert.Equal(2, ex.InnerExceptions.Count);
    }

    [Fact]
    public async Task Retry_SucceedsAfterTransientFailures()
    {
        var attempts = 0;

        var result = await Resilience.RetryAsync(_ =>
            ++attempts < 3 ? throw new HttpRequestException("flaky") : Task.FromResult("ok"),
            maxAttempts: 5, initialDelay: TimeSpan.FromMilliseconds(1));

        Assert.Equal("ok", result);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task Retry_GivesUpAfterMaxAttempts()
    {
        var attempts = 0;

        await Assert.ThrowsAsync<HttpRequestException>(() => Resilience.RetryAsync<string>(_ =>
        {
            attempts++;
            throw new HttpRequestException("down");
        }, maxAttempts: 3, initialDelay: TimeSpan.FromMilliseconds(1)));

        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task Retry_StopsWhenCancelled()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Resilience.RetryAsync<string>(
            _ => throw new HttpRequestException("down"),
            maxAttempts: 100, initialDelay: TimeSpan.FromSeconds(10), cts.Token));
    }

    [Fact]
    public async Task WithTimeout_ReturnsResultWhenFastEnough()
    {
        var result = await Resilience.WithTimeoutAsync(async ct =>
        {
            await Task.Delay(1, ct);
            return 42;
        }, TimeSpan.FromSeconds(5));

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task WithTimeout_ThrowsTimeoutExceptionWhenTooSlow() =>
        await Assert.ThrowsAsync<TimeoutException>(() => Resilience.WithTimeoutAsync(async ct =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return 0;
        }, TimeSpan.FromMilliseconds(20)));

    [Fact]
    public async Task WithTimeout_CallerCancellationIsNotATimeout()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Resilience.WithTimeoutAsync(async ct =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return 0;
        }, TimeSpan.FromSeconds(30), cts.Token));
    }

    [Fact]
    public async Task Countdown_YieldsValuesInOrder() =>
        Assert.Equal([3, 2, 1, 0], await CollectAsync(AsyncStreams.CountdownAsync(3, TimeSpan.FromMilliseconds(1))));

    [Fact]
    public async Task Countdown_CanBeCancelledMidStream()
    {
        using var cts = new CancellationTokenSource();
        var seen = new List<int>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var n in AsyncStreams.CountdownAsync(10, TimeSpan.FromMilliseconds(1), cts.Token))
            {
                seen.Add(n);
                if (n == 8) await cts.CancelAsync();
            }
        });

        Assert.Equal([10, 9, 8], seen);
    }

    [Fact]
    public async Task Batch_GroupsStreamIntoChunks()
    {
        var batches = await CollectAsync(AsyncStreams.CountdownAsync(6, TimeSpan.Zero).BatchAsync(3));

        Assert.Equal(3, batches.Count);
        Assert.Equal([6, 5, 4], batches[0]);
        Assert.Equal([0], batches[2]);
    }

    [Fact]
    public async Task Workers_ProcessEveryItem()
    {
        var results = await AsyncStreams.ProcessWithWorkersAsync(
            Enumerable.Range(1, 50),
            async (n, ct) =>
            {
                await Task.Delay(1, ct);
                return n * n;
            },
            workerCount: 4);

        Assert.Equal(Enumerable.Range(1, 50).Select(n => n * n), results.Order());
    }

    [Fact]
    public async Task Workers_FailureStopsPipelineAndSurfacesError()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => AsyncStreams.ProcessWithWorkersAsync(
            Enumerable.Range(1, 1000),
            async (n, ct) =>
            {
                await Task.Yield();
                return n == 7 ? throw new InvalidOperationException("bad item 7") : n;
            },
            workerCount: 3).WaitAsync(TimeSpan.FromSeconds(10)));

        Assert.Equal("bad item 7", ex.Message);
    }
}
