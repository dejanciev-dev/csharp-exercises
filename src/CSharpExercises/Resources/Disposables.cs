using System.Text;
using CSharpExercises.ErrorHandling;

namespace CSharpExercises.Resources;

/// <summary>
/// A file that deletes itself when disposed: the basic <see cref="IDisposable"/> pattern for a sealed class.
/// Dispose can be called any number of times, and using the object afterwards throws <see cref="ObjectDisposedException"/>.
/// </summary>
public sealed class TempFile : IDisposable
{
    private readonly string _path;
    private bool _disposed;

    public TempFile(string? contents = null)
    {
        _path = System.IO.Path.GetTempFileName();
        if (contents is not null) File.WriteAllText(_path, contents);
    }

    public string Path
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _path;
        }
    }

    public string ReadAll() => File.ReadAllText(Path);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        File.Delete(_path);
    }
}

/// <summary>
/// Owns several disposables and disposes them in reverse order, like nested <c>using</c> blocks.
/// Every item is disposed even if one throws; the failures are reported afterwards.
/// </summary>
public sealed class CompositeDisposable : IDisposable
{
    private readonly Lock _gate = new();
    private readonly List<IDisposable> _items = [];
    private bool _disposed;

    public int Count
    {
        get { lock (_gate) return _items.Count; }
    }

    /// <summary>Takes ownership of <paramref name="item"/>. If this is already disposed, the item is disposed right away.</summary>
    public T Add<T>(T item) where T : IDisposable
    {
        lock (_gate)
        {
            if (!_disposed)
            {
                _items.Add(item);
                return item;
            }
        }

        item.Dispose();
        return item;
    }

    public void Dispose()
    {
        IDisposable[] items;
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            items = [.. _items];
            _items.Clear();
        }

        // Dispose outside the lock: Dispose methods can do anything, including call back into this object.
        // Enumerable.Reverse explicitly: in C# 14 `items.Reverse()` binds to the in-place Span version, which returns void.
        ExceptionHandling.RunAll(Enumerable.Reverse(items).Select(item => (Action)item.Dispose));
    }
}

/// <summary>
/// Buffers lines and writes them in batches. <see cref="IAsyncDisposable"/> lets <c>await using</c> flush the last
/// batch without blocking a thread; <see cref="IDisposable"/> is kept for callers that cannot await.
/// </summary>
public sealed class BatchWriter(TextWriter output, int batchSize) : IAsyncDisposable, IDisposable
{
    private readonly List<string> _pending = [];
    private bool _disposed;

    public int BatchesWritten { get; private set; }

    public async ValueTask WriteLineAsync(string line)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _pending.Add(line);
        if (_pending.Count >= batchSize) await FlushAsync();
    }

    public async ValueTask FlushAsync()
    {
        if (_pending.Count == 0) return;

        var batch = new StringBuilder();
        foreach (var line in _pending) batch.AppendLine(line);
        _pending.Clear();

        await output.WriteAsync(batch.ToString());
        await output.FlushAsync();
        BatchesWritten++;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await FlushAsync();
        _disposed = true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        // The sync path has to block. That is why `await using` is preferred when a type offers both.
        FlushAsync().AsTask().GetAwaiter().GetResult();
        _disposed = true;
    }
}
