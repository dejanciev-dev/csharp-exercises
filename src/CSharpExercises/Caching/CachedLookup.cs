namespace CSharpExercises.Caching;

/// <summary>
/// The cache-aside pattern: look in the cache, and on a miss load from the source and store the result.
/// Built from <see cref="TtlCache{TKey, TValue}"/> (keeps results for a while) and
/// <see cref="SingleFlight{TKey, TValue}"/> (one load per key at a time).
/// </summary>
/// <remarks>
/// "Not found" is cached too, for a shorter time. Otherwise requests for an id that does not exist would all go to the
/// database: a common way to overload one by accident, or on purpose.
/// </remarks>
public sealed class CachedLookup<TKey, TValue>(
    Func<TKey, CancellationToken, Task<TValue?>> load,
    TimeProvider time,
    TimeSpan foundTtl,
    TimeSpan notFoundTtl)
    where TKey : notnull
    where TValue : class
{
    private readonly TtlCache<TKey, TValue?> _cache = new(time);
    private readonly SingleFlight<TKey, TValue?> _flight = new();
    private int _loads;

    /// <summary>How many times the source was actually called.</summary>
    public int Loads => Volatile.Read(ref _loads);

    public int CacheHits => _cache.Hits;

    public async Task<TValue?> GetAsync(TKey key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGet(key, out var cached)) return cached;

        return await _flight.RunAsync(key, async k =>
        {
            Interlocked.Increment(ref _loads);
            // The load is shared by every waiting caller, so no single caller's token may cancel it.
            var value = await load(k, CancellationToken.None);
            _cache.Set(k, value, value is null ? notFoundTtl : foundTtl);
            return value;
        }, cancellationToken);
    }

    /// <summary>
    /// Call after writing to the source, so the next read loads the new value. (A load that was already running when the
    /// write happened can still store the old value; production caches guard against that with version numbers.)
    /// </summary>
    public void Invalidate(TKey key) => _cache.Remove(key);
}
