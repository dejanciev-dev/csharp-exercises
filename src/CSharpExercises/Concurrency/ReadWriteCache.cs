namespace CSharpExercises.Concurrency;

/// <summary>
/// A cache that is read constantly and written rarely, guarded by <see cref="ReaderWriterLockSlim"/>: any number of
/// readers at once, writers alone. The upgradeable read lock handles "read, and write only on a miss" without a gap
/// in which another thread could insert the same key. Every acquire is paired with a release in finally.
/// </summary>
public sealed class ReadWriteCache<TKey, TValue> : IDisposable where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _items = new();
    private readonly ReaderWriterLockSlim _lock = new();

    public int FactoryCalls => Volatile.Read(ref _factoryCalls);
    private int _factoryCalls;

    public bool TryGet(TKey key, out TValue value)
    {
        _lock.EnterReadLock();
        try
        {
            return _items.TryGetValue(key, out value!);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Set(TKey key, TValue value)
    {
        _lock.EnterWriteLock();
        try
        {
            _items[key] = value;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Only one thread may hold the upgradeable lock, so two threads missing on the same key can't both run the
    /// factory; plain readers still proceed in parallel until the upgrade to a write lock.
    /// </summary>
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
    {
        if (TryGet(key, out var cached)) return cached; // fast path: shared read lock only

        _lock.EnterUpgradeableReadLock();
        try
        {
            if (_items.TryGetValue(key, out var existing)) return existing; // someone added it meanwhile
            Interlocked.Increment(ref _factoryCalls);
            var value = factory(key);

            _lock.EnterWriteLock();
            try
            {
                _items[key] = value;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
            return value;
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }

    public int Count
    {
        get
        {
            _lock.EnterReadLock();
            try { return _items.Count; }
            finally { _lock.ExitReadLock(); }
        }
    }

    public void Dispose() => _lock.Dispose();
}
