namespace CSharpExercises.Async;

/// <summary>
/// Running many I/O-bound operations at once with <see cref="Task.WhenAll(IEnumerable{Task})"/>,
/// limiting concurrency with a <see cref="SemaphoreSlim"/>, and racing requests with <see cref="Task.WhenAny(IEnumerable{Task})"/>.
/// The fetch function is injected, so it can be an HttpClient call in real code and a fake in tests.
/// </summary>
public class ParallelDownloader(Func<string, CancellationToken, Task<string>> fetch)
{
    public async Task<IReadOnlyDictionary<string, string>> DownloadAllAsync(
        IEnumerable<string> urls, CancellationToken cancellationToken = default)
    {
        var distinct = urls.Distinct().ToArray();
        var contents = await Task.WhenAll(distinct.Select(url => fetch(url, cancellationToken)));
        return distinct.Zip(contents).ToDictionary(p => p.First, p => p.Second);
    }

    /// <summary>Like <see cref="DownloadAllAsync"/>, but never runs more than <paramref name="maxConcurrency"/> requests at a time.</summary>
    public async Task<IReadOnlyDictionary<string, string>> DownloadThrottledAsync(
        IEnumerable<string> urls, int maxConcurrency, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConcurrency);

        using var gate = new SemaphoreSlim(maxConcurrency);
        var distinct = urls.Distinct().ToArray();
        var contents = await Task.WhenAll(distinct.Select(async url =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                return await fetch(url, cancellationToken);
            }
            finally
            {
                gate.Release();
            }
        }));

        return distinct.Zip(contents).ToDictionary(p => p.First, p => p.Second);
    }

    /// <summary>Asks every mirror at once and returns the first successful response, cancelling the rest.</summary>
    public async Task<string> FirstToRespondAsync(IEnumerable<string> mirrors, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var pending = mirrors.Select(m => fetch(m, cts.Token)).ToList();
        if (pending.Count == 0) throw new ArgumentException("At least one mirror is required.", nameof(mirrors));

        var failures = new List<Exception>();
        while (pending.Count > 0)
        {
            var finished = await Task.WhenAny(pending);
            pending.Remove(finished);

            if (finished.IsCompletedSuccessfully)
            {
                await cts.CancelAsync();
                return finished.Result;
            }

            failures.Add(finished.Exception?.InnerException ?? new OperationCanceledException());
        }

        cancellationToken.ThrowIfCancellationRequested();
        throw new AggregateException("All mirrors failed.", failures);
    }
}
