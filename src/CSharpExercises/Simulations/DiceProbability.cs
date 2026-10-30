namespace CSharpExercises.Simulations;

/// <summary>
/// Exact dice probabilities instead of simulation: the distribution of a sum of dice is the convolution of the single
/// distributions, built up one die at a time. That answers "chance of rolling at least 10 with 3d6" exactly, handles
/// modifiers like "roll 4d6 and drop the lowest" by enumerating outcomes, and gives expected values for game design.
/// </summary>
public static class DiceProbability
{
    /// <summary>Probability of each total for <paramref name="count"/> dice with <paramref name="sides"/> sides.</summary>
    public static IReadOnlyDictionary<int, double> Sum(int count, int sides)
    {
        var distribution = new Dictionary<int, double> { [0] = 1 };
        for (var die = 0; die < count; die++)
        {
            var next = new Dictionary<int, double>();
            foreach (var (total, p) in distribution)
                for (var face = 1; face <= sides; face++)
                    next[total + face] = next.GetValueOrDefault(total + face) + p / sides;
            distribution = next;
        }
        return distribution;
    }

    public static double AtLeast(int count, int sides, int target) => Sum(count, sides).Where(p => p.Key >= target).Sum(p => p.Value);

    public static double Expected(IReadOnlyDictionary<int, double> distribution) => distribution.Sum(p => p.Key * p.Value);

    /// <summary>Roll several dice, keep the highest few (4d6 drop lowest, advantage = 2d20 keep highest).</summary>
    public static IReadOnlyDictionary<int, double> KeepHighest(int count, int sides, int keep)
    {
        var result = new Dictionary<int, double>();
        var total = Math.Pow(sides, count);
        var faces = new int[count];
        for (long outcome = 0; outcome < (long)total; outcome++)
        {
            var o = outcome;
            for (var i = 0; i < count; i++) { faces[i] = (int)(o % sides) + 1; o /= sides; }
            var sum = faces.OrderDescending().Take(keep).Sum();
            result[sum] = result.GetValueOrDefault(sum) + 1 / total;
        }
        return result;
    }

    /// <summary>A Monte Carlo estimate, to check the exact answer.</summary>
    public static double SimulateAtLeast(int count, int sides, int target, int trials, Random random) =>
        (double)Enumerable.Range(0, trials).Count(_ => Enumerable.Range(0, count).Sum(_ => random.Next(1, sides + 1)) >= target) / trials;
}
