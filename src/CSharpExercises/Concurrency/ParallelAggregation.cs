namespace CSharpExercises.Concurrency;

/// <summary>
/// Summing or counting in parallel without contention. Updating one shared total from every iteration serialises
/// the threads on that variable (or loses updates without a lock). The fix is thread-local partial results combined
/// once per thread at the end, which is what <see cref="Parallel.For(long, long, Func{long}, Func{long, ParallelLoopState, long, long}, Action{long})"/>'s
/// localInit/localFinally overloads and PLINQ's Aggregate do.
/// </summary>
public static class ParallelAggregation
{
    public static long SumSequential(int[] values)
    {
        long total = 0;
        foreach (var v in values) total += v;
        return total;
    }

    /// <summary>Correct but slow: every iteration takes the same lock.</summary>
    public static long SumWithSharedLock(int[] values)
    {
        long total = 0;
        var gate = new Lock();
        Parallel.For(0, values.Length, i =>
        {
            lock (gate) total += values[i];
        });
        return total;
    }

    /// <summary>Each worker sums into its own local; only the final per-worker totals are combined atomically.</summary>
    public static long SumWithThreadLocals(int[] values)
    {
        long total = 0;
        Parallel.For(0, values.Length,
            localInit: () => 0L,
            body: (i, _, local) => local + values[i],
            localFinally: local => Interlocked.Add(ref total, local));
        return total;
    }

    public static long SumWithPlinq(int[] values) => values.AsParallel().Sum(v => (long)v);

    /// <summary>A histogram: per-thread arrays merged at the end, instead of contended increments on shared buckets.</summary>
    public static int[] Histogram(int[] values, int buckets, int maxValue)
    {
        var result = new int[buckets];
        Parallel.For(0, values.Length,
            () => new int[buckets],
            (i, _, local) =>
            {
                local[Math.Min(buckets - 1, (int)((long)values[i] * buckets / (maxValue + 1L)))]++;
                return local;
            },
            local =>
            {
                lock (result)
                    for (var b = 0; b < buckets; b++) result[b] += local[b];
            });
        return result;
    }

    /// <summary>Word counts with PLINQ: group and count in parallel, results merged by the framework.</summary>
    public static Dictionary<string, int> WordCounts(IEnumerable<string> lines) =>
        lines.AsParallel()
            .SelectMany(l => l.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .GroupBy(w => w.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.Count());
}
