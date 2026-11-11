namespace CSharpExercises.Tournaments;

/// <summary>
/// Elo ratings: a rating gap predicts a score (400 points means the stronger player is expected to score 10 times as
/// much), and after a game each player moves by K times the surprise, actual minus expected. Points are only moved,
/// never created, so the pool's total stays the same; an upset moves more points than an expected win.
/// </summary>
public static class Elo
{
    /// <summary>Expected score of A against B: 1 is a sure win, 0.5 an even game.</summary>
    public static double Expected(double ratingA, double ratingB) =>
        1 / (1 + Math.Pow(10, (ratingB - ratingA) / 400));

    /// <summary>New ratings after a game where A scored <paramref name="scoreA"/> (1 win, 0.5 draw, 0 loss).</summary>
    public static (double A, double B) Update(double ratingA, double ratingB, double scoreA, double k = 32)
    {
        if (scoreA is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(scoreA));
        var delta = k * (scoreA - Expected(ratingA, ratingB));
        return (ratingA + delta, ratingB - delta);
    }

    /// <summary>Replays games in order and returns the final ratings, everyone starting at <paramref name="initial"/>.</summary>
    public static IReadOnlyDictionary<string, double> Replay(
        IEnumerable<(string A, string B, double ScoreA)> games, double initial = 1500, double k = 32)
    {
        var ratings = new Dictionary<string, double>();
        foreach (var (a, b, score) in games)
        {
            var (ra, rb) = Update(ratings.GetValueOrDefault(a, initial), ratings.GetValueOrDefault(b, initial), score, k);
            ratings[a] = ra;
            ratings[b] = rb;
        }
        return ratings;
    }
}
