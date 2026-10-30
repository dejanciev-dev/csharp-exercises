using System.Collections;

namespace CSharpExercises.Collections;

/// <summary>
/// A double-ended queue on a circular array: O(1) amortised push and pop at both ends, and O(1) indexing. The head
/// index wraps around, and when the array fills up it is copied into a bigger one, unwrapped.
/// </summary>
public sealed class Deque<T> : IReadOnlyList<T>
{
    private T[] _items;
    private int _head; // index of the first element
    private int _version;

    public Deque(int capacity = 8) => _items = new T[Math.Max(capacity, 1)];

    public int Count { get; private set; }

    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
            return _items[Physical(index)];
        }
    }

    public void PushFront(T item)
    {
        GrowIfFull();
        _head = (_head - 1 + _items.Length) % _items.Length;
        _items[_head] = item;
        Count++;
        _version++;
    }

    public void PushBack(T item)
    {
        GrowIfFull();
        _items[Physical(Count)] = item;
        Count++;
        _version++;
    }

    public T PopFront()
    {
        if (Count == 0) throw new InvalidOperationException("The deque is empty.");
        var item = _items[_head];
        _items[_head] = default!; // let the GC collect it
        _head = (_head + 1) % _items.Length;
        Count--;
        _version++;
        return item;
    }

    public T PopBack()
    {
        if (Count == 0) throw new InvalidOperationException("The deque is empty.");
        var index = Physical(Count - 1);
        var item = _items[index];
        _items[index] = default!;
        Count--;
        _version++;
        return item;
    }

    public T PeekFront() => Count > 0 ? _items[_head] : throw new InvalidOperationException("The deque is empty.");
    public T PeekBack() => Count > 0 ? _items[Physical(Count - 1)] : throw new InvalidOperationException("The deque is empty.");

    public IEnumerator<T> GetEnumerator()
    {
        var version = _version;
        for (var i = 0; i < Count; i++)
        {
            if (version != _version) throw new InvalidOperationException("The deque was modified during enumeration.");
            yield return _items[Physical(i)];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private int Physical(int index) => (_head + index) % _items.Length;

    private void GrowIfFull()
    {
        if (Count < _items.Length) return;
        var bigger = new T[_items.Length * 2];
        for (var i = 0; i < Count; i++) bigger[i] = _items[Physical(i)];
        _items = bigger;
        _head = 0;
    }

    /// <summary>A palindrome check that compares from both ends, the classic deque exercise.</summary>
    public static bool IsPalindrome(string text)
    {
        var deque = new Deque<char>();
        foreach (var c in text.Where(char.IsLetterOrDigit)) deque.PushBack(char.ToLowerInvariant(c));
        while (deque.Count > 1)
            if (deque.PopFront() != deque.PopBack()) return false;
        return true;
    }
}
