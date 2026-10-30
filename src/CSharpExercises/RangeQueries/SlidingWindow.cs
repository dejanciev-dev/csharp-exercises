namespace CSharpExercises.RangeQueries;

/// <summary>
/// Sliding-window techniques. The monotonic deque gives the maximum of every window in O(n) total: it keeps indices
/// of values in decreasing order, because a value with a bigger one to its right can never be a maximum again.
/// </summary>
public static class SlidingWindow
{
    public static int[] Maximums(IReadOnlyList<int> values, int window)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(window, 1);
        if (values.Count < window) return [];

        var result = new int[values.Count - window + 1];
        var deque = new LinkedList<int>(); // indices; front is the current maximum

        for (var i = 0; i < values.Count; i++)
        {
            if (deque.Count > 0 && deque.First!.Value <= i - window) deque.RemoveFirst();
            while (deque.Count > 0 && values[deque.Last!.Value] <= values[i]) deque.RemoveLast();
            deque.AddLast(i);

            if (i >= window - 1) result[i - window + 1] = values[deque.First!.Value];
        }
        return result;
    }

    /// <summary>
    /// Longest substring without repeated characters: grow the right edge, and when a character repeats, jump the
    /// left edge just past its previous occurrence.
    /// </summary>
    public static string LongestWithoutRepeats(string text)
    {
        var lastSeen = new Dictionary<char, int>();
        int start = 0, bestStart = 0, bestLength = 0;

        for (var i = 0; i < text.Length; i++)
        {
            if (lastSeen.TryGetValue(text[i], out var previous) && previous >= start) start = previous + 1;
            lastSeen[text[i]] = i;
            if (i - start + 1 > bestLength) (bestStart, bestLength) = (start, i - start + 1);
        }
        return text.Substring(bestStart, bestLength);
    }

    /// <summary>Shortest run of non-negative numbers adding up to at least <paramref name="target"/>; 0 if none.</summary>
    public static int ShortestWithSumAtLeast(IReadOnlyList<int> values, long target)
    {
        long sum = 0;
        int start = 0, best = int.MaxValue;
        for (var end = 0; end < values.Count; end++)
        {
            if (values[end] < 0) throw new ArgumentException("Values must be non-negative.", nameof(values));
            sum += values[end];
            while (sum >= target)
            {
                best = Math.Min(best, end - start + 1);
                sum -= values[start++];
            }
        }
        return best == int.MaxValue ? 0 : best;
    }
}
