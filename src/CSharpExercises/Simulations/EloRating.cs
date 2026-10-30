namespace CSharpExercises.Simulations;

/// <summary>
/// Elo ratings (chess, matchmaking, ranking anything by pairwise comparisons): the rating difference predicts the
/// expected score, 1 / (1 + 10^(-diff/400)), so 400 points ahead means winning about 10 to 1. After a game each player
/// moves by K × (actual - expected): beating a stronger player gains a lot, beating a weaker one very little. Points
/// are zero-sum, and with enough games ratings converge to true strength.
/// </summary>
public sealed class EloRating(double k = 32, double initial = 1500)
{
    private readonly Dictionary<string, double> _ratings = new();

    public double this[string player] => _ratings.GetValueOrDefault(player, initial);

    public IReadOnlyList<(string Player, double Rating)> Leaderboard =>
        _ratings.OrderByDescending(p => p.Value).Select(p => (p.Key, Math.Round(p.Value, 1))).ToList();

    public static double ExpectedScore(double rating, double opponent) => 1 / (1 + Math.Pow(10, (opponent - rating) / 400));

    /// <param name="score">1 for a win by <paramref name="a"/>, 0.5 for a draw, 0 for a loss.</param>
    public (double DeltaA, double DeltaB) Record(string a, string b, double score)
    {
        if (score is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(score));
        var (ra, rb) = (this[a], this[b]);
        var delta = k * (score - ExpectedScore(ra, rb));
        _ratings[a] = ra + delta;
        _ratings[b] = rb - delta;
        return (delta, -delta);
    }

    /// <summary>Plays many games between players of known true strength and lets the ratings find it.</summary>
    public static EloRating Simulate(IReadOnlyDictionary<string, double> trueStrength, int games, Random random)
    {
        var elo = new EloRating();
        var players = trueStrength.Keys.ToArray();
        for (var g = 0; g < games; g++)
        {
            var a = players[random.Next(players.Length)];
            var b = players[random.Next(players.Length)];
            if (a == b) continue;
            var aWins = random.NextDouble() < ExpectedScore(trueStrength[a], trueStrength[b]);
            elo.Record(a, b, aWins ? 1 : 0);
        }
        return elo;
    }
}
