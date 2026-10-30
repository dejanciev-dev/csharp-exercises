using System.Collections;

namespace CSharpExercises.Collections;

/// <summary>
/// A bidirectional map: look up by key or by value, both in O(1). Two dictionaries kept in sync, where the hard part
/// is the invariant: a value can belong to only one key, so every write has to check and update both sides together.
/// </summary>
public sealed class BiMap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    where TKey : notnull
    where TValue : notnull
{
    private readonly Dictionary<TKey, TValue> _forward;
    private readonly Dictionary<TValue, TKey> _reverse;

    public BiMap(IEqualityComparer<TKey>? keyComparer = null, IEqualityComparer<TValue>? valueComparer = null)
    {
        _forward = new Dictionary<TKey, TValue>(keyComparer);
        _reverse = new Dictionary<TValue, TKey>(valueComparer);
    }

    public int Count => _forward.Count;

    public TValue this[TKey key] => _forward[key];

    public TKey KeyOf(TValue value) => _reverse[value];

    public bool TryGetValue(TKey key, out TValue value) => _forward.TryGetValue(key, out value!);
    public bool TryGetKey(TValue value, out TKey key) => _reverse.TryGetValue(value, out key!);

    /// <summary>Adds a pair; fails if either the key or the value is already used.</summary>
    public void Add(TKey key, TValue value)
    {
        if (_forward.ContainsKey(key)) throw new ArgumentException($"Key '{key}' is already mapped.", nameof(key));
        if (_reverse.ContainsKey(value)) throw new ArgumentException($"Value '{value}' is already mapped.", nameof(value));
        _forward.Add(key, value);
        _reverse.Add(value, key);
    }

    /// <summary>Sets the pair, removing any existing pair that used the same key or the same value.</summary>
    public void Set(TKey key, TValue value)
    {
        RemoveKey(key);
        RemoveValue(value);
        Add(key, value);
    }

    public bool RemoveKey(TKey key)
    {
        if (!_forward.Remove(key, out var value)) return false;
        _reverse.Remove(value);
        return true;
    }

    public bool RemoveValue(TValue value)
    {
        if (!_reverse.Remove(value, out var key)) return false;
        _forward.Remove(key);
        return true;
    }

    /// <summary>The same pairs seen from the other side.</summary>
    public BiMap<TValue, TKey> Inverse()
    {
        var inverse = new BiMap<TValue, TKey>(_reverse.Comparer, _forward.Comparer);
        foreach (var (key, value) in _forward) inverse.Add(value, key);
        return inverse;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _forward.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
