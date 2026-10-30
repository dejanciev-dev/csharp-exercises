namespace CSharpExercises.Randomness;

/// <summary>
/// Monte Carlo: answer a question by simulating it many times. The error shrinks like 1/√n, so each extra digit of
/// precision costs 100× more samples, but it works when the exact maths is hard or unknown.
/// </summary>
public static class MonteCarlo
{
    /// <summary>The fraction of random points in the unit square that fall inside the quarter circle is π/4.</summary>
    public static double EstimatePi(int samples, Random random)
    {
        var inside = 0;
        for (var i = 0; i < samples; i++)
        {
            double x = random.NextDouble(), y = random.NextDouble();
            if (x * x + y * y <= 1) inside++;
        }
        return 4.0 * inside / samples;
    }

    /// <summary>Simulated chance that at least two of <paramref name="people"/> share a birthday.</summary>
    public static double SharedBirthdayChance(int people, int trials, Random random)
    {
        var hits = 0;
        var seen = new bool[365];
        for (var t = 0; t < trials; t++)
        {
            Array.Clear(seen);
            for (var p = 0; p < people; p++)
            {
                var day = random.Next(365);
                if (seen[day]) { hits++; break; }
                seen[day] = true;
            }
        }
        return (double)hits / trials;
    }

    /// <summary>The exact answer: 1 - (365/365 · 364/365 · …), for comparison.</summary>
    public static double SharedBirthdayExact(int people)
    {
        var allDifferent = 1.0;
        for (var i = 0; i < people; i++) allDifferent *= (365.0 - i) / 365;
        return 1 - allDifferent;
    }

    /// <summary>
    /// The Monty Hall problem: after the host opens a goat door, switching wins when the first pick was wrong,
    /// which happens 2/3 of the time.
    /// </summary>
    public static double MontyHallWinRate(bool switchDoors, int trials, Random random)
    {
        var wins = 0;
        for (var t = 0; t < trials; t++)
        {
            var car = random.Next(3);
            var pick = random.Next(3);
            var opened = Enumerable.Range(0, 3).First(d => d != car && d != pick);
            if (switchDoors) pick = Enumerable.Range(0, 3).First(d => d != pick && d != opened);
            if (pick == car) wins++;
        }
        return (double)wins / trials;
    }
}
