namespace CSharpExercises.Simulations;

/// <summary>
/// The Lotka–Volterra predator–prey equations: rabbits grow on their own and are eaten in proportion to encounters with
/// foxes; foxes grow from eating and die off without food. The populations cycle, with predators peaking a quarter cycle
/// after prey, as in the famous Hudson's Bay lynx and hare records. Integrated with Runge–Kutta 4, because simple Euler
/// steps make the orbits spiral outward. A conserved quantity checks the integration.
/// </summary>
public static class PredatorPrey
{
    public sealed record Parameters(double PreyGrowth, double PredationRate, double PredatorGrowth, double PredatorDeath);

    public static IReadOnlyList<(double Time, double Prey, double Predators)> Run(Parameters p, double prey, double predators, double time, double dt = 0.01)
    {
        var path = new List<(double, double, double)> { (0, prey, predators) };
        (double, double) Rates(double x, double y) =>
            (p.PreyGrowth * x - p.PredationRate * x * y, p.PredatorGrowth * x * y - p.PredatorDeath * y);

        for (var t = dt; t <= time + 1e-9; t += dt)
        {
            var (k1x, k1y) = Rates(prey, predators);
            var (k2x, k2y) = Rates(prey + dt / 2 * k1x, predators + dt / 2 * k1y);
            var (k3x, k3y) = Rates(prey + dt / 2 * k2x, predators + dt / 2 * k2y);
            var (k4x, k4y) = Rates(prey + dt * k3x, predators + dt * k3y);
            prey += dt / 6 * (k1x + 2 * k2x + 2 * k3x + k4x);
            predators += dt / 6 * (k1y + 2 * k2y + 2 * k3y + k4y);
            path.Add((t, prey, predators));
        }
        return path;
    }

    /// <summary>V = δx - γ ln x + βy - α ln y stays constant along every orbit.</summary>
    public static double Invariant(Parameters p, double prey, double predators) =>
        p.PredatorGrowth * prey - p.PredatorDeath * Math.Log(prey) + p.PredationRate * predators - p.PreyGrowth * Math.Log(predators);

    /// <summary>The point where both populations stay constant forever.</summary>
    public static (double Prey, double Predators) Equilibrium(Parameters p) => (p.PredatorDeath / p.PredatorGrowth, p.PreyGrowth / p.PredationRate);

    /// <summary>Times at which a series reaches a local maximum, to measure the cycle length and lag.</summary>
    public static IReadOnlyList<double> Peaks(IReadOnlyList<(double Time, double Value)> series) =>
        Enumerable.Range(1, series.Count - 2)
            .Where(i => series[i].Value > series[i - 1].Value && series[i].Value >= series[i + 1].Value)
            .Select(i => series[i].Time)
            .ToList();
}
