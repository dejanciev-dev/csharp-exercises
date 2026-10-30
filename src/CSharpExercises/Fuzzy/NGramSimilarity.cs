namespace CSharpExercises.Fuzzy;

/// <summary>
/// Comparing strings by their overlapping n-grams ("night" → #n, ni, ig, gh, ht, t#). Insensitive to word order and
/// cheap to index, which is why databases use trigram indexes (PostgreSQL's pg_trgm) for fuzzy LIKE searches.
/// </summary>
public static class NGramSimilarity
{
    /// <summary>Padded with '#' so the first and last characters get as much weight as the middle ones.</summary>
    public static HashSet<string> NGrams(string text, int n = 2)
    {
        var padded = new string('#', n - 1) + text.ToLowerInvariant() + new string('#', n - 1);
        var grams = new HashSet<string>();
        for (var i = 0; i + n <= padded.Length; i++) grams.Add(padded.Substring(i, n));
        return grams;
    }

    /// <summary>Shared n-grams over all n-grams.</summary>
    public static double Jaccard(string a, string b, int n = 2)
    {
        var x = NGrams(a, n);
        var y = NGrams(b, n);
        var union = x.Union(y).Count();
        return union == 0 ? 1 : (double)x.Intersect(y).Count() / union;
    }

    /// <summary>Sørensen–Dice: weighs the overlap more generously than Jaccard (2|X∩Y| / (|X|+|Y|)).</summary>
    public static double Dice(string a, string b, int n = 2)
    {
        var x = NGrams(a, n);
        var y = NGrams(b, n);
        return x.Count + y.Count == 0 ? 1 : 2.0 * x.Intersect(y).Count() / (x.Count + y.Count);
    }

    /// <summary>A trigram index: candidates sharing enough trigrams with the query, then ranked by similarity.</summary>
    public sealed class Index(IEnumerable<string> words)
    {
        private readonly Dictionary<string, List<string>> _postings = words
            .Distinct()
            .SelectMany(w => NGrams(w, 3).Select(g => (g, w)))
            .GroupBy(p => p.g, p => p.w)
            .ToDictionary(g => g.Key, g => g.ToList());

        public IEnumerable<(string Word, double Score)> Search(string query, double minScore = 0.3) =>
            NGrams(query, 3)
                .SelectMany(g => _postings.GetValueOrDefault(g) ?? [])
                .Distinct()
                .Select(w => (w, Jaccard(query, w, 3)))
                .Where(p => p.Item2 >= minScore)
                .OrderByDescending(p => p.Item2)
                .ThenBy(p => p.w, StringComparer.Ordinal);
    }
}
