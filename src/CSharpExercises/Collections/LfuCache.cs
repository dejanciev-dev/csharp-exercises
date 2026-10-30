namespace CSharpExercises.Collections;

/// <summary>
/// A least-frequently-used cache with O(1) get and put. LRU evicts what wasn't used recently; LFU evicts what is used
/// least often, so a burst of one-off reads can't flush the popular entries. Keys are grouped into one linked list per
/// use count, and the smallest count is tracked so eviction never has to search. Ties go to the least recently used.
/// </summary>
public sealed class LfuCache<TKey, TValue> where TKey : notnull
{
    private sealed record Entry(TKey Key, TValue Value, int Frequency);

    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<Entry>> _nodes = new();
    private readonly Dictionary<int, LinkedList<Entry>> _byFrequency = new();
    private int _minFrequency;

    public LfuCache(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _capacity = capacity;
    }

    public int Count => _nodes.Count;

    public bool TryGet(TKey key, out TValue value)
    {
        if (!_nodes.TryGetValue(key, out var node))
        {
            value = default!;
            return false;
        }
        value = Touch(node, node.Value.Value).Value.Value;
        return true;
    }

    public void Put(TKey key, TValue value)
    {
        if (_nodes.TryGetValue(key, out var existing))
        {
            Touch(existing, value);
            return;
        }

        if (_nodes.Count == _capacity)
        {
            var victims = _byFrequency[_minFrequency];
            var victim = victims.First!; // oldest among the least used
            victims.RemoveFirst();
            if (victims.Count == 0) _byFrequency.Remove(_minFrequency);
            _nodes.Remove(victim.Value.Key);
        }

        _nodes[key] = ListFor(1).AddLast(new Entry(key, value, 1));
        _minFrequency = 1;
    }

    public int FrequencyOf(TKey key) => _nodes.TryGetValue(key, out var node) ? node.Value.Frequency : 0;

    /// <summary>Moves an entry from its frequency list to the next one up.</summary>
    private LinkedListNode<Entry> Touch(LinkedListNode<Entry> node, TValue value)
    {
        var entry = node.Value;
        var list = _byFrequency[entry.Frequency];
        list.Remove(node);
        if (list.Count == 0)
        {
            _byFrequency.Remove(entry.Frequency);
            if (_minFrequency == entry.Frequency) _minFrequency++;
        }

        var moved = ListFor(entry.Frequency + 1).AddLast(entry with { Value = value, Frequency = entry.Frequency + 1 });
        _nodes[entry.Key] = moved;
        return moved;
    }

    private LinkedList<Entry> ListFor(int frequency)
    {
        if (!_byFrequency.TryGetValue(frequency, out var list)) _byFrequency[frequency] = list = new LinkedList<Entry>();
        return list;
    }
}
