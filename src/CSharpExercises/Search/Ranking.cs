namespace CSharpExercises.Search;

/// <summary>
/// Ranking matches by relevance. TF-IDF: a term counts more the more often it appears in the document (term frequency)
/// and the rarer it is across all documents (inverse document frequency), so "the" is worth nothing and "kubernetes"
/// a lot. BM25, the default in Lucene and Elasticsearch, adds two fixes: repeated terms saturate (k1) and long
/// documents are normalised by length (b).
/// </summary>
public static class Ranking
{
    public static IReadOnlyList<(int Id, double Score)> TfIdf(InvertedIndex index, string query) =>
        Score(index, query, (term, id) =>
        {
            var tf = index.TermFrequency(term, id);
            var idf = Math.Log((double)index.DocumentCount / index.DocumentFrequency(term));
            return tf == 0 ? 0 : (1 + Math.Log(tf)) * idf;
        });

    public static IReadOnlyList<(int Id, double Score)> Bm25(InvertedIndex index, string query, double k1 = 1.2, double b = 0.75) =>
        Score(index, query, (term, id) =>
        {
            double tf = index.TermFrequency(term, id);
            double n = index.DocumentCount, df = index.DocumentFrequency(term);
            var idf = Math.Log(1 + (n - df + 0.5) / (df + 0.5));
            var lengthRatio = index.Length(id) / index.AverageLength;
            return idf * tf * (k1 + 1) / (tf + k1 * (1 - b + b * lengthRatio));
        });

    private static IReadOnlyList<(int Id, double Score)> Score(InvertedIndex index, string query, Func<string, int, double> termScore)
    {
        var terms = Tokenizer.Terms(query).Distinct().Where(t => index.DocumentFrequency(t) > 0).ToList();
        return terms
            .SelectMany(index.Postings)
            .Distinct()
            .Select(id => (id, terms.Sum(t => termScore(t, id))))
            .OrderByDescending(r => r.Item2)
            .ThenBy(r => r.id)
            .ToList();
    }
}
