namespace CSharpExercises.Reliability;

public sealed class BulkheadRejectedException() : Exception("The bulkhead is full.");

/// <summary>
/// A bulkhead caps how many calls to one dependency run at once, plus a short queue, and rejects the rest straight
/// away. Named after a ship's watertight compartments: one slow dependency can then only exhaust its own slots,
/// not every thread and connection in the process.
/// </summary>
public sealed class Bulkhead(int maxConcurrency, int maxQueue)
{
    private readonly SemaphoreSlim _slots = new(maxConcurrency, maxConcurrency);
    private int _waiting; // running + queued, to decide on rejection without racing
    private int _rejected;

    public int Running => maxConcurrency - _slots.CurrentCount;
    public int Rejected => Volatile.Read(ref _rejected);

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        if (Interlocked.Increment(ref _waiting) > maxConcurrency + maxQueue)
        {
            Interlocked.Decrement(ref _waiting);
            Interlocked.Increment(ref _rejected);
            throw new BulkheadRejectedException();
        }

        try
        {
            await _slots.WaitAsync(cancellationToken);
            try
            {
                return await action(cancellationToken);
            }
            finally
            {
                _slots.Release();
            }
        }
        finally
        {
            Interlocked.Decrement(ref _waiting);
        }
    }
}
