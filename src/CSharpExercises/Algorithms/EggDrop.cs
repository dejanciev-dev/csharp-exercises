namespace CSharpExercises.Algorithms;

/// <summary>
/// The egg drop puzzle: with e eggs and a building of f floors, how many drops guarantee finding the highest safe floor?
/// With one egg you must go floor by floor; with unlimited eggs, binary search. In between, flip the question: with
/// d drops and e eggs, how many floors can be covered? cover(d, e) = cover(d-1, e-1) + cover(d-1, e) + 1 (the egg
/// breaks or it doesn't), and the answer is the smallest d whose cover reaches f. The same reasoning sizes test
/// budgets when each failure is expensive.
/// </summary>
public static class EggDrop
{
    public static int MinimumDrops(int eggs, int floors)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(eggs, 1);
        var cover = new long[eggs + 1]; // cover[e] for the current number of drops
        var drops = 0;
        while (cover[eggs] < floors)
        {
            drops++;
            for (var e = eggs; e >= 1; e--) cover[e] = cover[e - 1] + cover[e] + 1;
        }
        return drops;
    }

    /// <summary>The floors to try, in order, with two eggs: steps shrinking by one so the worst case is equal everywhere.</summary>
    public static IReadOnlyList<int> TwoEggPlan(int floors)
    {
        var drops = MinimumDrops(2, floors);
        var plan = new List<int>();
        for (int step = drops, floor = 0; floor + step <= floors && step > 0; step--) plan.Add(floor += step);
        return plan;
    }

    /// <summary>The direct O(e·f²) DP over "drop from floor k", to verify the faster one.</summary>
    public static int MinimumDropsSlow(int eggs, int floors)
    {
        var best = new int[eggs + 1, floors + 1];
        for (var f = 1; f <= floors; f++) best[1, f] = f;
        for (var e = 2; e <= eggs; e++)
            for (var f = 1; f <= floors; f++)
            {
                best[e, f] = int.MaxValue;
                for (var k = 1; k <= f; k++)
                    best[e, f] = Math.Min(best[e, f], 1 + Math.Max(best[e - 1, k - 1], best[e, f - k]));
            }
        return best[eggs, floors];
    }
}
