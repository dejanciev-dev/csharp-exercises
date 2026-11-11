using CSharpExercises.Scoring;

namespace CSharpExercises.Tests;

public class ScoringTests
{
    [Fact]
    public void Bowling_ScoresStrikesAndSpares()
    {
        Assert.Equal(300, Bowling.Score(Enumerable.Repeat(10, 12).ToArray()));
        Assert.Equal(150, Bowling.Score(Enumerable.Repeat(5, 21).ToArray()));
        Assert.Equal(0, Bowling.Score(new int[20]));
        Assert.Equal(90, Bowling.Score(Enumerable.Repeat(new[] { 9, 0 }, 10).SelectMany(f => f).ToArray()));
        Assert.Equal(167, Bowling.Score([10, 7, 3, 9, 0, 10, 0, 8, 8, 2, 0, 6, 10, 10, 10, 8, 1]));
    }

    [Fact]
    public void Bowling_RunningTotalsWaitForBonuses()
    {
        Assert.Equal([20, 39], Bowling.RunningTotals([10, 7, 3, 9]));
        Assert.Empty(Bowling.RunningTotals([10, 10]));
        Assert.Throws<InvalidOperationException>(() => Bowling.Score([10, 7, 3, 9]));
    }

    [Fact]
    public void Bowling_RejectsImpossibleGames()
    {
        Assert.Throws<ArgumentException>(() => Bowling.RunningTotals([7, 5]));
        Assert.Throws<ArgumentException>(() => Bowling.RunningTotals([11]));
        Assert.Throws<ArgumentException>(() => Bowling.RunningTotals(Enumerable.Repeat(10, 13).ToArray()));
        Assert.Throws<ArgumentException>(() => Bowling.RunningTotals([.. new int[18], 10, 5, 6]));
    }

    [Theory]
    [InlineData(0, 0, "Love-all")]
    [InlineData(2, 1, "30-15")]
    [InlineData(0, 3, "Love-40")]
    [InlineData(3, 3, "Deuce")]
    [InlineData(7, 7, "Deuce")]
    [InlineData(5, 4, "Advantage A")]
    [InlineData(4, 1, "Game A")]
    [InlineData(6, 8, "Game B")]
    public void Tennis_GameCalls(int a, int b, string expected)
    {
        Assert.Equal(expected, Tennis.GameScore(a, b));
    }

    [Fact]
    public void Tennis_RejectsUnreachableScores()
    {
        Assert.Throws<ArgumentException>(() => Tennis.GameScore(5, 0));
        Assert.Throws<ArgumentException>(() => Tennis.GameScore(3, 7));
    }

    private static void WinGame(TennisSet set, Player player)
    {
        for (var i = 0; i < 4; i++) set.PointTo(player);
    }

    [Fact]
    public void TennisSet_NeedsTwoGamesClearOrATiebreak()
    {
        var set = new TennisSet();
        for (var i = 0; i < 5; i++) { WinGame(set, Player.A); WinGame(set, Player.B); }
        WinGame(set, Player.A);
        Assert.Null(set.Winner); // 6-5 isn't enough

        WinGame(set, Player.B);
        Assert.True(set.InTiebreak);

        for (var i = 0; i < 6; i++) { set.PointTo(Player.A); set.PointTo(Player.B); }
        set.PointTo(Player.B);
        Assert.Equal("6-6, 6-7", set.ToString());
        set.PointTo(Player.B);

        Assert.Equal(Player.B, set.Winner);
        Assert.Equal((6, 7), set.Games);
        Assert.Throws<InvalidOperationException>(() => set.PointTo(Player.A));
    }

    [Fact]
    public void TennisSet_ShowsTheCurrentGame()
    {
        var set = new TennisSet();
        for (var i = 0; i < 6; i++) WinGame(set, Player.A);
        Assert.Equal(Player.A, set.Winner);

        var other = new TennisSet();
        other.PointTo(Player.B);
        other.PointTo(Player.B);
        Assert.Equal("0-0, Love-30", other.ToString());
    }

    [Theory]
    [InlineData(170, 3)]
    [InlineData(100, 2)]
    [InlineData(50, 1)]
    [InlineData(40, 1)]
    [InlineData(41, 2)]
    [InlineData(3, 2)]
    public void Darts_FindsTheShortestCheckout(int remaining, int darts)
    {
        var route = Darts.Checkout(remaining);

        Assert.NotNull(route);
        Assert.Equal(darts, route.Count);
        Assert.Equal(remaining, route.Sum(Darts.Value));
        Assert.True(route[^1] == "Bull" || route[^1].StartsWith('D'));
    }

    [Fact]
    public void Darts_KnowsTheBogeyNumbers()
    {
        var impossible = Enumerable.Range(2, 169).Where(n => Darts.Checkout(n) is null);

        Assert.Equal([159, 162, 163, 165, 166, 168, 169], impossible);
        Assert.Null(Darts.Checkout(1));
        Assert.Null(Darts.Checkout(171));
    }
}
