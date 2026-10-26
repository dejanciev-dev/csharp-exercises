namespace CSharpExercises.Async;

/// <summary>
/// Retries with exponential backoff and timeouts built on <see cref="CancellationToken"/>.
/// </summary>
public static class Resilience
{
    /// <summary>
    /// Runs <paramref name="operation"/> up to <paramref name="maxAttempts"/> times,
    /// doubling the wait between attempts. Cancellation is never retried.
    /// </summary>
    public static async Task<T> RetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        int maxAttempts,
        TimeSpan initialDelay,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxAttempts);

        var delay = initialDelay;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await operation(cancellationToken);
            }
            catch (Exception) when (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(delay, cancellationToken);
                delay *= 2;
            }
        }
    }

    /// <summary>
    /// Cancels <paramref name="operation"/> if it runs longer than <paramref name="timeout"/> and reports that
    /// as a <see cref="TimeoutException"/>. Cancellation by the caller still surfaces as <see cref="OperationCanceledException"/>.
    /// </summary>
    public static async Task<T> WithTimeoutAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            return await operation(cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"The operation did not complete within {timeout}.");
        }
    }
}
