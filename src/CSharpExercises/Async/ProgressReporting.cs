namespace CSharpExercises.Async;

public readonly record struct CopyProgress(long BytesDone, long BytesTotal, int FilesDone, int FilesTotal)
{
    public double Percent => BytesTotal == 0 ? 100 : 100.0 * BytesDone / BytesTotal;
}

/// <summary>
/// Reporting progress from async work with <see cref="IProgress{T}"/>: the worker only calls Report and doesn't know
/// who listens or on which thread. <see cref="Progress{T}"/> marshals to the creating context (the UI thread in a
/// desktop app). Reporting on every byte would flood the listener, so a throttling wrapper passes on at most one
/// report per interval, always including the final one.
/// </summary>
public static class ProgressReporting
{
    public static async Task CopyFilesAsync(IReadOnlyList<(string Name, byte[] Data)> files, IDictionary<string, byte[]> destination,
        IProgress<CopyProgress>? progress = null, int chunkSize = 1024, CancellationToken cancellationToken = default)
    {
        var total = files.Sum(f => (long)f.Data.Length);
        long done = 0;
        for (var i = 0; i < files.Count; i++)
        {
            var (name, data) = files[i];
            var copy = new byte[data.Length];
            for (var offset = 0; offset < data.Length; offset += chunkSize)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var length = Math.Min(chunkSize, data.Length - offset);
                Array.Copy(data, offset, copy, offset, length);
                done += length;
                progress?.Report(new CopyProgress(done, total, i, files.Count));
                await Task.Yield(); // pretend each chunk is real I/O
            }
            destination[name] = copy;
            progress?.Report(new CopyProgress(done, total, i + 1, files.Count));
        }
    }
}

/// <summary>Forwards at most one report per interval, plus the last one (100% must always arrive).</summary>
public sealed class ThrottledProgress<T>(IProgress<T> inner, TimeSpan interval, TimeProvider? time = null, Func<T, bool>? isFinal = null) : IProgress<T>
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private readonly Lock _lock = new();
    private long _lastReport = long.MinValue;

    public void Report(T value)
    {
        lock (_lock)
        {
            var now = _time.GetTimestamp();
            var final = isFinal?.Invoke(value) ?? false;
            if (!final && _lastReport != long.MinValue && _time.GetElapsedTime(_lastReport, now) < interval) return;
            _lastReport = now;
        }
        inner.Report(value);
    }
}

/// <summary>An IProgress that reports synchronously, for tests and console apps (Progress&lt;T&gt; posts asynchronously).</summary>
public sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
{
    public void Report(T value) => handler(value);
}
