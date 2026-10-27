using System.Buffers;
using System.Collections.Concurrent;

namespace CSharpExercises.Resources;

/// <summary>
/// Reuses objects that are expensive to create instead of allocating new ones and leaving the old ones to the GC.
/// </summary>
public sealed class ObjectPool<T> where T : class
{
    private readonly ConcurrentBag<T> _items = [];
    private readonly Func<T> _create;
    private readonly Action<T>? _reset;
    private readonly int _maxRetained;
    private int _created;

    public ObjectPool(Func<T> create, Action<T>? reset = null, int maxRetained = 16)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRetained);
        _create = create;
        _reset = reset;
        _maxRetained = maxRetained;
    }

    /// <summary>How many objects the factory has built in total. Low numbers mean the pool is doing its job.</summary>
    public int Created => Volatile.Read(ref _created);

    public int Available => _items.Count;

    /// <summary>
    /// Takes an object out of the pool, creating one if it is empty. Dispose the lease to give it back:
    /// <c>using var lease = pool.Rent();</c>
    /// </summary>
    public Lease Rent()
    {
        if (!_items.TryTake(out var item))
        {
            item = _create();
            Interlocked.Increment(ref _created);
        }
        return new Lease(this, item);
    }

    private void Return(T item)
    {
        // Reset before it can be handed out again, so no state leaks from one user to the next.
        _reset?.Invoke(item);

        // Keep at most _maxRetained; extras are left to the GC so a burst does not pin memory forever.
        // (Count and Add are not atomic together, so the limit can be overshot slightly under contention.)
        if (_items.Count < _maxRetained) _items.Add(item);
    }

    /// <summary>
    /// A struct so renting allocates nothing. Disposing returns the object; disposing twice is a no-op
    /// only for the same copy, so do not copy leases around.
    /// </summary>
    public struct Lease : IDisposable
    {
        private ObjectPool<T>? _pool;

        internal Lease(ObjectPool<T> pool, T value)
        {
            _pool = pool;
            Value = value;
        }

        public T Value { get; }

        public void Dispose()
        {
            _pool?.Return(Value);
            _pool = null;
        }
    }
}

public static class StreamCopier
{
    /// <summary>
    /// Copies a stream with a buffer rented from <see cref="ArrayPool{T}.Shared"/> instead of a new array per call.
    /// The pool may hand out a larger array than asked for, and the buffer must go back even if copying throws.
    /// </summary>
    public static async Task<long> CopyAsync(Stream source, Stream destination, int bufferSize = 81_920, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            long total = 0;
            int read;
            while ((read = await source.ReadAsync(buffer.AsMemory(0, bufferSize), cancellationToken)) > 0)
            {
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                total += read;
            }
            return total;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
