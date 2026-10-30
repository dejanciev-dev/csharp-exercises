namespace CSharpExercises.Fuzzy;

/// <summary>
/// Jaro–Winkler similarity (0 to 1), designed for short strings like names in record linkage: characters "match" if
/// equal and not too far apart, transpositions count half, and a shared prefix earns a bonus because typos are
/// rarer at the start of a name. MARTHA/MARHTA scores 0.961 where edit distance only says "2 edits".
/// </summary>
public static class JaroWinkler
{
    public static double Jaro(string a, string b)
    {
        if (a.Length == 0 && b.Length == 0) return 1;
        if (a.Length == 0 || b.Length == 0) return 0;

        var window = Math.Max(0, Math.Max(a.Length, b.Length) / 2 - 1);
        var aMatched = new bool[a.Length];
        var bMatched = new bool[b.Length];
        var matches = 0;

        for (var i = 0; i < a.Length; i++)
        {
            for (int j = Math.Max(0, i - window), end = Math.Min(b.Length, i + window + 1); j < end; j++)
            {
                if (bMatched[j] || a[i] != b[j]) continue;
                aMatched[i] = bMatched[j] = true;
                matches++;
                break;
            }
        }
        if (matches == 0) return 0;

        // Matched characters that appear in a different order count as transpositions (each pair counted twice).
        var transpositions = 0;
        for (int i = 0, j = 0; i < a.Length; i++)
        {
            if (!aMatched[i]) continue;
            while (!bMatched[j]) j++;
            if (a[i] != b[j]) transpositions++;
            j++;
        }

        double m = matches;
        return (m / a.Length + m / b.Length + (m - transpositions / 2.0) / m) / 3;
    }

    public static double Similarity(string a, string b, double prefixScale = 0.1)
    {
        var jaro = Jaro(a, b);
        var prefix = 0;
        while (prefix < Math.Min(4, Math.Min(a.Length, b.Length)) && a[prefix] == b[prefix]) prefix++;
        return jaro + prefix * prefixScale * (1 - jaro);
    }

    /// <summary>The candidates above a threshold, best first: a "did you mean" for names.</summary>
    public static IEnumerable<(string Candidate, double Score)> BestMatches(string query, IEnumerable<string> candidates, double threshold = 0.85) =>
        candidates
            .Select(c => (c, Similarity(query.ToUpperInvariant(), c.ToUpperInvariant())))
            .Where(p => p.Item2 >= threshold)
            .OrderByDescending(p => p.Item2);
}
