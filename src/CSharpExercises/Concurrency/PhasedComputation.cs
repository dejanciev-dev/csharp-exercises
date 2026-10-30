namespace CSharpExercises.Concurrency;

/// <summary>
/// Iterative computations where every step needs the previous step's complete results, like a heat simulation on a
/// rod: each worker updates its own slice, then all wait at a <see cref="Barrier"/> before the next step reads the
/// neighbours' values. Double buffering (read from one array, write to the other, swap) means no cell is read while
/// it's being written. A <see cref="CountdownEvent"/> covers the simpler "wait until N things are done" case.
/// </summary>
public static class PhasedComputation
{
    /// <summary>Each cell moves toward the average of its neighbours; the two ends are held at fixed temperatures.</summary>
    public static double[] SimulateHeat(double[] initial, int steps, int workers)
    {
        var current = initial.ToArray();
        var next = initial.ToArray();
        var n = current.Length;

        // The post-phase action runs once, on one thread, after everyone arrives: the safe place to swap buffers.
        using var barrier = new Barrier(workers, _ => (current, next) = (next, current));

        var threads = Enumerable.Range(0, workers).Select(w => new Thread(() =>
        {
            var from = Math.Max(1, w * n / workers);
            var to = Math.Min(n - 1, (w + 1) * n / workers);
            for (var step = 0; step < steps; step++)
            {
                var (read, write) = (current, next); // captured fresh each phase, after the swap
                for (var i = from; i < to; i++) write[i] = read[i] + 0.25 * (read[i - 1] - 2 * read[i] + read[i + 1]);
                barrier.SignalAndWait();
            }
        })).ToList();

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());
        return current;
    }

    public static double[] SimulateHeatSequential(double[] initial, int steps)
    {
        var current = initial.ToArray();
        var next = initial.ToArray();
        for (var step = 0; step < steps; step++)
        {
            for (var i = 1; i < current.Length - 1; i++) next[i] = current[i] + 0.25 * (current[i - 1] - 2 * current[i] + current[i + 1]);
            (current, next) = (next, current);
        }
        return current;
    }

    /// <summary>
    /// Starts N jobs on the thread pool and blocks until every one has signalled. An exception escaping a thread-pool
    /// work item terminates the process, so each job catches its own; failures are rethrown together at the end.
    /// </summary>
    public static int RunAndWait(int jobs, Action<int> work)
    {
        var completed = 0;
        var failures = new System.Collections.Concurrent.ConcurrentQueue<Exception>();
        using var countdown = new CountdownEvent(jobs);
        for (var i = 0; i < jobs; i++)
        {
            var job = i;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    work(job);
                    Interlocked.Increment(ref completed);
                }
                catch (Exception e)
                {
                    failures.Enqueue(e);
                }
                finally
                {
                    countdown.Signal(); // signal even on failure, or Wait would hang
                }
            });
        }
        countdown.Wait();
        if (!failures.IsEmpty) throw new AggregateException($"{failures.Count} of {jobs} jobs failed.", failures);
        return completed;
    }
}
