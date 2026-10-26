namespace CSharpExercises.Collections;

/// <summary>
/// A binary min-heap stored in an array: the parent of index i is (i - 1) / 2, its children are 2i + 1 and 2i + 2.
/// With a custom <see cref="IComparer{T}"/> it works as a priority queue.
/// </summary>
public class MinHeap<T>(IComparer<T>? comparer = null)
{
    private readonly IComparer<T> _comparer = comparer ?? Comparer<T>.Default;
    private readonly List<T> _items = [];

    public int Count => _items.Count;

    public bool IsEmpty => Count == 0;

    /// <summary>O(log n): add at the end, then swap upward while smaller than the parent.</summary>
    public void Push(T item)
    {
        _items.Add(item);

        var i = _items.Count - 1;
        while (i > 0)
        {
            var parent = (i - 1) / 2;
            if (_comparer.Compare(_items[i], _items[parent]) >= 0) break;

            (_items[i], _items[parent]) = (_items[parent], _items[i]);
            i = parent;
        }
    }

    public T Peek() =>
        IsEmpty ? throw new InvalidOperationException("Heap is empty.") : _items[0];

    /// <summary>O(log n): move the last item to the root, then swap downward with the smaller child.</summary>
    public T Pop()
    {
        var top = Peek();
        var last = _items[^1];
        _items.RemoveAt(_items.Count - 1);

        if (!IsEmpty)
        {
            _items[0] = last;
            SiftDown(0);
        }

        return top;
    }

    private void SiftDown(int i)
    {
        while (true)
        {
            int left = 2 * i + 1, right = left + 1, smallest = i;
            if (left < Count && _comparer.Compare(_items[left], _items[smallest]) < 0) smallest = left;
            if (right < Count && _comparer.Compare(_items[right], _items[smallest]) < 0) smallest = right;
            if (smallest == i) return;

            (_items[i], _items[smallest]) = (_items[smallest], _items[i]);
            i = smallest;
        }
    }
}
