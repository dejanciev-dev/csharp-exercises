namespace CSharpExercises.Voting;

/// <summary>
/// The same ranked ballots can elect different winners depending on the counting rule. In the textbook Tennessee
/// capital example, plurality picks Memphis (the largest single bloc), instant runoff picks Knoxville, and Borda and
/// Condorcet pick Nashville, the city everyone else ranks second. A Condorcet winner beats every rival head to head,
/// but there may be none: preferences can cycle like rock, paper, scissors.
/// </summary>
public static class RankedVoting
{
    /// <summary>First-choice counts, most votes first.</summary>
    public static IReadOnlyList<(string Candidate, int Votes)> Plurality(IEnumerable<IReadOnlyList<string>> ballots) =>
        ballots.Where(b => b.Count > 0)
            .GroupBy(b => b[0])
            .Select(g => (g.Key, g.Count()))
            .OrderByDescending(t => t.Item2).ThenBy(t => t.Key, StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// Instant runoff: count each ballot for its highest-ranked candidate still standing; if nobody has a majority of
    /// the ballots still in play, eliminate the last-placed candidate (ties go out alphabetically last) and recount.
    /// </summary>
    public static (string Winner, IReadOnlyList<IReadOnlyDictionary<string, int>> Rounds) InstantRunoff(
        IReadOnlyList<IReadOnlyList<string>> ballots)
    {
        var standing = ballots.SelectMany(b => b).ToHashSet();
        if (standing.Count == 0) throw new ArgumentException("No candidates on any ballot.", nameof(ballots));
        var rounds = new List<IReadOnlyDictionary<string, int>>();

        while (true)
        {
            var tally = standing.ToDictionary(c => c, _ => 0);
            foreach (var ballot in ballots)
                if (ballot.FirstOrDefault(standing.Contains) is { } choice) tally[choice]++;
            rounds.Add(tally);

            var live = tally.Values.Sum(); // exhausted ballots no longer count toward the majority
            var leader = tally.MaxBy(t => t.Value);
            if (leader.Value * 2 > live || standing.Count == 1) return (leader.Key, rounds);

            var loser = tally.OrderBy(t => t.Value).ThenByDescending(t => t.Key, StringComparer.Ordinal).First().Key;
            standing.Remove(loser);
        }
    }

    /// <summary>Borda count: with n candidates, a ballot gives n - 1 points to its first choice, n - 2 to the next, and so on.</summary>
    public static IReadOnlyDictionary<string, int> Borda(IEnumerable<IReadOnlyList<string>> ballots, IReadOnlyCollection<string> candidates)
    {
        var points = candidates.ToDictionary(c => c, _ => 0);
        foreach (var ballot in ballots)
            for (var rank = 0; rank < ballot.Count; rank++)
                points[ballot[rank]] += candidates.Count - 1 - rank;
        return points;
    }

    /// <summary>How many ballots prefer <paramref name="x"/> to <paramref name="y"/>; a ranked candidate beats an unranked one.</summary>
    public static int Prefer(IEnumerable<IReadOnlyList<string>> ballots, string x, string y) =>
        ballots.Count(b =>
        {
            int ix = IndexOf(b, x), iy = IndexOf(b, y);
            return ix < iy;
        });

    private static int IndexOf(IReadOnlyList<string> ballot, string candidate)
    {
        for (var i = 0; i < ballot.Count; i++)
            if (ballot[i] == candidate) return i;
        return int.MaxValue;
    }

    /// <summary>The candidate who wins every head-to-head contest, or null when there is a cycle or a tie.</summary>
    public static string? CondorcetWinner(IReadOnlyList<IReadOnlyList<string>> ballots, IReadOnlyCollection<string> candidates) =>
        candidates.FirstOrDefault(c => candidates.All(o => o == c || Prefer(ballots, c, o) > Prefer(ballots, o, c)));
}
