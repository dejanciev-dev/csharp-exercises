namespace CSharpExercises.Planning;

/// <summary>
/// Bin packing (VMs onto hosts, files onto disks, parcels into trucks): use as few fixed-size bins as possible. It's
/// NP-hard, but simple heuristics come close. Next-fit only looks at the current bin; first-fit tries every open bin;
/// first-fit decreasing sorts the biggest items first and is proven to use at most 11/9 of the optimum (plus a constant).
/// The lower bound ⌈total / capacity⌉ shows how good a packing is.
/// </summary>
public static class BinPacking
{
    public static IReadOnlyList<IReadOnlyList<int>> NextFit(IEnumerable<int> items, int capacity)
    {
        var bins = new List<List<int>>();
        foreach (var item in Validate(items, capacity))
        {
            if (bins.Count == 0 || bins[^1].Sum() + item > capacity) bins.Add([]);
            bins[^1].Add(item);
        }
        return bins;
    }

    public static IReadOnlyList<IReadOnlyList<int>> FirstFit(IEnumerable<int> items, int capacity)
    {
        var bins = new List<List<int>>();
        var free = new List<int>();
        foreach (var item in Validate(items, capacity))
        {
            var index = free.FindIndex(f => f >= item);
            if (index < 0)
            {
                bins.Add([]);
                free.Add(capacity);
                index = bins.Count - 1;
            }
            bins[index].Add(item);
            free[index] -= item;
        }
        return bins;
    }

    public static IReadOnlyList<IReadOnlyList<int>> FirstFitDecreasing(IEnumerable<int> items, int capacity) =>
        FirstFit(items.OrderDescending(), capacity);

    /// <summary>No packing can use fewer bins than this.</summary>
    public static int LowerBound(IEnumerable<int> items, int capacity) => (int)Math.Ceiling((double)items.Sum() / capacity);

    /// <summary>The true optimum by branch and bound, for small inputs: try each item in each open bin or a new one.</summary>
    public static int Optimal(IReadOnlyList<int> items, int capacity)
    {
        var sorted = Validate(items, capacity).OrderDescending().ToArray();
        var best = FirstFitDecreasing(sorted, capacity).Count; // a good starting upper bound prunes most branches
        var loads = new List<int>();

        void Place(int i)
        {
            if (loads.Count >= best) return;
            if (i == sorted.Length)
            {
                best = loads.Count;
                return;
            }
            var tried = new HashSet<int>();
            for (var b = 0; b < loads.Count; b++)
            {
                if (loads[b] + sorted[i] > capacity || !tried.Add(loads[b])) continue; // equal loads are interchangeable
                loads[b] += sorted[i];
                Place(i + 1);
                loads[b] -= sorted[i];
            }
            loads.Add(sorted[i]);
            Place(i + 1);
            loads.RemoveAt(loads.Count - 1);
        }

        Place(0);
        return best;
    }

    private static IEnumerable<int> Validate(IEnumerable<int> items, int capacity)
    {
        foreach (var item in items)
        {
            if (item <= 0 || item > capacity) throw new ArgumentException($"Item {item} doesn't fit a bin of {capacity}.");
            yield return item;
        }
    }
}
