namespace CSharpExercises.Async;

/// <summary>
/// A background job that runs on an interval with <see cref="PeriodicTimer"/>, the async-friendly timer: no overlapping
/// runs (the next tick waits for the current run), a failing run is logged and doesn't stop the job, and stopping is a
/// cancellation that ends the loop cleanly. Using a <see cref="TimeProvider"/> lets tests advance time instantly.
/// </summary>
public sealed class PeriodicJob(Func<CancellationToken, Task> work, TimeSpan interval, TimeProvider? time = null) : IAsyncDisposable
{
    private readonly CancellationTokenSource _stop = new();
    private Task? _loop;
    private int _runs;
    private int _failures;

    public int Runs => Volatile.Read(ref _runs);
    public int Failures => Volatile.Read(ref _failures);
    public Exception? LastError { get; private set; }

    public void Start() => _loop ??= RunAsync();

    private async Task RunAsync()
    {
        using var timer = new PeriodicTimer(interval, time ?? TimeProvider.System);
        try
        {
            while (await timer.WaitForNextTickAsync(_stop.Token))
            {
                try
                {
                    await work(_stop.Token);
                }
                catch (Exception e) when (e is not OperationCanceledException || !_stop.IsCancellationRequested)
                {
                    LastError = e; // keep going: one bad run shouldn't kill the job
                    Interlocked.Increment(ref _failures);
                }
                Interlocked.Increment(ref _runs);
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { } // normal shutdown
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        if (_loop is not null) await _loop;
        _stop.Dispose();
    }
}
