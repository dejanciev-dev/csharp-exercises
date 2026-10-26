namespace CSharpExercises.Testing;

/// <summary>
/// Allows at most <paramref name="maxRequests"/> in any <paramref name="window"/>-long period.
/// Reads the time from <see cref="TimeProvider"/>, so tests can move the clock instead of sleeping.
/// </summary>
public sealed class SlidingWindowRateLimiter(int maxRequests, TimeSpan window, TimeProvider time)
{
    private readonly Queue<DateTimeOffset> _accepted = new();

    public bool TryAcquire()
    {
        var now = time.GetUtcNow();
        DropExpired(now);

        if (_accepted.Count >= maxRequests) return false;

        _accepted.Enqueue(now);
        return true;
    }

    /// <summary>How long until the next request would be allowed (zero if it would be allowed now).</summary>
    public TimeSpan RetryAfter
    {
        get
        {
            var now = time.GetUtcNow();
            DropExpired(now);
            return _accepted.Count < maxRequests ? TimeSpan.Zero : _accepted.Peek() + window - now;
        }
    }

    private void DropExpired(DateTimeOffset now)
    {
        while (_accepted.Count > 0 && now - _accepted.Peek() >= window) _accepted.Dequeue();
    }
}

/// <summary>
/// Runs <paramref name="action"/> only once calls to <see cref="Trigger"/> have stopped for <paramref name="delay"/>,
/// e.g. searching only after the user pauses typing. The timer comes from <see cref="TimeProvider"/>,
/// so a fake clock can fire it in tests.
/// </summary>
public sealed class Debouncer(TimeSpan delay, Action action, TimeProvider time) : IDisposable
{
    private readonly Lock _lock = new();
    private ITimer? _timer;
    private bool _disposed;

    public void Trigger()
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // Each call restarts the countdown.
            _timer ??= time.CreateTimer(_ => action(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _timer.Change(delay, Timeout.InfiniteTimeSpan);
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _disposed = true;
            _timer?.Dispose();
        }
    }
}
