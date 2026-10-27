namespace CSharpExercises.Caching;

/// <summary>
/// A cache whose entries expire. Absolute expiration removes an entry a fixed time after it was stored;
/// sliding expiration removes it after a period without reads, so popular entries stay and idle ones go.
/// The clock comes from <see cref="TimeProvider"/>, so tests move time forward instead of sleeping.
/// </summary>
public sealed class TtlCache<TKey, TValue>(TimeProvider time) where TKey : notnull
{
    private sealed class Entry(TValue value, DateTimeOffset absoluteExpiry, TimeSpan? sliding, DateTimeOffset now)
    {
        public TValue Value { get; } = value;
        public DateTimeOffset AbsoluteExpiry { get; } = absoluteExpiry;
        public TimeSpan? Sliding { get; } = sliding;
        public DateTimeOffset LastAccess { get; set; } = now;

        public bool IsExpired(DateTimeOffset now) =>
            now >= AbsoluteExpiry || (Sliding is { } sliding && now - LastAccess >= sliding);
    }

    private readonly Lock _gate = new();
    private readonly Dictionary<TKey, Entry> _entries = new();

    public int Hits { get; private set; }
    public int Misses { get; private set; }

    /// <summary>Entries held, including expired ones not yet removed. Call <see cref="Compact"/> to drop those.</summary>
    public int Count
    {
        get { lock (_gate) return _entries.Count; }
    }

    /// <summary>
    /// Stores a value for at most <paramref name="absoluteExpiration"/>. With <paramref name="slidingExpiration"/> it also
    /// expires early if it is not read for that long. The absolute limit still applies, so a constantly-read entry is
    /// refreshed from the source now and then instead of living forever.
    /// </summary>
    public void Set(TKey key, TValue value, TimeSpan absoluteExpiration, TimeSpan? slidingExpiration = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(absoluteExpiration, TimeSpan.Zero);

        var now = time.GetUtcNow();
        lock (_gate) _entries[key] = new Entry(value, now + absoluteExpiration, slidingExpiration, now);
    }

    public bool TryGet(TKey key, out TValue value)
    {
        var now = time.GetUtcNow();
        lock (_gate)
        {
            if (_entries.TryGetValue(key, out var entry))
            {
                if (!entry.IsExpired(now))
                {
                    entry.LastAccess = now; // reading keeps a sliding entry alive
                    Hits++;
                    value = entry.Value;
                    return true;
                }

                // Expiry is lazy: an expired entry is only removed when someone looks for it (or on Compact).
                _entries.Remove(key);
            }

            Misses++;
            value = default!;
            return false;
        }
    }

    public bool Remove(TKey key)
    {
        lock (_gate) return _entries.Remove(key);
    }

    /// <summary>Removes every expired entry, freeing memory held by keys that are never asked for again. Returns how many.</summary>
    public int Compact()
    {
        var now = time.GetUtcNow();
        lock (_gate)
        {
            var expired = _entries.Where(e => e.Value.IsExpired(now)).Select(e => e.Key).ToList();
            expired.ForEach(key => _entries.Remove(key));
            return expired.Count;
        }
    }
}
