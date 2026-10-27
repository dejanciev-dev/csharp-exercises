using System.Collections.Concurrent;

namespace CSharpExercises.Concurrency;

/// <summary>
/// Thread-safe cache that runs the factory at most once per key, even when many threads ask at the same time.
/// </summary>
/// <remarks>
/// <c>ConcurrentDictionary.GetOrAdd(key, factory)</c> alone does not guarantee that: two threads that miss together
/// both run the factory and one result is thrown away. Storing a <see cref="Lazy{T}"/> instead means both threads
/// get the same <c>Lazy</c>, and only the first <c>.Value</c> runs the factory.
/// </remarks>
public sealed class OnceCache<TKey, TValue>(Func<TKey, TValue> factory) where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, Lazy<TValue>> _entries = new();

    public int Count => _entries.Count;

    public TValue GetOrCreate(TKey key)
    {
        var lazy = _entries.GetOrAdd(key, k => new Lazy<TValue>(() => factory(k), LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            return lazy.Value;
        }
        catch
        {
            // Lazy<T> caches exceptions. Remove this exact entry (not one another thread may have added since)
            // so the next call retries instead of failing forever.
            _entries.TryRemove(new KeyValuePair<TKey, Lazy<TValue>>(key, lazy));
            throw;
        }
    }

    public bool Invalidate(TKey key) => _entries.TryRemove(key, out _);
}
