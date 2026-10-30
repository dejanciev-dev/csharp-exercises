namespace CSharpExercises.Networking;

/// <summary>
/// Retry delays. Plain exponential backoff makes every client that failed together retry together, hammering a
/// recovering server in waves; jitter spreads them out. These are the strategies compared in the AWS Architecture
/// Blog's "Exponential Backoff and Jitter": none, full jitter, equal jitter and decorrelated jitter.
/// </summary>
public static class Backoff
{
    public static IEnumerable<TimeSpan> Exponential(TimeSpan baseDelay, TimeSpan cap, int attempts)
    {
        for (var i = 0; i < attempts; i++) yield return Min(cap, baseDelay * Math.Pow(2, i));
    }

    /// <summary>A random delay between 0 and the exponential value: the most spread, the least total work.</summary>
    public static IEnumerable<TimeSpan> FullJitter(TimeSpan baseDelay, TimeSpan cap, int attempts, Random random) =>
        Exponential(baseDelay, cap, attempts).Select(d => d * random.NextDouble());

    /// <summary>Half fixed, half random: never retries immediately.</summary>
    public static IEnumerable<TimeSpan> EqualJitter(TimeSpan baseDelay, TimeSpan cap, int attempts, Random random) =>
        Exponential(baseDelay, cap, attempts).Select(d => d / 2 + d / 2 * random.NextDouble());

    /// <summary>Each delay is random between the base and three times the previous delay.</summary>
    public static IEnumerable<TimeSpan> DecorrelatedJitter(TimeSpan baseDelay, TimeSpan cap, int attempts, Random random)
    {
        var previous = baseDelay;
        for (var i = 0; i < attempts; i++)
        {
            var upper = previous * 3;
            previous = Min(cap, baseDelay + (upper - baseDelay) * random.NextDouble());
            yield return previous;
        }
    }

    /// <summary>
    /// Simulates many clients failing at the same moment and retrying with the given strategy, and returns the busiest
    /// 10 ms bucket: what the server sees as a spike.
    /// </summary>
    public static int PeakLoad(int clients, Func<Random, IEnumerable<TimeSpan>> strategy, int seed = 1)
    {
        var random = new Random(seed);
        var buckets = new Dictionary<long, int>();
        for (var c = 0; c < clients; c++)
        {
            var time = TimeSpan.Zero;
            foreach (var delay in strategy(random))
            {
                time += delay;
                var bucket = (long)(time.TotalMilliseconds / 10);
                buckets[bucket] = buckets.GetValueOrDefault(bucket) + 1;
            }
        }
        return buckets.Values.Max();
    }

    private static TimeSpan Min(TimeSpan a, TimeSpan b) => a < b ? a : b;
}
