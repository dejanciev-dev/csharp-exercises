namespace CSharpExercises.Async;

/// <summary>
/// An async lock per key: operations on the same key (one customer's account, one file) run one at a time, while
/// different keys run in parallel. Each key gets a <see cref="SemaphoreSlim"/> with a reference count, and the entry is
/// removed when the last user releases it, so memory doesn't grow with every key ever seen.
/// </summary>
public sealed class KeyedLock<TKey> where TKey : notnull
{
    private sealed class Entry
    {
        public readonly SemaphoreSlim Semaphore = new(1, 1);
        public int References;
    }

    private readonly Dictionary<TKey, Entry> _entries = new();
    private readonly Lock _gate = new();

    public int ActiveKeys
    {
        get
        {
            lock (_gate) return _entries.Count;
        }
    }

    public async Task<IDisposable> AcquireAsync(TKey key, CancellationToken cancellationToken = default)
    {
        Entry entry;
        lock (_gate)
        {
            if (!_entries.TryGetValue(key, out entry!)) _entries[key] = entry = new Entry();
            entry.References++;
        }

        try
        {
            await entry.Semaphore.WaitAsync(cancellationToken);
        }
        catch
        {
            Release(key, entry, held: false); // cancelled while waiting: give back the reference only
            throw;
        }
        return new Releaser(this, key, entry);
    }

    private void Release(TKey key, Entry entry, bool held)
    {
        lock (_gate)
        {
            if (held) entry.Semaphore.Release();
            if (--entry.References == 0) _entries.Remove(key);
        }
    }

    private sealed class Releaser(KeyedLock<TKey> owner, TKey key, Entry entry) : IDisposable
    {
        private int _disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) owner.Release(key, entry, held: true); // safe to dispose twice
        }
    }
}
