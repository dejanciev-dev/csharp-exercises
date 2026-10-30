namespace CSharpExercises.Bits;

/// <summary>
/// Held–Karp: dynamic programming over subsets, with the set of visited cities stored as a bit mask. O(2^n · n²)
/// instead of the O(n!) of trying every order, which turns 15 cities from 87 billion tours into 7 million steps.
/// </summary>
public static class TravellingSalesman
{
    public static (int Cost, int[] Tour) Solve(int[,] distances)
    {
        var n = distances.GetLength(0);
        if (n != distances.GetLength(1)) throw new ArgumentException("The distance matrix must be square.", nameof(distances));
        if (n is < 1 or > 20) throw new ArgumentOutOfRangeException(nameof(distances), "Supports 1 to 20 cities.");
        if (n == 1) return (0, [0, 0]);

        const int Unreachable = int.MaxValue / 2;
        var full = 1 << n;

        // best[mask, last]: cheapest path starting at city 0, visiting exactly the cities in mask, ending at last.
        var best = new int[full, n];
        var parent = new int[full, n];
        for (var m = 0; m < full; m++)
            for (var c = 0; c < n; c++) best[m, c] = Unreachable;
        best[1, 0] = 0;

        for (var mask = 1; mask < full; mask += 2) // only masks that contain the start city
        {
            for (var last = 0; last < n; last++)
            {
                if ((mask & (1 << last)) == 0 || best[mask, last] == Unreachable) continue;

                for (var next = 0; next < n; next++)
                {
                    if ((mask & (1 << next)) != 0) continue;
                    var nextMask = mask | (1 << next);
                    var cost = best[mask, last] + distances[last, next];
                    if (cost < best[nextMask, next])
                    {
                        best[nextMask, next] = cost;
                        parent[nextMask, next] = last;
                    }
                }
            }
        }

        var all = full - 1;
        var (bestCost, end) = (Unreachable, -1);
        for (var last = 1; last < n; last++)
        {
            var cost = best[all, last] + distances[last, 0];
            if (cost < bestCost) (bestCost, end) = (cost, last);
        }

        var tour = new List<int> { 0 };
        for (int mask = all, city = end; city != 0;)
        {
            tour.Add(city);
            var previous = parent[mask, city];
            mask &= ~(1 << city);
            city = previous;
        }
        tour.Add(0);
        tour.Reverse();
        return (bestCost, [.. tour]);
    }
}
