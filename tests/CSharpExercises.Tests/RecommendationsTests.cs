using CSharpExercises.Recommendations;

namespace CSharpExercises.Tests;

public class RecommendationsTests
{
    private static CollaborativeFiltering Movies()
    {
        var cf = new CollaborativeFiltering();
        void Rate(string user, params (string Movie, double Stars)[] ratings)
        {
            foreach (var (movie, stars) in ratings) cf.Rate(user, movie, stars);
        }

        Rate("ana", ("Alien", 5), ("Aliens", 5), ("Notebook", 1), ("Titanic", 2));
        Rate("ben", ("Alien", 4), ("Aliens", 5), ("Notebook", 2), ("Predator", 5));
        Rate("cy", ("Alien", 1), ("Aliens", 2), ("Notebook", 5), ("Titanic", 5), ("Love Actually", 5));
        Rate("dee", ("Alien", 5), ("Notebook", 1), ("Predator", 4), ("Love Actually", 1));
        return cf;
    }

    [Fact]
    public void CollaborativeFiltering_FindsLikeMindedUsers()
    {
        var cf = Movies();

        Assert.True(cf.Similarity("ana", "ben") > 0.8);
        Assert.True(cf.Similarity("ana", "cy") < -0.8);
        cf.Rate("eve", "Alien", 5);
        Assert.Equal(0, cf.Similarity("ana", "eve")); // one movie in common isn't enough to judge
    }

    [Fact]
    public void CollaborativeFiltering_RecommendsWhatSimilarUsersLiked()
    {
        var cf = Movies();

        var recommendations = cf.Recommend("ana", 2);

        Assert.Equal("Predator", recommendations[0].Item);
        Assert.True(recommendations[0].Predicted > cf.Mean("ana"));
        Assert.True(cf.Predict("ana", "Love Actually") is null or < 3.25);
    }

    private static readonly IReadOnlyList<IReadOnlySet<string>> Baskets =
    [
        new HashSet<string> { "bread", "milk" },
        new HashSet<string> { "bread", "diapers", "beer", "eggs" },
        new HashSet<string> { "milk", "diapers", "beer", "cola" },
        new HashSet<string> { "bread", "milk", "diapers", "beer" },
        new HashSet<string> { "bread", "milk", "diapers", "cola" },
    ];

    [Fact]
    public void Apriori_FindsFrequentItemsets()
    {
        var itemsets = AssociationRules.FrequentItemsets(Baskets, minSupport: 0.6);

        Assert.Equal(0.8, itemsets["bread"]);
        Assert.Equal(0.6, itemsets["beer|diapers"]);
        Assert.False(itemsets.ContainsKey("cola"));          // in only 2 of 5 baskets
        Assert.False(itemsets.ContainsKey("beer|bread|diapers"));
    }

    [Fact]
    public void Apriori_RulesWithConfidenceAndLift()
    {
        var rules = AssociationRules.Rules(Baskets, minSupport: 0.6, minConfidence: 0.7);
        var beer = rules.Single(r => r.If.SetEquals(["beer"]) && r.Then.SetEquals(["diapers"]));

        Assert.Equal(1.0, beer.Confidence);   // everyone who bought beer bought diapers
        Assert.Equal(1.25, beer.Lift);        // and that's more than diapers' 80% base rate
        Assert.Equal("{beer} → {diapers} (conf 1.00, lift 1.25)", beer.ToString());
        Assert.All(rules, r => Assert.True(r.Confidence >= 0.7));
    }

    [Fact]
    public void Ranking_FreshnessAndEvidence()
    {
        // Same points: the newer story ranks higher. More points can't beat age forever.
        Assert.True(RankingFormulas.HackerNews(100, 1) > RankingFormulas.HackerNews(100, 10));
        Assert.True(RankingFormulas.HackerNews(50, 2) > RankingFormulas.HackerNews(500, 48));

        var posted = new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal(1, RankingFormulas.RedditHot(110, 10, posted) - RankingFormulas.RedditHot(20, 10, posted), 6); // 100 vs 10: one order
        Assert.Equal(1, RankingFormulas.RedditHot(20, 10, posted.AddHours(12.5)) - RankingFormulas.RedditHot(20, 10, posted), 6);
    }

    [Fact]
    public void Ranking_WilsonNeedsEvidence()
    {
        var oneReview = RankingFormulas.WilsonLowerBound(1, 1);
        var manyReviews = RankingFormulas.WilsonLowerBound(950, 1000);

        Assert.True(manyReviews > oneReview);           // 95% of 1000 beats 100% of 1
        Assert.InRange(manyReviews, 0.93, 0.95);
        Assert.Equal(0, RankingFormulas.WilsonLowerBound(0, 0));
        Assert.True(RankingFormulas.BayesianAverage([5]) < RankingFormulas.BayesianAverage(Enumerable.Repeat(5, 100).ToArray()));
    }
}
