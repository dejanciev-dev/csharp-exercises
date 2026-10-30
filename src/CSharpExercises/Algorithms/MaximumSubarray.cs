namespace CSharpExercises.Algorithms;

/// <summary>
/// Kadane's algorithm: the best subarray ending at i either extends the best one ending at i - 1 or starts fresh at
/// i, whichever is larger. One pass, O(1) memory. Think "best stretch of days to have held a stock".
/// </summary>
public static class MaximumSubarray
{
    public static (long Sum, int Start, int Length) Find(IReadOnlyList<int> values)
    {
        if (values.Count == 0) throw new ArgumentException("Need at least one value.", nameof(values));

        long best = values[0], current = values[0];
        int bestStart = 0, bestEnd = 0, start = 0;

        for (var i = 1; i < values.Count; i++)
        {
            if (current < 0) (current, start) = (values[i], i);
            else current += values[i];

            if (current > best) (best, bestStart, bestEnd) = (current, start, i);
        }
        return (best, bestStart, bestEnd - bestStart + 1);
    }

    /// <summary>
    /// On a circular array the best run may wrap around the end: then it's the total minus the worst run in the
    /// middle. If every value is negative, the wrap-around answer would be empty, so take the plain one.
    /// </summary>
    public static long CircularMax(IReadOnlyList<int> values)
    {
        var plain = Find(values).Sum;
        if (plain < 0) return plain;

        var negated = values.Select(v => -v).ToArray();
        var worst = -Find(negated).Sum;
        return Math.Max(plain, values.Sum(v => (long)v) - worst);
    }

    /// <summary>Best profit from one buy followed by one sell: Kadane on the day-to-day price changes.</summary>
    public static decimal BestTrade(IReadOnlyList<decimal> prices)
    {
        decimal lowest = decimal.MaxValue, best = 0;
        foreach (var price in prices)
        {
            lowest = Math.Min(lowest, price);
            best = Math.Max(best, price - lowest);
        }
        return best;
    }
}
