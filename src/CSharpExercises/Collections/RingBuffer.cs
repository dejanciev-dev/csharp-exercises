using System.Collections;

namespace CSharpExercises.Collections;

/// <summary>
/// Fixed-size circular buffer: when it is full, adding overwrites the oldest item. Nothing is ever shifted or
/// reallocated, so it suits "the last N" problems: recent log lines, moving averages, undo history.
/// </summary>
public sealed class RingBuffer<T> : IReadOnlyList<T>
{
    private readonly T[] _items;
    private int _start; // index of the oldest item
    private int _version;

    public RingBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _items = new T[capacity];
    }

    public int Capacity => _items.Length;
    public int Count { get; private set; }
    public bool IsFull => Count == Capacity;

    /// <summary>Index 0 is the oldest item, <c>Count - 1</c> the newest.</summary>
    public T this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
            return _items[(_start + index) % Capacity];
        }
    }

    /// <summary>Adds an item. Returns the item that was overwritten to make room, if any.</summary>
    public bool Add(T item, out T overwritten)
    {
        _version++;
        if (IsFull)
        {
            overwritten = _items[_start];
            _items[_start] = item;
            _start = (_start + 1) % Capacity;
            return true;
        }

        _items[(_start + Count) % Capacity] = item;
        Count++;
        overwritten = default!;
        return false;
    }

    public void Add(T item) => Add(item, out _);

    /// <summary>Removes and returns the oldest item.</summary>
    public T RemoveOldest()
    {
        if (Count == 0) throw new InvalidOperationException("The buffer is empty.");

        _version++;
        var item = _items[_start];
        _items[_start] = default!; // do not keep a reference that stops the GC collecting it
        _start = (_start + 1) % Capacity;
        Count--;
        return item;
    }

    public IEnumerator<T> GetEnumerator()
    {
        var version = _version;
        for (var i = 0; i < Count; i++)
        {
            // Like List<T>, fail loudly instead of returning a mix of old and new items.
            if (version != _version) throw new InvalidOperationException("The buffer was modified during enumeration.");
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
