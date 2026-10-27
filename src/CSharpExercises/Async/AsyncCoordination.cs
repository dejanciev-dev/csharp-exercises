namespace CSharpExercises.Async;

/// <summary>
/// Mutual exclusion that works across <c>await</c>. The <c>lock</c> statement cannot contain an await (error CS1996),
/// because the code after the await may resume on a different thread, and a Monitor must be released by the thread
/// that took it. A <see cref="SemaphoreSlim"/> has no thread affinity, so it can be held across awaits.
/// </summary>
/// <remarks>Not reentrant: awaiting <see cref="LockAsync"/> again while holding the lock waits forever.</remarks>
public sealed class AsyncLock
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>Use as <c>using (await gate.LockAsync()) { ... }</c>.</summary>
    public async Task<IDisposable> LockAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        return new Releaser(_semaphore);
    }

    private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        private SemaphoreSlim? _semaphore = semaphore;

        // Releasing twice would let two callers in at once, so a second Dispose does nothing.
        public void Dispose() => Interlocked.Exchange(ref _semaphore, null)?.Release();
    }
}

/// <summary>
/// A gate that tasks can await: closed until <see cref="Set"/>, then every current and future waiter passes until
/// <see cref="Reset"/>. Built on a <see cref="TaskCompletionSource"/>, swapped for a fresh one on reset.
/// </summary>
public sealed class AsyncManualResetEvent
{
    // RunContinuationsAsynchronously: without it, Set() runs every waiter's continuation inline, on the caller's
    // thread, before returning. A slow or blocking waiter would then stall whoever called Set, or even deadlock it.
    private TaskCompletionSource _signal = NewSignal();

    public bool IsSet => Volatile.Read(ref _signal).Task.IsCompleted;

    public Task WaitAsync(CancellationToken cancellationToken = default) =>
        Volatile.Read(ref _signal).Task.WaitAsync(cancellationToken);

    public void Set() => Volatile.Read(ref _signal).TrySetResult();

    public void Reset()
    {
        while (true)
        {
            var current = Volatile.Read(ref _signal);
            // Only a completed signal needs replacing; the compare-exchange fails if another thread replaced it first.
            if (!current.Task.IsCompleted || Interlocked.CompareExchange(ref _signal, NewSignal(), current) == current) return;
        }
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}

/// <summary>
/// Runs an async factory once, on first use, and gives every caller the same result. Unlike <c>Lazy&lt;Task&lt;T&gt;&gt;</c>,
/// a failed attempt is not cached forever (unless asked): a connection that failed during a network blip is retried
/// by the next caller.
/// </summary>
public sealed class AsyncLazy<T>(Func<Task<T>> factory, bool retryOnFailure = true)
{
    private Lazy<Task<T>> _lazy = Create(factory);

    public bool IsValueCreated
    {
        get
        {
            // && short-circuits, so Value (which would start the factory) is only read once the Lazy has been created.
            // A property pattern would not promise that order.
            var lazy = Volatile.Read(ref _lazy);
            return lazy.IsValueCreated && lazy.Value.IsCompletedSuccessfully;
        }
    }

    public async Task<T> GetValueAsync()
    {
        var current = Volatile.Read(ref _lazy);
        try
        {
            return await current.Value;
        }
        catch when (retryOnFailure)
        {
            // Swap in a fresh Lazy, but only if no one else already has: concurrent failures trigger one retry, not many.
            Interlocked.CompareExchange(ref _lazy, Create(factory), current);
            throw;
        }
    }

    // Task.Run: the factory's synchronous part (before its first await) runs on the thread pool, not inside
    // whichever caller happened to come first.
    private static Lazy<Task<T>> Create(Func<Task<T>> factory) => new(() => Task.Run(factory));
}
