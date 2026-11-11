namespace CSharpExercises.Tournaments;

/// <summary>
/// The circle method: pin one team and rotate the rest one place each round, pairing first with last, second with
/// second-to-last and so on. n teams play n - 1 rounds and every pair meets exactly once. With an odd number of teams a
/// dummy "bye" is added and whoever is paired with it sits the round out. The pinned team alternates home and away.
/// </summary>
public static class RoundRobin
{
    public static IReadOnlyList<IReadOnlyList<(T Home, T Away)>> Schedule<T>(IReadOnlyList<T> teams)
    {
        if (teams.Count < 2) return [];

        const int Bye = -1;
        var slots = Enumerable.Range(0, teams.Count).ToList();
        if (slots.Count % 2 == 1) slots.Add(Bye);
        var n = slots.Count;

        var rounds = new List<IReadOnlyList<(T, T)>>();
        for (var round = 0; round < n - 1; round++)
        {
            var games = new List<(T, T)>();
            for (var i = 0; i < n / 2; i++)
            {
                int a = slots[i], b = slots[n - 1 - i];
                if (a == Bye || b == Bye) continue;
                var swap = i == 0 ? round % 2 == 1 : i % 2 == 1;
                games.Add(swap ? (teams[b], teams[a]) : (teams[a], teams[b]));
            }
            rounds.Add(games);

            var last = slots[n - 1]; // rotate everyone except slot 0 one step
            slots.RemoveAt(n - 1);
            slots.Insert(1, last);
        }
        return rounds;
    }
}
