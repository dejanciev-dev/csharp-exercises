namespace CSharpExercises.Recommendations;

/// <summary>
/// How sites rank content. Hacker News divides votes by a power of age so fresh items rise and old ones sink ("gravity").
/// Reddit's hot score uses log10 of the net votes plus a time bonus, so the first 10 votes count as much as the next 90.
/// For "best rated", sorting by average rating is wrong (one 5-star review beats 1,000 at 4.9); the lower bound of the
/// Wilson score interval asks "what's the worst the true rating plausibly is?" and needs evidence to rank high.
/// </summary>
public static class RankingFormulas
{
    public static double HackerNews(int points, double ageHours, double gravity = 1.8) =>
        (points - 1) / Math.Pow(ageHours + 2, gravity);

    public static double RedditHot(int upvotes, int downvotes, DateTimeOffset posted)
    {
        var score = upvotes - downvotes;
        var order = Math.Log10(Math.Max(Math.Abs(score), 1));
        var sign = Math.Sign(score);
        var seconds = (posted - new DateTimeOffset(2005, 12, 8, 7, 46, 43, TimeSpan.Zero)).TotalSeconds;
        return Math.Round(sign * order + seconds / 45000, 7); // 45,000 s = 12.5 h buys a factor of 10 in votes
    }

    /// <summary>Lower bound of the Wilson score interval for the share of positive votes (z = 1.96 for 95% confidence).</summary>
    public static double WilsonLowerBound(int positive, int total, double z = 1.96)
    {
        if (total == 0) return 0;
        var p = (double)positive / total;
        var z2 = z * z;
        return (p + z2 / (2 * total) - z * Math.Sqrt((p * (1 - p) + z2 / (4 * total)) / total)) / (1 + z2 / total);
    }

    /// <summary>Star ratings: treat each star level as a share of "positive" and rank by a Bayesian average instead.</summary>
    public static double BayesianAverage(IReadOnlyList<int> stars, double priorMean = 3, int priorWeight = 10) =>
        (priorMean * priorWeight + stars.Sum()) / (priorWeight + stars.Count);
}
