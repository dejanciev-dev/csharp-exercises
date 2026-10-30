namespace CSharpExercises.Async;

/// <summary>A legacy API with completion callbacks, as older libraries and native wrappers often have.</summary>
public sealed class LegacyDownloader
{
    public event EventHandler<string>? Completed;
    public event EventHandler<Exception>? Failed;

    public void Start(string url, Action<string>? onSuccess = null, Action<Exception>? onError = null)
    {
        ThreadPool.QueueUserWorkItem(_ =>
        {
            Thread.Sleep(10);
            if (url.Contains("bad"))
            {
                var error = new HttpRequestException($"404 for {url}");
                onError?.Invoke(error);
                Failed?.Invoke(this, error);
            }
            else
            {
                onSuccess?.Invoke($"content of {url}");
                Completed?.Invoke(this, $"content of {url}");
            }
        });
    }
}

/// <summary>
/// Wrapping callback- and event-based APIs as tasks with <see cref="TaskCompletionSource{TResult}"/>, so callers can
/// await them, combine them with WhenAll and cancel them. The details matter: RunContinuationsAsynchronously so the
/// callback thread isn't hijacked by the awaiting code, unsubscribing from events afterwards, and cancellation that
/// completes the task even if the callback never comes.
/// </summary>
public static class CallbackAdapters
{
    public static Task<string> DownloadAsync(this LegacyDownloader downloader, string url, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

        // TrySet*: whichever of result, error or cancellation comes first wins; later ones are ignored.
        downloader.Start(url, result => tcs.TrySetResult(result), error => tcs.TrySetException(error));
        tcs.Task.ContinueWith(_ => registration.Dispose(), TaskScheduler.Default);
        return tcs.Task;
    }

    /// <summary>Awaits the next raising of an event, then unsubscribes so the handler doesn't leak.</summary>
    public static Task<T> NextEventAsync<T>(Action<EventHandler<T>> subscribe, Action<EventHandler<T>> unsubscribe, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<T>? handler = null;
        handler = (_, args) =>
        {
            unsubscribe(handler!);
            tcs.TrySetResult(args);
        };
        subscribe(handler);
        cancellationToken.Register(() =>
        {
            unsubscribe(handler);
            tcs.TrySetCanceled(cancellationToken);
        });
        return tcs.Task;
    }

    /// <summary>A Task that completes after a delay, on a timer instead of a blocked thread: how Task.Delay works inside.</summary>
    public static Task DelayAsync(TimeSpan delay, TimeProvider time)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ITimer? timer = null;
        timer = time.CreateTimer(_ =>
        {
            tcs.TrySetResult();
            timer?.Dispose();
        }, null, delay, Timeout.InfiniteTimeSpan);
        return tcs.Task;
    }
}
