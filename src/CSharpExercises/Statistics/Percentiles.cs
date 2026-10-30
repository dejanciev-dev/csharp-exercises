namespace CSharpExercises.Statistics;

/// <summary>
/// Percentiles for latency reports ("p99 is 340 ms"). There is no single definition: nearest-rank always returns an
/// actual observation, linear interpolation (Excel's PERCENTILE.INC, NumPy's default) blends neighbours. For a
/// single percentile there's no need to sort everything: quickselect finds the k-th smallest in O(n) on average.
/// </summary>
public static class Percentiles
{
    public static double NearestRank(IReadOnlyList<double> values, double percentile)
    {
        var sorted = Sorted(values, percentile);
        var rank = (int)Math.Ceiling(percentile / 100 * sorted.Length);
        return sorted[Math.Max(rank, 1) - 1];
    }

    public static double Interpolated(IReadOnlyList<double> values, double percentile)
    {
        var sorted = Sorted(values, percentile);
        var position = percentile / 100 * (sorted.Length - 1);
        var lower = (int)Math.Floor(position);
        var upper = Math.Min(lower + 1, sorted.Length - 1);
        return sorted[lower] + (position - lower) * (sorted[upper] - sorted[lower]);
    }

    /// <summary>The k-th smallest value (0-based) by Hoare-style partitioning around a random pivot.</summary>
    public static double QuickSelect(IReadOnlyList<double> values, int k, Random? random = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(k);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(k, values.Count);
        random ??= Random.Shared;

        var a = values.ToArray();
        int left = 0, right = a.Length - 1;
        while (left < right)
        {
            var pivot = a[random.Next(left, right + 1)];
            int i = left, j = right;
            while (i <= j)
            {
                while (a[i] < pivot) i++;
                while (a[j] > pivot) j--;
                if (i <= j) (a[i], a[j], i, j) = (a[j], a[i], i + 1, j - 1);
            }
            // Now a[left..j] ≤ pivot ≤ a[i..right]; continue in the side that holds position k.
            if (k <= j) right = j;
            else if (k >= i) left = i;
            else return a[k];
        }
        return a[k];
    }

    /// <summary>The usual dashboard summary.</summary>
    public static (double P50, double P90, double P99) Summary(IReadOnlyList<double> latencies) =>
        (NearestRank(latencies, 50), NearestRank(latencies, 90), NearestRank(latencies, 99));

    private static double[] Sorted(IReadOnlyList<double> values, double percentile)
    {
        if (values.Count == 0) throw new ArgumentException("Need at least one value.", nameof(values));
        ArgumentOutOfRangeException.ThrowIfNegative(percentile);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(percentile, 100);
        var sorted = values.ToArray();
        Array.Sort(sorted);
        return sorted;
    }
}
