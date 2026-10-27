namespace CSharpExercises.Concurrency;

/// <summary>
/// Three counters that look the same from the outside but behave very differently when
/// several threads call <see cref="Increment"/> at once.
/// </summary>
public interface ICounter
{
    void Increment();
    long Value { get; }
}

/// <summary>
/// <c>_value++</c> is read, add, write: two threads can read the same value and one update is lost.
/// </summary>
public sealed class UnsafeCounter : ICounter
{
    private long _value;

    public void Increment() => _value++;
    public long Value => _value;
}

/// <summary>Correct under contention: only one thread at a time runs the increment.</summary>
public sealed class LockedCounter : ICounter
{
    private readonly Lock _gate = new();
    private long _value;

    public void Increment()
    {
        lock (_gate) _value++;
    }

    public long Value
    {
        get { lock (_gate) return _value; }
    }
}

/// <summary>Correct and lock-free: the CPU does the read-add-write as one atomic instruction.</summary>
public sealed class InterlockedCounter : ICounter
{
    private long _value;

    public void Increment() => Interlocked.Increment(ref _value);

    // A 64-bit read is not guaranteed to be atomic on 32-bit platforms.
    public long Value => Interlocked.Read(ref _value);
}

public static class Atomic
{
    /// <summary>
    /// Sets <paramref name="target"/> to <paramref name="value"/> if it is larger, without a lock.
    /// The compare-and-swap loop retries whenever another thread changed the value in between.
    /// Returns the value <paramref name="target"/> holds afterwards.
    /// </summary>
    public static int Max(ref int target, int value)
    {
        var current = Volatile.Read(ref target);
        while (value > current)
        {
            var seen = Interlocked.CompareExchange(ref target, value, current);
            if (seen == current) return value;
            current = seen;
        }
        return current;
    }
}
