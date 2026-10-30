namespace CSharpExercises.Recommendations;

/// <summary>
/// User-based collaborative filtering: "people who rated like you also liked…". Similarity between users is the cosine
/// of their mean-centred rating vectors over the items both rated (centring removes the difference between generous and
/// harsh raters). A predicted rating is the user's mean plus the similarity-weighted deviations of the most similar
/// users who rated the item; recommendations are the unrated items with the highest predictions.
/// </summary>
public sealed class CollaborativeFiltering
{
    private readonly Dictionary<string, Dictionary<string, double>> _ratings = new();

    public void Rate(string user, string item, double rating)
    {
        if (!_ratings.TryGetValue(user, out var items)) _ratings[user] = items = new Dictionary<string, double>();
        items[item] = rating;
    }

    public double Mean(string user) => _ratings[user].Values.Average();

    /// <summary>Pearson-style similarity in [-1, 1]; 0 when the users share fewer than two items.</summary>
    public double Similarity(string a, string b)
    {
        var common = _ratings[a].Keys.Intersect(_ratings[b].Keys).ToList();
        if (common.Count < 2) return 0;
        var (meanA, meanB) = (Mean(a), Mean(b));
        double dot = 0, normA = 0, normB = 0;
        foreach (var item in common)
        {
            var (x, y) = (_ratings[a][item] - meanA, _ratings[b][item] - meanB);
            dot += x * y;
            normA += x * x;
            normB += y * y;
        }
        return normA == 0 || normB == 0 ? 0 : dot / Math.Sqrt(normA * normB);
    }

    public double? Predict(string user, string item, int neighbours = 5)
    {
        var peers = _ratings.Keys
            .Where(other => other != user && _ratings[other].ContainsKey(item))
            .Select(other => (Other: other, Similarity: Similarity(user, other)))
            .Where(p => p.Similarity > 0)
            .OrderByDescending(p => p.Similarity)
            .Take(neighbours)
            .ToList();
        if (peers.Count == 0) return null;

        var weighted = peers.Sum(p => p.Similarity * (_ratings[p.Other][item] - Mean(p.Other)));
        return Mean(user) + weighted / peers.Sum(p => p.Similarity);
    }

    public IReadOnlyList<(string Item, double Predicted)> Recommend(string user, int count)
    {
        var unrated = _ratings.Values.SelectMany(r => r.Keys).Distinct().Where(i => !_ratings[user].ContainsKey(i));
        return unrated
            .Select(item => (item, Predict(user, item)))
            .Where(p => p.Item2 is not null)
            .Select(p => (p.item, Math.Round(p.Item2!.Value, 2)))
            .OrderByDescending(p => p.Item2)
            .ThenBy(p => p.item, StringComparer.Ordinal)
            .Take(count)
            .ToList();
    }
}
