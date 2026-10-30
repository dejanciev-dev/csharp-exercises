namespace CSharpExercises.Simulations;

public readonly record struct SirState(double Day, double Susceptible, double Infected, double Recovered);

/// <summary>
/// The SIR epidemic model: people move from susceptible to infected at a rate proportional to contacts between the two
/// groups (β·S·I/N), and from infected to recovered at rate γ. R₀ = β/γ decides everything: below 1 the outbreak fizzles,
/// above 1 it grows until enough people are immune; the peak arrives when S/N falls to 1/R₀, and vaccinating a
/// fraction above 1 - 1/R₀ prevents the outbreak (herd immunity).
/// </summary>
public static class SirModel
{
    public static IReadOnlyList<SirState> Run(double population, double initialInfected, double beta, double gamma, double days, double dt = 0.1, double vaccinated = 0)
    {
        var s = population * (1 - vaccinated) - initialInfected;
        var i = initialInfected;
        var r = population * vaccinated;
        var states = new List<SirState> { new(0, s, i, r) };

        for (var t = dt; t <= days + 1e-9; t += dt)
        {
            // Small Euler steps are accurate enough for a smooth model like this one.
            var newInfections = beta * s * i / population * dt;
            var recoveries = gamma * i * dt;
            s -= newInfections;
            i += newInfections - recoveries;
            r += recoveries;
            states.Add(new SirState(Math.Round(t, 6), s, i, r));
        }
        return states;
    }

    public static double HerdImmunityThreshold(double r0) => r0 <= 1 ? 0 : 1 - 1 / r0;

    public static SirState Peak(IEnumerable<SirState> states) => states.MaxBy(s => s.Infected);
}
