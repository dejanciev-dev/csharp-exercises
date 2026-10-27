using System.Collections;
using System.Numerics;

namespace CSharpExercises.Hashing;

/// <summary>
/// A hash map from scratch, using open addressing with linear probing: all entries live in one array, and a collision
/// moves on to the next slot. Compare with <see cref="Dictionary{TKey, TValue}"/>, which chains colliding entries instead.
/// </summary>
public sealed class OpenAddressingMap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> where TKey : notnull
{
    private enum SlotState : byte { Empty, Occupied, Deleted }

    private struct Slot
    {
        public SlotState State;
        public int Hash;
        public TKey Key;
        public TValue Value;
    }

    // Count live + deleted slots against the limit: tombstones make probe sequences longer just like real entries.
    private const double MaxLoadFactor = 0.7;

    private readonly IEqualityComparer<TKey> _comparer;
    private Slot[] _slots;
    private int _tombstones;

    public OpenAddressingMap(int capacity = 8, IEqualityComparer<TKey>? comparer = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _slots = new Slot[(int)BitOperations.RoundUpToPowerOf2((uint)capacity)];
        _comparer = comparer ?? EqualityComparer<TKey>.Default;
    }

    public int Count { get; private set; }

    /// <summary>Always a power of two, so <c>hash &amp; (Capacity - 1)</c> replaces the slower <c>hash % Capacity</c>.</summary>
    public int Capacity => _slots.Length;

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException($"The key '{key}' was not found.");
        set => Insert(key, value, overwrite: true);
    }

    public bool TryAdd(TKey key, TValue value) => Insert(key, value, overwrite: false);

    public bool ContainsKey(TKey key) => TryGetValue(key, out _);

    public bool TryGetValue(TKey key, out TValue value)
    {
        var index = Find(key, Hash(key));
        if (index < 0)
        {
            value = default!;
            return false;
        }

        value = _slots[index].Value;
        return true;
    }

    /// <summary>
    /// Marks the slot as deleted rather than empty. Emptying it would break the probe chain: a key that collided and
    /// was placed after it would no longer be found, because lookups stop at the first empty slot.
    /// </summary>
    public bool Remove(TKey key)
    {
        var index = Find(key, Hash(key));
        if (index < 0) return false;

        _slots[index] = new Slot { State = SlotState.Deleted };
        Count--;
        _tombstones++;
        return true;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (var slot in _slots)
        {
            if (slot.State == SlotState.Occupied) yield return new(slot.Key, slot.Value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Spreads the high bits of the hash into the low ones. The index uses only the low bits, so without this, keys whose
    /// hash codes differ only in high bits (common for some types) would all land in the same few slots.
    /// </summary>
    private int Hash(TKey key)
    {
        var hash = _comparer.GetHashCode(key);
        return (hash ^ (hash >>> 16)) & int.MaxValue;
    }

    private int Find(TKey key, int hash)
    {
        var mask = _slots.Length - 1;
        for (var index = hash & mask; ; index = (index + 1) & mask)
        {
            ref var slot = ref _slots[index];
            if (slot.State == SlotState.Empty) return -1;
            if (slot.State == SlotState.Occupied && slot.Hash == hash && _comparer.Equals(slot.Key, key)) return index;
        }
    }

    private bool Insert(TKey key, TValue value, bool overwrite)
    {
        if (Count + _tombstones + 1 > _slots.Length * MaxLoadFactor) Resize();

        var hash = Hash(key);
        var mask = _slots.Length - 1;
        var firstDeleted = -1;

        for (var index = hash & mask; ; index = (index + 1) & mask)
        {
            ref var slot = ref _slots[index];

            if (slot.State == SlotState.Occupied && slot.Hash == hash && _comparer.Equals(slot.Key, key))
            {
                if (!overwrite) return false;
                slot.Value = value;
                return true;
            }

            if (slot.State == SlotState.Deleted && firstDeleted < 0) firstDeleted = index;

            if (slot.State == SlotState.Empty)
            {
                // The key is not in the map. Reuse the first tombstone on the way, if any, to keep chains short.
                if (firstDeleted >= 0)
                {
                    index = firstDeleted;
                    _tombstones--;
                }

                _slots[index] = new Slot { State = SlotState.Occupied, Hash = hash, Key = key, Value = value };
                Count++;
                return true;
            }
        }
    }

    /// <summary>
    /// Rebuilds the table. It doubles when live entries are filling it; when it is mostly tombstones,
    /// rebuilding at the same size is enough to clear them out.
    /// </summary>
    private void Resize()
    {
        var newCapacity = Count + 1 > _slots.Length * MaxLoadFactor / 2 ? _slots.Length * 2 : _slots.Length;
        var old = _slots;

        _slots = new Slot[newCapacity];
        Count = 0;
        _tombstones = 0;

        foreach (var slot in old)
        {
            if (slot.State == SlotState.Occupied) Insert(slot.Key, slot.Value, overwrite: false);
        }
    }
}
