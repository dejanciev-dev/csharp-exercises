using System.Collections.Concurrent;

namespace CSharpExercises.Caching;

/// <summary>
/// Collapses concurrent requests for the same key into one: while a load is running, everyone else asking for that key
/// waits for the same task instead of starting their own. This prevents a "cache stampede", where a popular entry
/// expires and a thousand requests hit the database at the same moment.
/// </summary>
/// <remarks>
/// Unlike <c>OnceCache</c>, nothing is kept once the load finishes: the next request after that starts a fresh load.
/// Pair it with a cache (see <see cref="CachedLookup{TKey, TValue}"/>) to keep results.
/// </remarks>
public sealed class SingleFlight<TKey, TValue> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, Lazy<Task<TValue>>> _inFlight = new();

    public int InFlightCount => _inFlight.Count;

    /// <summary>
    /// Runs <paramref name="load"/> unless a load for <paramref name="key"/> is already running, in which case it waits for that one.
    /// <paramref name="cancellationToken"/> only stops <em>this caller</em> waiting: the shared load keeps running for the
    /// others, which is why <paramref name="load"/> is not given the token.
    /// </summary>
    public async Task<TValue> RunAsync(TKey key, Func<TKey, Task<TValue>> load, CancellationToken cancellationToken = default)
    {
        // Lazy makes sure only one load starts even if two threads both add for the same key at the same time.
        var lazy = _inFlight.GetOrAdd(key, k => new Lazy<Task<TValue>>(() => load(k)));
        var task = lazy.Value;

        // Remove the entry when the load completes (successfully or not), whoever is still waiting.
        // Removing this exact pair means a newer load for the same key is never removed by mistake.
        _ = task.ContinueWith(
            _ => _inFlight.TryRemove(new KeyValuePair<TKey, Lazy<Task<TValue>>>(key, lazy)),
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

        return await task.WaitAsync(cancellationToken);
    }
}
