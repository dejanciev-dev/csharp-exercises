namespace CSharpExercises.MachineLearning;

/// <summary>
/// A multinomial naive Bayes text classifier, the classic spam filter. It assumes words are independent given the class
/// (naive, but it works), so P(class | words) ∝ P(class) · Π P(word | class). Probabilities are summed as logarithms to
/// avoid underflow from multiplying hundreds of tiny numbers, and Laplace smoothing (+1 to every count) keeps a word
/// never seen with a class from zeroing out the whole product.
/// </summary>
public sealed class NaiveBayes
{
    private readonly Dictionary<string, Dictionary<string, int>> _wordCounts = new();
    private readonly Dictionary<string, int> _documentCounts = new();
    private readonly Dictionary<string, int> _totalWords = new();
    private readonly HashSet<string> _vocabulary = [];

    public void Train(string label, string text)
    {
        _documentCounts[label] = _documentCounts.GetValueOrDefault(label) + 1;
        if (!_wordCounts.TryGetValue(label, out var counts)) _wordCounts[label] = counts = new Dictionary<string, int>();
        foreach (var word in Tokenize(text))
        {
            counts[word] = counts.GetValueOrDefault(word) + 1;
            _totalWords[label] = _totalWords.GetValueOrDefault(label) + 1;
            _vocabulary.Add(word);
        }
    }

    public IReadOnlyDictionary<string, double> Probabilities(string text)
    {
        var words = Tokenize(text).ToList();
        var totalDocs = _documentCounts.Values.Sum();
        var logScores = _documentCounts.Keys.ToDictionary(label => label, label =>
        {
            var score = Math.Log((double)_documentCounts[label] / totalDocs);
            var denominator = _totalWords.GetValueOrDefault(label) + _vocabulary.Count;
            foreach (var word in words) score += Math.Log((_wordCounts[label].GetValueOrDefault(word) + 1.0) / denominator);
            return score;
        });

        // Turn log scores into probabilities, subtracting the max first so Exp doesn't underflow to zero.
        var max = logScores.Values.Max();
        var exp = logScores.ToDictionary(p => p.Key, p => Math.Exp(p.Value - max));
        var sum = exp.Values.Sum();
        return exp.ToDictionary(p => p.Key, p => p.Value / sum);
    }

    public string Classify(string text) => Probabilities(text).MaxBy(p => p.Value).Key;

    /// <summary>The words that most favour a label over the others: what the model has learned.</summary>
    public IReadOnlyList<string> MostIndicative(string label, int count) =>
        _vocabulary
            .OrderByDescending(w =>
                Math.Log((_wordCounts[label].GetValueOrDefault(w) + 1.0) / (_totalWords[label] + _vocabulary.Count)) -
                _wordCounts.Keys.Where(l => l != label).Average(l => Math.Log((_wordCounts[l].GetValueOrDefault(w) + 1.0) / (_totalWords[l] + _vocabulary.Count))))
            .ThenBy(w => w, StringComparer.Ordinal)
            .Take(count)
            .ToList();

    private static IEnumerable<string> Tokenize(string text) =>
        text.ToLowerInvariant().Split([' ', ',', '.', '!', '?', ':', ';', '\n'], StringSplitOptions.RemoveEmptyEntries);
}
