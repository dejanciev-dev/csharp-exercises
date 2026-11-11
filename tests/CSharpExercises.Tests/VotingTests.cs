using CSharpExercises.Voting;

namespace CSharpExercises.Tests;

public class VotingTests
{
    private static readonly string[] Cities = ["Memphis", "Nashville", "Chattanooga", "Knoxville"];

    /// <summary>The Tennessee capital example, one ballot per percent of voters.</summary>
    private static List<IReadOnlyList<string>> Tennessee()
    {
        var ballots = new List<IReadOnlyList<string>>();
        void Add(int count, params string[] ranking) => ballots.AddRange(Enumerable.Repeat(ranking, count));
        Add(42, "Memphis", "Nashville", "Chattanooga", "Knoxville");
        Add(26, "Nashville", "Chattanooga", "Knoxville", "Memphis");
        Add(15, "Chattanooga", "Knoxville", "Nashville", "Memphis");
        Add(17, "Knoxville", "Chattanooga", "Nashville", "Memphis");
        return ballots;
    }

    [Fact]
    public void Plurality_PicksTheBiggestBloc()
    {
        var tally = RankedVoting.Plurality(Tennessee());

        Assert.Equal(("Memphis", 42), tally[0]);
        Assert.Equal(("Chattanooga", 15), tally[^1]);
    }

    [Fact]
    public void InstantRunoff_TransfersEliminatedVotes()
    {
        var (winner, rounds) = RankedVoting.InstantRunoff(Tennessee());

        Assert.Equal("Knoxville", winner);
        Assert.Equal(3, rounds.Count);
        Assert.Equal(32, rounds[1]["Knoxville"]); // Chattanooga's 15 went to Knoxville
        Assert.Equal(58, rounds[2]["Knoxville"]);
    }

    [Fact]
    public void Borda_AndCondorcet_FindTheCompromise()
    {
        var points = RankedVoting.Borda(Tennessee(), Cities);

        Assert.Equal(194, points["Nashville"]);
        Assert.Equal("Nashville", points.MaxBy(p => p.Value).Key);
        Assert.Equal("Nashville", RankedVoting.CondorcetWinner(Tennessee(), Cities));
    }

    [Fact]
    public void Condorcet_CanCycle()
    {
        IReadOnlyList<IReadOnlyList<string>> ballots = [["R", "S", "P"], ["S", "P", "R"], ["P", "R", "S"]];

        Assert.Null(RankedVoting.CondorcetWinner(ballots, ["R", "S", "P"]));
        Assert.Equal(2, RankedVoting.Prefer(ballots, "R", "S"));
    }

    [Fact]
    public void InstantRunoff_IgnoresExhaustedBallots()
    {
        IReadOnlyList<IReadOnlyList<string>> ballots = [["A"], ["A"], ["B"], ["B"], ["C", "B"], ["C"], ["D"]];

        var (winner, rounds) = RankedVoting.InstantRunoff(ballots);

        Assert.Equal("B", winner);
        Assert.Equal(3, rounds.Count);
        Assert.Equal(3, rounds[^1]["B"]); // 3 of 7 ballots, but a majority of the 5 still ranking someone
        Assert.Equal(5, rounds[^1].Values.Sum());
    }

    private static readonly Dictionary<string, long> Parties = new()
    {
        ["A"] = 100_000, ["B"] = 80_000, ["C"] = 30_000, ["D"] = 20_000,
    };

    [Fact]
    public void DHondt_FavoursLargeParties()
    {
        var seats = Apportionment.DHondt(Parties, 8);

        Assert.Equal([4, 3, 1, 0], new[] { "A", "B", "C", "D" }.Select(p => seats[p]));
    }

    [Fact]
    public void SainteLague_IsKinderToSmallParties()
    {
        var seats = Apportionment.SainteLague(Parties, 8);

        Assert.Equal([3, 3, 1, 1], new[] { "A", "B", "C", "D" }.Select(p => seats[p]));
    }

    [Fact]
    public void Hamilton_ShowsTheAlabamaParadox()
    {
        var states = new Dictionary<string, long> { ["A"] = 6, ["B"] = 6, ["C"] = 2 };

        var ten = Apportionment.Hamilton(states, 10);
        var eleven = Apportionment.Hamilton(states, 11);

        Assert.Equal(2, ten["C"]);
        Assert.Equal(1, eleven["C"]); // a bigger house, yet C loses a seat
        Assert.Equal(11, eleven.Values.Sum());
    }
}
