namespace CSharpExercises.Scoring;

/// <summary>
/// Ten-pin bowling. A strike scores 10 plus the next two rolls, a spare 10 plus the next one, so a frame's score is
/// often unknown until later rolls arrive. The tenth frame gets bonus rolls to settle its own strike or spare.
/// A running scoreboard therefore only shows totals for frames whose bonuses are already known.
/// </summary>
public static class Bowling
{
    /// <summary>Cumulative totals for every frame that can be scored so far.</summary>
    public static IReadOnlyList<int> RunningTotals(IReadOnlyList<int> rolls)
    {
        if (rolls.Any(r => r is < 0 or > 10)) throw new ArgumentException("A roll knocks down 0 to 10 pins.", nameof(rolls));

        var totals = new List<int>();
        var total = 0;
        var i = 0;
        for (var frame = 0; frame < 10 && i < rolls.Count; frame++)
        {
            if (rolls[i] == 10)
            {
                if (frame == 9 && i + 2 < rolls.Count && rolls[i + 1] != 10 && rolls[i + 1] + rolls[i + 2] > 10)
                    throw new ArgumentException("Bonus rolls after a non-strike can't exceed 10 pins.", nameof(rolls));
                if (i + 2 >= rolls.Count) break;
                total += 10 + rolls[i + 1] + rolls[i + 2];
                i += frame == 9 ? 3 : 1;
            }
            else
            {
                if (i + 1 >= rolls.Count) break;
                var pins = rolls[i] + rolls[i + 1];
                if (pins > 10) throw new ArgumentException($"Frame {frame + 1} knocks down {pins} pins.", nameof(rolls));
                if (pins == 10)
                {
                    if (i + 2 >= rolls.Count) break;
                    total += 10 + rolls[i + 2];
                    i += frame == 9 ? 3 : 2;
                }
                else
                {
                    total += pins;
                    i += 2;
                }
            }
            totals.Add(total);
        }

        if (totals.Count == 10 && i < rolls.Count) throw new ArgumentException("Rolls after the game is over.", nameof(rolls));
        return totals;
    }

    /// <summary>The final score of a complete game.</summary>
    public static int Score(IReadOnlyList<int> rolls)
    {
        var totals = RunningTotals(rolls);
        if (totals.Count < 10) throw new InvalidOperationException("The game isn't finished.");
        return totals[^1];
    }
}
