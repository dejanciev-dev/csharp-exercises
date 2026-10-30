using CSharpExercises.Simulations;

namespace CSharpExercises.Tests;

public class MoreSimulationsTests
{
    [Fact]
    public void Boids_FlockAlignsOverTime()
    {
        var flock = new Boids(60, worldSize: 100, new Random(1)) { Radius = 25 };
        var before = flock.Polarisation();

        for (var i = 0; i < 300; i++) flock.Step();

        Assert.True(before < 0.4);
        Assert.True(flock.Polarisation() > 0.8);
        Assert.All(flock.Flock, b => Assert.InRange(b.X, 0, 100));
    }

    [Fact]
    public void Elo_UpdatesByUpsetSize()
    {
        Assert.Equal(0.909, EloRating.ExpectedScore(1900, 1500), 3); // 400 points ≈ 10 to 1

        var elo = new EloRating();
        var (expectedWin, _) = elo.Record("champ", "rookie", 1);
        elo.Record("champ", "x", 1);
        elo.Record("champ", "y", 1);
        var (upset, loss) = elo.Record("rookie", "champ", 1);

        Assert.Equal(16, expectedWin, 6);     // equal ratings: half of K
        Assert.True(upset > 16);               // beating a higher-rated player pays more
        Assert.Equal(-upset, loss);            // zero-sum
    }

    [Fact]
    public void Elo_RatingsConvergeToTrueStrength()
    {
        var strengths = new Dictionary<string, double> { ["weak"] = 1200, ["average"] = 1500, ["strong"] = 1800 };

        var elo = EloRating.Simulate(strengths, 20_000, new Random(2));

        Assert.Equal(["strong", "average", "weak"], elo.Leaderboard.Select(p => p.Player));
        Assert.InRange(elo["strong"] - elo["weak"], 450, 750);
    }

    [Fact]
    public void Dice_ExactDistributions()
    {
        var twoD6 = DiceProbability.Sum(2, 6);

        Assert.Equal(6.0 / 36, twoD6[7], 12);
        Assert.Equal(1.0 / 36, twoD6[12], 12);
        Assert.Equal(7, DiceProbability.Expected(twoD6), 12);
        Assert.Equal(1, twoD6.Values.Sum(), 12);
        Assert.Equal(0.625, DiceProbability.AtLeast(3, 6, 10), 12); // 135/216
        Assert.InRange(DiceProbability.SimulateAtLeast(3, 6, 10, 50_000, new Random(3)), 0.615, 0.635);
    }

    [Fact]
    public void Dice_KeepHighest()
    {
        var advantage = DiceProbability.KeepHighest(2, 20, 1);
        var abilityScores = DiceProbability.KeepHighest(4, 6, 3);

        Assert.Equal(13.825, DiceProbability.Expected(advantage), 9);   // vs 10.5 for a single d20
        Assert.Equal(12.2446, DiceProbability.Expected(abilityScores), 4); // vs 10.5 for plain 3d6
        Assert.Equal(1.0 / 1296, abilityScores[3], 12);
    }

    [Fact]
    public void Sir_OutbreakDependsOnR0()
    {
        var outbreak = SirModel.Run(100_000, 10, beta: 0.3, gamma: 0.1, days: 300); // R0 = 3
        var fizzle = SirModel.Run(100_000, 10, beta: 0.08, gamma: 0.1, days: 300);   // R0 = 0.8

        var peak = SirModel.Peak(outbreak);
        Assert.InRange(peak.Susceptible / 100_000, 0.30, 0.37);     // peak when S/N ≈ 1/R0
        Assert.True(outbreak[^1].Recovered > 90_000);
        Assert.True(fizzle[^1].Recovered < 100);
        Assert.All(outbreak, s => Assert.Equal(100_000, s.Susceptible + s.Infected + s.Recovered, 6));
    }

    [Fact]
    public void Sir_HerdImmunityStopsTheOutbreak()
    {
        Assert.Equal(2.0 / 3, SirModel.HerdImmunityThreshold(3), 12);
        var protectedPopulation = SirModel.Run(100_000, 10, 0.3, 0.1, 300, vaccinated: 0.7);
        Assert.True(protectedPopulation[^1].Recovered - 70_000 < 200);
    }

    [Fact]
    public void PredatorPrey_CyclesWithALag()
    {
        var p = new PredatorPrey.Parameters(PreyGrowth: 1.0, PredationRate: 0.1, PredatorGrowth: 0.02, PredatorDeath: 0.5);

        var path = PredatorPrey.Run(p, prey: 40, predators: 9, time: 60);

        var invariant = path.Select(s => PredatorPrey.Invariant(p, s.Prey, s.Predators)).ToArray();
        Assert.True(invariant.Max() - invariant.Min() < 1e-6); // RK4 keeps the orbit closed

        var preyPeaks = PredatorPrey.Peaks(path.Select(s => (s.Time, s.Prey)).ToList());
        var predatorPeaks = PredatorPrey.Peaks(path.Select(s => (s.Time, s.Predators)).ToList());
        Assert.True(preyPeaks.Count >= 3);
        Assert.True(predatorPeaks[0] > preyPeaks[0]); // predators peak after prey
        Assert.Equal((25.0, 10.0), PredatorPrey.Equilibrium(p));
    }

    [Fact]
    public void GamblersRuin_HouseEdgeIsBrutal()
    {
        Assert.Equal(0.5, GamblersRuin.ProbabilityOfReaching(50, 100, 0.5), 12);
        Assert.True(GamblersRuin.ProbabilityOfReaching(50, 100, 18.0 / 37) < 0.07); // roulette red: 1.35% edge
        Assert.Equal(2500, GamblersRuin.ExpectedRoundsFair(50, 100));

        var (rate, rounds) = GamblersRuin.Simulate(10, 20, 0.5, 20_000, new Random(4));
        Assert.InRange(rate, 0.48, 0.52);
        Assert.InRange(rounds, 95, 105); // 10 × 10
    }
}
