using CSharpExercises.Tournaments;

namespace CSharpExercises.Tests;

public class TournamentsTests
{
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
