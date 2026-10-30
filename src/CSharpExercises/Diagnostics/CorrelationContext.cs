namespace CSharpExercises.Diagnostics;

/// <summary>
/// A correlation id ties together every log line produced by one request, across async code. <see cref="AsyncLocal{T}"/>
/// flows with the execution context: values set before an await are visible after it, and child tasks started with
/// Task.Run inherit them, but a value changed inside a child never leaks back to the caller (the context is copied, not
/// shared). That one-way flow is exactly what request-scoped data needs, and the opposite of a [ThreadStatic] field,
/// which is lost as soon as an await resumes on another thread.
/// </summary>
public static class CorrelationContext
{
    private static readonly AsyncLocal<string?> Current = new();

    public static string? Id => Current.Value;

    public static IDisposable Begin(string? id = null)
    {
        var previous = Current.Value;
        Current.Value = id ?? Guid.NewGuid().ToString("N")[..12];
        return new Restore(previous);
    }

    /// <summary>Runs work under a fresh id, as middleware would for each incoming request.</summary>
    public static async Task<T> RunAsync<T>(Func<Task<T>> work, string? id = null)
    {
        using (Begin(id)) return await work();
    }

    /// <summary>Starts work that must not inherit the current context (e.g. a background job unrelated to this request).</summary>
    public static Task RunDetached(Func<Task> work)
    {
        using (ExecutionContext.SuppressFlow()) return Task.Run(work);
    }

    private sealed class Restore(string? previous) : IDisposable
    {
        public void Dispose() => Current.Value = previous;
    }
}
