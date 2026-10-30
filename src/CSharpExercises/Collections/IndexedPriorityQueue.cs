namespace CSharpExercises.Collections;

/// <summary>
/// A min-heap that also knows where each item sits, so an item's priority can be changed in O(log n). .NET's
/// <see cref="PriorityQueue{TElement,TPriority}"/> has no "decrease key", which is why Dijkstra implementations with it
/// push duplicates and skip stale entries. With a position index the heap stays at one entry per vertex.
/// </summary>
public sealed class IndexedPriorityQueue<TItem, TPriority>(IComparer<TPriority>? comparer = null)
    where TItem : notnull
{
    private readonly List<(TItem Item, TPriority Priority)> _heap = [];
    private readonly Dictionary<TItem, int> _position = new();
    private readonly IComparer<TPriority> _comparer = comparer ?? Comparer<TPriority>.Default;

    public int Count => _heap.Count;

    public bool Contains(TItem item) => _position.ContainsKey(item);

    public TPriority PriorityOf(TItem item) => _heap[_position[item]].Priority;

    /// <summary>Adds the item, or changes its priority if it's already queued (up or down).</summary>
    public void Set(TItem item, TPriority priority)
    {
        if (_position.TryGetValue(item, out var i))
        {
            var old = _heap[i].Priority;
            _heap[i] = (item, priority);
            if (_comparer.Compare(priority, old) < 0) SiftUp(i);
            else SiftDown(i);
            return;
        }
        _heap.Add((item, priority));
        _position[item] = _heap.Count - 1;
        SiftUp(_heap.Count - 1);
    }

    public (TItem Item, TPriority Priority) Dequeue()
    {
        if (_heap.Count == 0) throw new InvalidOperationException("The queue is empty.");
        var top = _heap[0];
        RemoveAt(0);
        return top;
    }

    public bool Remove(TItem item)
    {
        if (!_position.TryGetValue(item, out var i)) return false;
        RemoveAt(i);
        return true;
    }

    /// <summary>Dijkstra with decrease-key: every vertex is in the queue at most once.</summary>
    public static Dictionary<string, int> ShortestDistances(IReadOnlyDictionary<string, (string To, int Weight)[]> graph, string source)
    {
        var distance = new Dictionary<string, int> { [source] = 0 };
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Set(source, 0);

        while (queue.Count > 0)
        {
            var (vertex, d) = queue.Dequeue();
            foreach (var (to, weight) in graph.GetValueOrDefault(vertex) ?? [])
            {
                if (distance.TryGetValue(to, out var known) && known <= d + weight) continue;
                distance[to] = d + weight;
                queue.Set(to, d + weight); // decrease-key instead of a duplicate entry
            }
        }
        return distance;
    }

    private void RemoveAt(int i)
    {
        var last = _heap.Count - 1;
        _position.Remove(_heap[i].Item);
        if (i != last)
        {
            _heap[i] = _heap[last];
            _position[_heap[i].Item] = i;
        }
        _heap.RemoveAt(last);
        if (i < _heap.Count)
        {
            SiftUp(i);
            SiftDown(i);
        }
    }

    private void SiftUp(int i)
    {
        while (i > 0)
        {
            var parent = (i - 1) / 2;
            if (_comparer.Compare(_heap[i].Priority, _heap[parent].Priority) >= 0) return;
            Swap(i, parent);
            i = parent;
        }
    }

    private void SiftDown(int i)
    {
        while (true)
        {
            var smallest = i;
            foreach (var child in new[] { 2 * i + 1, 2 * i + 2 })
                if (child < _heap.Count && _comparer.Compare(_heap[child].Priority, _heap[smallest].Priority) < 0) smallest = child;
            if (smallest == i) return;
            Swap(i, smallest);
            i = smallest;
        }
    }

    private void Swap(int a, int b)
    {
        (_heap[a], _heap[b]) = (_heap[b], _heap[a]);
        _position[_heap[a].Item] = a;
        _position[_heap[b].Item] = b;
    }
}
