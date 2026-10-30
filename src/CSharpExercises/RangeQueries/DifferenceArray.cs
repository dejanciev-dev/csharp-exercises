namespace CSharpExercises.RangeQueries;

/// <summary>
/// The inverse of prefix sums: to add a value to a whole range, mark +v where it starts and -v where it ends, then
/// rebuild with one running total. Many range updates followed by one read cost O(updates + n).
/// </summary>
public sealed class DifferenceArray
{
    private readonly long[] _diff;

    public DifferenceArray(int length) => _diff = new long[length + 1];

    /// <summary>Adds <paramref name="value"/> to every element in [from, to).</summary>
    public void AddRange(int from, int to, long value)
    {
        if (from < 0 || to > _diff.Length - 1 || from > to) throw new ArgumentOutOfRangeException(nameof(from));
        _diff[from] += value;
        _diff[to] -= value;
    }

    public long[] Build()
    {
        var result = new long[_diff.Length - 1];
        long running = 0;
        for (var i = 0; i < result.Length; i++) result[i] = running += _diff[i];
        return result;
    }

    /// <summary>
    /// The most rooms needed at once, given half-open booking intervals: +1 at check-in, -1 at check-out, and the
    /// running total peaks at the busiest moment. Sorting the events avoids allocating one slot per time unit.
    /// </summary>
    public static int PeakOverlap(IEnumerable<(int Start, int End)> intervals)
    {
        var events = intervals
            .SelectMany(i => new[] { (Time: i.Start, Delta: +1), (Time: i.End, Delta: -1) })
            .OrderBy(e => e.Time)
            .ThenBy(e => e.Delta); // at the same moment, leave before arriving

        int current = 0, peak = 0;
        foreach (var (_, delta) in events) peak = Math.Max(peak, current += delta);
        return peak;
    }
}
