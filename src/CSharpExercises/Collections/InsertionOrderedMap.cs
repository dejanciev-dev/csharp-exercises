using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace CSharpExercises.Collections;

/// <summary>
/// A dictionary that remembers insertion order, like Python's dict or Java's LinkedHashMap: a hash map from key to a
/// node in a linked list of entries. Lookups, inserts and removals stay O(1), and enumeration follows insertion order
/// (a plain <see cref="Dictionary{TKey,TValue}"/> happens to do this until you remove something, but doesn't promise it).
/// Optionally, reading a key moves it to the end, which turns the map into an LRU cache.
/// </summary>
public sealed class InsertionOrderedMap<TKey, TValue>(bool moveToEndOnAccess = false) : IEnumerable<KeyValuePair<TKey, TValue>>
    where TKey : notnull
{
    private readonly Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> _index = new();
    private readonly LinkedList<KeyValuePair<TKey, TValue>> _order = new();

    public int Count => _index.Count;

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException($"Key '{key}' not found.");
        set
        {
            if (_index.TryGetValue(key, out var node))
            {
                node.Value = new(key, value); // updating keeps the original position
                return;
            }
            _index[key] = _order.AddLast(new KeyValuePair<TKey, TValue>(key, value));
        }
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (!_index.TryGetValue(key, out var node))
        {
            value = default;
            return false;
        }
        if (moveToEndOnAccess)
        {
            _order.Remove(node);
            _order.AddLast(node);
        }
        value = node.Value.Value;
        return true;
    }

    public bool Remove(TKey key)
    {
        if (!_index.Remove(key, out var node)) return false;
        _order.Remove(node);
        return true;
    }

    public KeyValuePair<TKey, TValue> First => _order.First?.Value ?? throw new InvalidOperationException("The map is empty.");

    /// <summary>Removes and returns the oldest entry: with move-to-end on access, the least recently used one.</summary>
    public KeyValuePair<TKey, TValue> RemoveFirst()
    {
        var first = First;
        Remove(first.Key);
        return first;
    }

    public IEnumerable<TKey> Keys => _order.Select(p => p.Key);

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _order.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
