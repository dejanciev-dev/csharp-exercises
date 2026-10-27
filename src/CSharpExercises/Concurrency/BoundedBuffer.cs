namespace CSharpExercises.Concurrency;

/// <summary>
/// Blocking producer/consumer queue built on <see cref="Monitor.Wait(object)"/> and <see cref="Monitor.PulseAll"/>:
/// producers wait while it is full, consumers wait while it is empty.
/// A hand-written version of what <c>BlockingCollection&lt;T&gt;</c> and bounded <c>Channel&lt;T&gt;</c> provide.
/// </summary>
public sealed class BoundedBuffer<T>
{
    // Monitor.Wait needs a plain object; System.Threading.Lock does not support it.
    private readonly object _gate = new();
    private readonly Queue<T> _items = new();
    private readonly int _capacity;
    private bool _addingCompleted;

    public BoundedBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _capacity = capacity;
    }

    public int Count
    {
        get { lock (_gate) return _items.Count; }
    }

    /// <summary>Adds an item, blocking while the buffer is full.</summary>
    public void Add(T item)
    {
        lock (_gate)
        {
            // Always re-check in a loop: waking up does not mean the condition is now true.
            while (_items.Count >= _capacity && !_addingCompleted) Monitor.Wait(_gate);

            if (_addingCompleted) throw new InvalidOperationException("The buffer has been marked as complete for adding.");

            _items.Enqueue(item);
            Monitor.PulseAll(_gate);
        }
    }

    /// <summary>
    /// Takes the next item, blocking while the buffer is empty.
    /// Returns <c>false</c> once adding is complete and every item has been taken.
    /// </summary>
    public bool TryTake(out T item)
    {
        lock (_gate)
        {
            while (_items.Count == 0 && !_addingCompleted) Monitor.Wait(_gate);

            if (_items.Count == 0)
            {
                item = default!;
                return false;
            }

            item = _items.Dequeue();
            Monitor.PulseAll(_gate);
            return true;
        }
    }

    /// <summary>No more items will be added: wakes every waiting consumer (and producer) so they can finish.</summary>
    public void CompleteAdding()
    {
        lock (_gate)
        {
            _addingCompleted = true;
            Monitor.PulseAll(_gate);
        }
    }

    /// <summary>Yields items as they arrive until adding is complete and the buffer is drained.</summary>
    public IEnumerable<T> GetConsumingEnumerable()
    {
        while (TryTake(out var item)) yield return item;
    }
}
