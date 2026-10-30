using System.Text.RegularExpressions;

namespace CSharpExercises.Fuzzy;

/// <summary>
/// Peter Norvig's spelling corrector: generate every string one edit away (deletes, swaps, replacements, inserts),
/// keep the ones that are real words, and pick the most frequent. If none exist, try two edits away. Crude, but
/// "frequency of the candidate" is a surprisingly good model of what the writer meant.
/// </summary>
public sealed partial class SpellChecker
{
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyz";
    private readonly Dictionary<string, int> _counts;

    public SpellChecker(string corpus) =>
        _counts = WordPattern().Matches(corpus.ToLowerInvariant()).GroupBy(m => m.Value).ToDictionary(g => g.Key, g => g.Count());

    public bool IsKnown(string word) => _counts.ContainsKey(word.ToLowerInvariant());

    public string Correct(string word)
    {
        word = word.ToLowerInvariant();
        if (IsKnown(word)) return word;

        var oneEdit = Known(Edits1(word)).ToList();
        if (oneEdit.Count > 0) return MostFrequent(oneEdit);

        var twoEdits = Known(Edits1(word).SelectMany(Edits1)).ToList();
        return twoEdits.Count > 0 ? MostFrequent(twoEdits) : word;
    }

    /// <summary>For a word of length n: n deletes, n-1 swaps, 26n replaces and 26(n+1) inserts.</summary>
    public static HashSet<string> Edits1(string word)
    {
        var edits = new HashSet<string>();
        for (var i = 0; i <= word.Length; i++)
        {
            var (left, right) = (word[..i], word[i..]);
            if (right.Length > 0) edits.Add(left + right[1..]);
            if (right.Length > 1) edits.Add(left + right[1] + right[0] + right[2..]);
            foreach (var c in Alphabet)
            {
                if (right.Length > 0) edits.Add(left + c + right[1..]);
                edits.Add(left + c + right);
            }
        }
        edits.Remove(word);
        return edits;
    }

    private IEnumerable<string> Known(IEnumerable<string> words) => words.Where(_counts.ContainsKey).Distinct();

    private string MostFrequent(IEnumerable<string> words) =>
        words.OrderByDescending(w => _counts[w]).ThenBy(w => w, StringComparer.Ordinal).First();

    [GeneratedRegex("[a-z]+")]
    private static partial Regex WordPattern();
}
