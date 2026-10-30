using System.Threading.Channels;

namespace CSharpExercises.Async;

/// <summary>
/// Batching small writes into bigger ones (database inserts, log shipping, metrics): items are queued and flushed
/// together when the batch is full or when the oldest item has waited long enough, whichever comes first. Callers
/// await their own item's completion, so they know it was written. One background loop owns the batch, so no locks.
/// </summary>
public sealed class AsyncBatcher<T> : IAsyncDisposable
{
    private readonly Channel<(T Item, TaskCompletionSource Done)> _queue = Channel.CreateUnbounded<(T, TaskCompletionSource)>();
    private readonly Func<IReadOnlyList<T>, Task> _flush;
    private readonly int _maxSize;
    private readonly TimeSpan _maxDelay;
    private readonly Task _loop;

    public AsyncBatcher(Func<IReadOnlyList<T>, Task> flush, int maxSize, TimeSpan maxDelay)
    {
        (_flush, _maxSize, _maxDelay) = (flush, maxSize, maxDelay);
        _loop = Task.Run(RunAsync);
    }

    public int BatchesFlushed { get; private set; }

    /// <summary>Completes when the batch containing this item has been flushed (or faults if the flush failed).</summary>
    public Task AddAsync(T item)
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_queue.Writer.TryWrite((item, done))) throw new InvalidOperationException("The batcher has been disposed.");
        return done.Task;
    }

    private async Task RunAsync()
    {
        var reader = _queue.Reader;
        while (await reader.WaitToReadAsync())
        {
            var batch = new List<(T Item, TaskCompletionSource Done)>();
            using var timeout = new CancellationTokenSource(_maxDelay); // the clock starts with the first item
            try
            {
                while (batch.Count < _maxSize)
                {
                    if (reader.TryRead(out var next)) batch.Add(next);
                    else if (!await reader.WaitToReadAsync(timeout.Token)) break; // channel completed
                }
            }
            catch (OperationCanceledException) { } // waited long enough: flush what we have

            if (batch.Count == 0) continue;
            try
            {
                await _flush(batch.Select(b => b.Item).ToList());
                BatchesFlushed++;
                batch.ForEach(b => b.Done.SetResult());
            }
            catch (Exception e)
            {
                batch.ForEach(b => b.Done.SetException(e));
            }
        }
    }

    /// <summary>Flushes whatever is still queued, then stops.</summary>
    public async ValueTask DisposeAsync()
    {
        _queue.Writer.TryComplete();
        await _loop;
    }
}
