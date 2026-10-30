namespace CSharpExercises.DataProcessing;

public enum GapFill { Leave, Zero, ForwardFill, Interpolate }

/// <summary>
/// Resampling irregular measurements onto a regular grid, as every metrics dashboard does: bucket by aligned intervals
/// (floor the timestamp), aggregate each bucket, then decide what an empty bucket means. Zero suits counts (no
/// requests), forward fill suits states (the last known temperature), interpolation suits smooth quantities.
/// </summary>
public static class TimeSeries
{
    public static DateTimeOffset Floor(DateTimeOffset time, TimeSpan interval) =>
        new(time.UtcTicks - time.UtcTicks % interval.Ticks, TimeSpan.Zero);

    public static IReadOnlyList<(DateTimeOffset Bucket, double? Value)> Resample(
        IEnumerable<(DateTimeOffset Time, double Value)> points, TimeSpan interval,
        Func<IEnumerable<double>, double> aggregate, GapFill fill = GapFill.Leave)
    {
        var buckets = points
            .GroupBy(p => Floor(p.Time, interval), p => p.Value)
            .ToDictionary(g => g.Key, g => aggregate(g));
        if (buckets.Count == 0) return [];

        var (first, last) = (buckets.Keys.Min(), buckets.Keys.Max());
        var result = new List<(DateTimeOffset, double?)>();
        for (var t = first; t <= last; t += interval) result.Add((t, buckets.TryGetValue(t, out var v) ? v : null));

        return fill switch
        {
            GapFill.Zero => result.Select(r => (r.Item1, (double?)(r.Item2 ?? 0))).ToList(),
            GapFill.ForwardFill => ForwardFill(result),
            GapFill.Interpolate => Interpolate(result),
            _ => result,
        };
    }

    /// <summary>Change from the previous bucket, e.g. turning a cumulative counter into per-interval rates.</summary>
    public static IEnumerable<(DateTimeOffset Bucket, double Delta)> Differences(IReadOnlyList<(DateTimeOffset Bucket, double? Value)> series)
    {
        for (var i = 1; i < series.Count; i++)
            if (series[i].Value is { } now && series[i - 1].Value is { } before)
                yield return (series[i].Bucket, now < before ? now : now - before); // a drop means the counter reset
    }

    private static List<(DateTimeOffset, double?)> ForwardFill(List<(DateTimeOffset Bucket, double? Value)> series)
    {
        double? last = null;
        return series.Select(s => (s.Bucket, last = s.Value ?? last)).ToList();
    }

    private static List<(DateTimeOffset, double?)> Interpolate(List<(DateTimeOffset Bucket, double? Value)> series)
    {
        var result = series.ToList();
        for (var i = 0; i < result.Count; i++)
        {
            if (result[i].Value is not null) continue;
            var previous = i - 1; // always valid: the first bucket has a value
            var next = result.FindIndex(i, r => r.Value is not null);
            var fraction = (double)(i - previous) / (next - previous);
            result[i] = (result[i].Bucket, result[previous].Value + (result[next].Value - result[previous].Value) * fraction);
        }
        return result;
    }
}
