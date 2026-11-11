namespace CSharpExercises.Scoring;

public enum Player { A, B }

/// <summary>
/// Tennis scoring is nested state machines. Points make games (Love, 15, 30, 40, then deuce and advantage until
/// someone leads by two), games make sets (first to 6 by two, otherwise a tiebreak at 6-6), and the tiebreak counts
/// plain numbers to 7 by two. Some scores can never happen: 5-0 in points means the game ended at 4-0.
/// </summary>
public static class Tennis
{
    private static readonly string[] Calls = ["Love", "15", "30", "40"];

    /// <summary>The umpire's call for a game in progress, from each player's points won.</summary>
    public static string GameScore(int a, int b)
    {
        if (a < 0 || b < 0) throw new ArgumentOutOfRangeException(a < 0 ? nameof(a) : nameof(b));
        var (high, low) = (Math.Max(a, b), Math.Min(a, b));
        if (high > 4 && high - low > 2) throw new ArgumentException($"{a}-{b} can't happen: the game was already over.");

        var leader = a > b ? Player.A : Player.B;
        if (high >= 4 && high - low >= 2) return $"Game {leader}";
        if (a >= 3 && b >= 3) return a == b ? "Deuce" : $"Advantage {leader}";
        return a == b ? $"{Calls[a]}-all" : $"{Calls[a]}-{Calls[b]}";
    }
}

/// <summary>One set, fed point by point.</summary>
public sealed class TennisSet
{
    private readonly int[] _points = new int[2];
    private readonly int[] _games = new int[2];

    public Player? Winner { get; private set; }

    public bool InTiebreak => _games[0] == 6 && _games[1] == 6;

    public (int A, int B) Games => (_games[0], _games[1]);

    public void PointTo(Player player)
    {
        if (Winner is not null) throw new InvalidOperationException("The set is over.");
        int me = (int)player, other = 1 - me;

        _points[me]++;
        var target = InTiebreak ? 7 : 4;
        if (_points[me] < target || _points[me] - _points[other] < 2) return;

        _points[0] = _points[1] = 0;
        _games[me]++;
        if (_games[me] == 7 || (_games[me] == 6 && _games[other] <= 4)) Winner = player;
    }

    /// <summary>Games, then the current game or tiebreak if a point has been played in it: "6-5, Advantage B".</summary>
    public override string ToString()
    {
        var games = $"{_games[0]}-{_games[1]}";
        if (Winner is not null || (_points[0] == 0 && _points[1] == 0)) return games;
        var current = InTiebreak ? $"{_points[0]}-{_points[1]}" : Tennis.GameScore(_points[0], _points[1]);
        return $"{games}, {current}";
    }
}
