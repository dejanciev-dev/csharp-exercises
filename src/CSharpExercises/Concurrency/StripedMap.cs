namespace CSharpExercises.Concurrency;

/// <summary>
/// Lock striping: instead of one lock for the whole map (every thread queues behind it) or one per key (too many
/// objects), keys hash to one of N stripes, each with its own lock and dictionary. Threads touching different stripes
/// never wait for each other. This was the design of Java's original ConcurrentHashMap, and .NET's
/// ConcurrentDictionary still uses a lock array sized by "concurrency level".
/// </summary>
public sealed class StripedMap<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, TValue>[] _buckets;
    private readonly Lock[] _locks;

    public StripedMap(int stripes = 16)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(stripes, 1);
        _buckets = Enumerable.Range(0, stripes).Select(_ => new Dictionary<TKey, TValue>()).ToArray();
        _locks = Enumerable.Range(0, stripes).Select(_ => new Lock()).ToArray();
    }

    public int Stripes => _locks.Length;

    public void Set(TKey key, TValue value)
    {
        var s = StripeOf(key);
        lock (_locks[s]) _buckets[s][key] = value;
    }

    public bool TryGet(TKey key, out TValue value)
    {
        var s = StripeOf(key);
        lock (_locks[s]) return _buckets[s].TryGetValue(key, out value!);
    }

    /// <summary>Read-modify-write under the key's stripe lock, so concurrent updates to one key are never lost.</summary>
    public TValue AddOrUpdate(TKey key, TValue addValue, Func<TValue, TValue> update)
    {
        var s = StripeOf(key);
        lock (_locks[s])
        {
            var bucket = _buckets[s];
            var value = bucket.TryGetValue(key, out var existing) ? update(existing) : addValue;
            bucket[key] = value;
            return value;
        }
    }

    /// <summary>
    /// A consistent count needs every stripe locked at once. Locks are always taken in the same order (stripe 0 first),
    /// which is what prevents deadlock between two threads doing this at the same time.
    /// </summary>
    public int Count
    {
        get
        {
            var taken = 0;
            try
            {
                for (; taken < _locks.Length; taken++) _locks[taken].Enter();
                return _buckets.Sum(b => b.Count);
            }
            finally
            {
                for (var i = taken - 1; i >= 0; i--) _locks[i].Exit();
            }
        }
    }

    public int StripeOf(TKey key) => (key.GetHashCode() & int.MaxValue) % _locks.Length;
}
