namespace CSharpExercises.Algorithms;

public readonly record struct KnapsackItem(string Name, int Weight, int Value);

/// <summary>
/// 0/1 knapsack: choose items to maximise value within a weight limit, each item taken at most once. Greedy by
/// value per kilo fails here; dynamic programming over capacities solves it in O(n · capacity).
/// </summary>
public static class Knapsack
{
    public static (int Value, IReadOnlyList<KnapsackItem> Chosen) Solve(IReadOnlyList<KnapsackItem> items, int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        // best[i, w]: the best value using the first i items with capacity w.
        var best = new int[items.Count + 1, capacity + 1];
        for (var i = 1; i <= items.Count; i++)
        {
            var item = items[i - 1];
            for (var w = 0; w <= capacity; w++)
            {
                best[i, w] = best[i - 1, w];
                if (item.Weight <= w) best[i, w] = Math.Max(best[i, w], best[i - 1, w - item.Weight] + item.Value);
            }
        }

        // Walk back: if the value changed when item i was allowed, item i was taken.
        var chosen = new List<KnapsackItem>();
        for (int i = items.Count, w = capacity; i > 0; i--)
        {
            if (best[i, w] == best[i - 1, w]) continue;
            chosen.Add(items[i - 1]);
            w -= items[i - 1].Weight;
        }
        chosen.Reverse();
        return (best[items.Count, capacity], chosen);
    }

    /// <summary>
    /// Only the value, in O(capacity) memory with one row. Iterating capacities downwards means each item is still
    /// read from the "previous row", so it can't be used twice.
    /// </summary>
    public static int MaxValue(IEnumerable<KnapsackItem> items, int capacity)
    {
        var best = new int[capacity + 1];
        foreach (var item in items)
            for (var w = capacity; w >= item.Weight; w--)
                best[w] = Math.Max(best[w], best[w - item.Weight] + item.Value);
        return best[capacity];
    }

    /// <summary>The greedy answer, to show what goes wrong: it takes the best ratio first and can't undo it.</summary>
    public static int GreedyByRatio(IEnumerable<KnapsackItem> items, int capacity)
    {
        var value = 0;
        foreach (var item in items.OrderByDescending(i => (double)i.Value / i.Weight))
        {
            if (item.Weight > capacity) continue;
            capacity -= item.Weight;
            value += item.Value;
        }
        return value;
    }

    /// <summary>Can some subset add up to exactly <paramref name="target"/>? The same DP with booleans.</summary>
    public static bool SubsetSum(IEnumerable<int> numbers, int target)
    {
        var reachable = new bool[target + 1];
        reachable[0] = true;
        foreach (var n in numbers)
            for (var t = target; t >= n; t--)
                reachable[t] |= reachable[t - n];
        return reachable[target];
    }
}
