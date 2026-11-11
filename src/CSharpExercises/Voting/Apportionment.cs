namespace CSharpExercises.Voting;

/// <summary>
/// Turning votes (or populations) into whole seats. Highest-averages methods hand out seats one at a time to the
/// party with the best votes ÷ divisor: D'Hondt's divisors 1, 2, 3… favour big parties, Sainte-Laguë's 1, 3, 5… are
/// more even-handed. Hamilton's largest-remainder method looks fairer still, but it suffers the Alabama paradox:
/// adding a seat to the house can take a seat away from someone.
/// </summary>
public static class Apportionment
{
    public static IReadOnlyDictionary<string, int> DHondt(IReadOnlyDictionary<string, long> votes, int seats) =>
        HighestAverages(votes, seats, won => won + 1);

    public static IReadOnlyDictionary<string, int> SainteLague(IReadOnlyDictionary<string, long> votes, int seats) =>
        HighestAverages(votes, seats, won => 2 * won + 1);

    private static Dictionary<string, int> HighestAverages(
        IReadOnlyDictionary<string, long> votes, int seats, Func<int, int> divisor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(seats);
        var won = votes.Keys.ToDictionary(p => p, _ => 0);
        var queue = new PriorityQueue<string, (double Average, long Votes, string Name)>(
            Comparer<(double Average, long Votes, string Name)>.Create((a, b) =>
                b.Average != a.Average ? b.Average.CompareTo(a.Average)
                : b.Votes != a.Votes ? b.Votes.CompareTo(a.Votes)
                : string.CompareOrdinal(a.Name, b.Name)));

        foreach (var (party, count) in votes) queue.Enqueue(party, ((double)count / divisor(0), count, party));
        for (var seat = 0; seat < seats && queue.Count > 0; seat++)
        {
            var party = queue.Dequeue();
            won[party]++;
            queue.Enqueue(party, ((double)votes[party] / divisor(won[party]), votes[party], party));
        }
        return won;
    }

    /// <summary>Hamilton / largest remainder: everyone gets the whole part of their quota, leftovers go to the largest fractions.</summary>
    public static IReadOnlyDictionary<string, int> Hamilton(IReadOnlyDictionary<string, long> votes, int seats)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(seats);
        var total = votes.Values.Sum();
        if (total == 0) throw new ArgumentException("No votes to apportion.", nameof(votes));

        // Work in exact integers: quota = votes × seats / total, remainder = (votes × seats) mod total.
        var won = votes.ToDictionary(v => v.Key, v => (int)(v.Value * seats / total));
        var left = seats - won.Values.Sum();
        foreach (var party in votes
                     .OrderByDescending(v => v.Value * seats % total)
                     .ThenByDescending(v => v.Value)
                     .ThenBy(v => v.Key, StringComparer.Ordinal)
                     .Take(left)
                     .Select(v => v.Key))
            won[party]++;
        return won;
    }
}
