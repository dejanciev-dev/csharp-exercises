namespace CSharpExercises.Simulations;

/// <summary>
/// Gambler's ruin, a random walk with absorbing barriers: bet one unit per round with win probability p until you're broke
/// or reach a target. The exact chance of reaching the target has a closed form, and even a tiny house edge makes long
/// sessions nearly hopeless. The expected number of rounds is surprisingly large for a fair game: start × (target - start).
/// </summary>
public static class GamblersRuin
{
    public static double ProbabilityOfReaching(int start, int target, double p)
    {
        if (start <= 0) return 0;
        if (start >= target) return 1;
        if (Math.Abs(p - 0.5) < 1e-12) return (double)start / target;
        var ratio = (1 - p) / p;
        return (1 - Math.Pow(ratio, start)) / (1 - Math.Pow(ratio, target));
    }

    public static double ExpectedRoundsFair(int start, int target) => (double)start * (target - start);

    public static (double SuccessRate, double AverageRounds) Simulate(int start, int target, double p, int trials, Random random)
    {
        int successes = 0;
        long rounds = 0;
        for (var t = 0; t < trials; t++)
        {
            var money = start;
            while (money > 0 && money < target)
            {
                money += random.NextDouble() < p ? 1 : -1;
                rounds++;
            }
            if (money == target) successes++;
        }
        return ((double)successes / trials, (double)rounds / trials);
    }
}
