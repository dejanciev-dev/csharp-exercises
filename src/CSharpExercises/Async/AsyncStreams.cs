using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Channels;

namespace CSharpExercises.Async;

/// <summary>
/// Producing and consuming data asynchronously with <see cref="IAsyncEnumerable{T}"/> and channels.
/// </summary>
public static class AsyncStreams
{
    /// <summary>Yields <paramref name="from"/> down to 0, waiting <paramref name="interval"/> between values.</summary>
    public static async IAsyncEnumerable<int> CountdownAsync(
        int from, TimeSpan interval, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var i = from; i >= 0; i--)
        {
            yield return i;
            if (i > 0) await Task.Delay(interval, cancellationToken);
        }
    }

    /// <summary>Groups an async stream into arrays of <paramref name="size"/>; the last batch may be smaller.</summary>
    public static async IAsyncEnumerable<T[]> BatchAsync<T>(
        this IAsyncEnumerable<T> source, int size, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        var batch = new List<T>(size);
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            batch.Add(item);
            if (batch.Count == size)
            {
                yield return batch.ToArray();
                batch.Clear();
            }
        }

        if (batch.Count > 0) yield return batch.ToArray();
    }

    /// <summary>
    /// Producer/consumer pipeline: one producer writes items into a bounded channel and
    /// <paramref name="workerCount"/> workers process them concurrently. Results are returned in completion order.
    /// If any worker fails, everything else is cancelled and that worker's exception is rethrown.
    /// </summary>
    public static async Task<IReadOnlyList<TResult>> ProcessWithWorkersAsync<T, TResult>(
        IEnumerable<T> items,
        Func<T, CancellationToken, Task<TResult>> process,
        int workerCount,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(workerCount);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var channel = Channel.CreateBounded<T>(workerCount * 2);
        var results = new ConcurrentBag<TResult>();

        var producer = Task.Run(async () =>
        {
            try
            {
                foreach (var item in items) await channel.Writer.WriteAsync(item, cts.Token);
            }
            finally
            {
                channel.Writer.Complete();
            }
        }, cts.Token);

        var workers = Enumerable.Range(0, workerCount).Select(_ => Task.Run(async () =>
        {
            try
            {
                await foreach (var item in channel.Reader.ReadAllAsync(cts.Token))
                {
                    results.Add(await process(item, cts.Token));
                }
            }
            catch
            {
                // Stop the producer and the other workers so nothing waits forever on a full channel.
                await cts.CancelAsync();
                throw;
            }
        }, cts.Token));

        var all = Task.WhenAll(workers.Append(producer));
        try
        {
            await all;
        }
        catch when (all.Exception is { } aggregate)
        {
            // Surface the real failure rather than the cancellations it caused.
            var root = aggregate.InnerExceptions.FirstOrDefault(e => e is not OperationCanceledException)
                       ?? aggregate.InnerExceptions[0];
            ExceptionDispatchInfo.Throw(root);
        }

        return results.ToList();
    }
}
