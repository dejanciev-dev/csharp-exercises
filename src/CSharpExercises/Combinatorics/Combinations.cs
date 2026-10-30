namespace CSharpExercises.Combinatorics;

/// <summary>
/// Generating k-combinations lazily in lexicographic order with an index array, instead of recursion: each step bumps
/// the rightmost index that can still move and resets the ones after it. Also the cartesian product of several lists
/// (an odometer), which is what nested loops do when the number of loops isn't known in advance.
/// </summary>
public static class Combinations
{
    public static IEnumerable<T[]> Choose<T>(IReadOnlyList<T> items, int k)
    {
        if (k < 0 || k > items.Count) yield break;
        var indices = Enumerable.Range(0, k).ToArray();

        while (true)
        {
            yield return indices.Select(i => items[i]).ToArray();

            var position = k - 1;
            while (position >= 0 && indices[position] == items.Count - k + position) position--;
            if (position < 0) yield break;

            indices[position]++;
            for (var j = position + 1; j < k; j++) indices[j] = indices[j - 1] + 1;
        }
    }

    public static IEnumerable<T[]> CartesianProduct<T>(IReadOnlyList<IReadOnlyList<T>> lists)
    {
        if (lists.Count == 0 || lists.Any(l => l.Count == 0)) yield break;
        var digits = new int[lists.Count];

        while (true)
        {
            yield return digits.Select((d, i) => lists[i][d]).ToArray();

            // Increment like an odometer: the last wheel turns fastest and carries into the one before.
            var wheel = lists.Count - 1;
            while (wheel >= 0 && ++digits[wheel] == lists[wheel].Count) digits[wheel--] = 0;
            if (wheel < 0) yield break;
        }
    }

    /// <summary>Every way to make change for an amount with unlimited coins, listed (not just counted).</summary>
    public static IEnumerable<int[]> WaysToMakeChange(int amount, IReadOnlyList<int> coins)
    {
        var sorted = coins.OrderDescending().ToArray();

        IEnumerable<int[]> From(int remaining, int coinIndex)
        {
            if (remaining == 0) { yield return []; yield break; }
            for (var i = coinIndex; i < sorted.Length; i++)
            {
                if (sorted[i] > remaining) continue;
                foreach (var rest in From(remaining - sorted[i], i)) // i, not i + 1: coins can repeat
                    yield return [sorted[i], .. rest];
            }
        }

        return From(amount, 0);
    }
}
