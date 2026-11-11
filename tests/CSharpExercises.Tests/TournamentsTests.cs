using CSharpExercises.Tournaments;

namespace CSharpExercises.Tests;

public class TournamentsTests
{
    [Fact]
    public void Elo_ExpectedScoreFollowsTheRatingGap()
    {
        Assert.Equal(0.5, Elo.Expected(1500, 1500));
        Assert.Equal(10.0 / 11, Elo.Expected(1900, 1500), 9);
        Assert.Equal(1, Elo.Expected(1900, 1500) + Elo.Expected(1500, 1900), 9);
    }

    [Fact]
    public void Elo_UpsetsMoveMorePointsThanExpectedWins()
    {
        var (a, b) = Elo.Update(1500, 1500, 1);
        Assert.Equal(1516, a, 9);
        Assert.Equal(1484, b, 9);

        var (favourite, _) = Elo.Update(1900, 1500, 1);
        var (_, underdog) = Elo.Update(1900, 1500, 0);
        Assert.True(favourite - 1900 < 3);
        Assert.True(underdog - 1500 > 29);
    }

    [Fact]
    public void Elo_ReplayKeepsTheTotalConstant()
    {
        var ratings = Elo.Replay([("ann", "bob", 1), ("bob", "cat", 0.5), ("cat", "ann", 1), ("ann", "bob", 0)]);

        Assert.Equal(3 * 1500, ratings.Values.Sum(), 9);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(10)]
    public void RoundRobin_EveryPairMeetsExactlyOnce(int count)
    {
        var teams = Enumerable.Range(1, count).ToList();

        var rounds = RoundRobin.Schedule(teams);

        Assert.Equal(count % 2 == 0 ? count - 1 : count, rounds.Count);
        foreach (var round in rounds)
        {
            var playing = round.SelectMany(g => new[] { g.Home, g.Away }).ToList();
            Assert.Equal(playing.Count, playing.Distinct().Count()); // nobody plays twice in a round
        }
        var pairs = rounds.SelectMany(r => r).Select(g => (Math.Min(g.Home, g.Away), Math.Max(g.Home, g.Away))).ToList();
        Assert.Equal(count * (count - 1) / 2, pairs.Count);
        Assert.Equal(pairs.Count, pairs.Distinct().Count());
    }

    [Fact]
    public void RoundRobin_OddFieldGivesEveryoneOneBye()
    {
        string[] teams = ["A", "B", "C", "D", "E"];

        var rounds = RoundRobin.Schedule(teams);

        foreach (var team in teams)
            Assert.Single(rounds, r => !r.Any(g => g.Home == team || g.Away == team));
    }

    [Fact]
    public void Bracket_SeedsMeetLateIfFavouritesWin()
    {
        Assert.Equal([1, 8, 4, 5, 2, 7, 3, 6], Bracket.SeedOrder(8));

        var (champion, rounds) = Bracket.Play(16, Math.Min);

        Assert.Equal(1, champion);
        Assert.Equal([1, 2], rounds[^2]);
        Assert.Equal([1, 4, 2, 3], rounds[^3]);
    }

    [Fact]
    public void Bracket_ByesGoToTheTopSeeds()
    {
        var matches = Bracket.FirstRound(6);

        Assert.Equal(4, matches.Count);
        Assert.Equal([1, 2], matches.Where(m => m.Opponent is null).Select(m => m.Seed).Order());
        Assert.Contains((4, (int?)5), matches);
        Assert.Contains((3, (int?)6), matches);
    }
}
