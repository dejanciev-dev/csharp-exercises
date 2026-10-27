using System.Runtime.ExceptionServices;

namespace CSharpExercises.ErrorHandling;

/// <summary>
/// Exceptions that are easy to lose: the extra failures from <c>Task.WhenAll</c>, and errors during cleanup.
/// </summary>
public static class ExceptionHandling
{
    /// <summary>
    /// <c>await Task.WhenAll(...)</c> rethrows only the <em>first</em> exception. This waits for every task and
    /// throws an <see cref="AggregateException"/> with all of them, or returns the results if none failed.
    /// </summary>
    public static async Task<T[]> WhenAllOrAggregateAsync<T>(IEnumerable<Task<T>> tasks)
    {
        var all = Task.WhenAll(tasks);
        try
        {
            return await all;
        }
        catch when (all.Exception is { InnerExceptions.Count: > 1 } aggregate)
        {
            throw aggregate.Flatten();
        }
    }

    /// <summary>
    /// Runs every cleanup step, even if earlier ones throw, and reports the failures afterwards.
    /// A single failure is rethrown as itself with its original stack trace (<see cref="ExceptionDispatchInfo"/>);
    /// several are wrapped in an <see cref="AggregateException"/>.
    /// </summary>
    public static void RunAll(params IEnumerable<Action> steps)
    {
        List<ExceptionDispatchInfo> failures = [];

        foreach (var step in steps)
        {
            try
            {
                step();
            }
            catch (Exception ex)
            {
                // Capture now: "throw ex;" later would reset the stack trace to this method.
                failures.Add(ExceptionDispatchInfo.Capture(ex));
            }
        }

        switch (failures)
        {
            case []:
                return;
            case [var only]:
                only.Throw();
                break;
            default:
                throw new AggregateException("Several steps failed.", failures.Select(f => f.SourceException));
        }
    }
}
