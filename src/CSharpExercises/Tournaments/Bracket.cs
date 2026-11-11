using System.Numerics;

namespace CSharpExercises.Tournaments;

/// <summary>
/// Single-elimination seeding: the bracket is laid out so that, if the higher seed always wins, 1 meets 2 only in the
/// final, 1 and 2 meet 3 and 4 only in the semi-finals, and so on. Each doubling step pairs every seed s with
/// (size + 1 - s). When the field isn't a power of two, the missing slots become byes and they go to the top seeds.
/// </summary>
public static class Bracket
{
    /// <summary>Seed order of the bracket's lines, top to bottom, for a power-of-two size: 8 → 1 8 4 5 2 7 3 6.</summary>
    public static IReadOnlyList<int> SeedOrder(int size)
    {
        if (size < 1 || (size & (size - 1)) != 0) throw new ArgumentException("Size must be a power of two.", nameof(size));
        var order = new List<int> { 1 };
        while (order.Count < size)
        {
            var next = order.Count * 2;
            order = order.SelectMany(s => new[] { s, next + 1 - s }).ToList();
        }
        return order;
    }

    /// <summary>First-round matches by seed; a null opponent is a bye.</summary>
    public static IReadOnlyList<(int Seed, int? Opponent)> FirstRound(int players)
    {
        if (players < 2) throw new ArgumentOutOfRangeException(nameof(players));
        var size = (int)BitOperations.RoundUpToPowerOf2((uint)players);
        var order = SeedOrder(size);
        var matches = new List<(int, int?)>();
        for (var i = 0; i < size; i += 2)
            matches.Add((order[i], order[i + 1] <= players ? order[i + 1] : null));
        return matches;
    }

    /// <summary>Plays the bracket out with the given rule and returns the champion's seed and each round's winners.</summary>
    public static (int Champion, IReadOnlyList<IReadOnlyList<int>> Rounds) Play(int players, Func<int, int, int> winner)
    {
        var alive = FirstRound(players).Select(m => m.Opponent is int o ? winner(m.Seed, o) : m.Seed).ToList();
        var rounds = new List<IReadOnlyList<int>> { alive };
        while (alive.Count > 1)
        {
            alive = alive.Chunk(2).Select(p => winner(p[0], p[1])).ToList();
            rounds.Add(alive);
        }
        return (alive[0], rounds);
    }
}
