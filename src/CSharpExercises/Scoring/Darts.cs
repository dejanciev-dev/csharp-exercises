namespace CSharpExercises.Scoring;

/// <summary>
/// Darts checkouts: in 501 you must hit exactly zero and the last dart must be a double (the bull counts as double 25).
/// Finding the fewest darts is a tiny search over the 62 scoring beds, at most three deep. The highest checkout is
/// 170 (T20 T20 Bull), and a handful of lower "bogey numbers" such as 169 and 159 have no three-dart finish at all.
/// </summary>
public static class Darts
{
    private static readonly (string Name, int Value)[] Beds = BuildBeds();

    private static readonly (string Name, int Value)[] Doubles = Beds.Where(b => IsDouble(b.Name)).ToArray();

    private static (string, int)[] BuildBeds()
    {
        var beds = new List<(string, int)>();
        for (var n = 1; n <= 20; n++)
        {
            beds.Add(($"S{n}", n));
            beds.Add(($"D{n}", 2 * n));
            beds.Add(($"T{n}", 3 * n));
        }
        beds.Add(("S25", 25));
        beds.Add(("Bull", 50));
        return beds.OrderByDescending(b => b.Item2).ToArray(); // try big set-up darts first
    }

    private static bool IsDouble(string name) => name == "Bull" || name[0] == 'D';

    public static int Value(string bed) =>
        Beds.FirstOrDefault(b => b.Name == bed) is { Name: not null } found
            ? found.Value
            : throw new ArgumentException($"Unknown bed '{bed}'.", nameof(bed));

    /// <summary>The shortest way to finish <paramref name="remaining"/> in one visit, or null if there is none.</summary>
    public static IReadOnlyList<string>? Checkout(int remaining)
    {
        for (var darts = 1; darts <= 3; darts++)
        {
            var route = Search(remaining, darts);
            if (route is not null) return route;
        }
        return null;
    }

    private static List<string>? Search(int remaining, int darts)
    {
        if (darts == 1)
        {
            foreach (var (name, value) in Doubles)
                if (value == remaining) return [name];
            return null;
        }
        foreach (var (name, value) in Beds)
        {
            if (value >= remaining) continue; // must leave something for the double
            var rest = Search(remaining - value, darts - 1);
            if (rest is not null) return [name, .. rest];
        }
        return null;
    }
}
