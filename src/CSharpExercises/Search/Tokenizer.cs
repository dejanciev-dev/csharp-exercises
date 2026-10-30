using System.Globalization;
using System.Text;

namespace CSharpExercises.Search;

/// <summary>
/// Turning text into search terms: lower-case, strip accents, split on anything that isn't a letter or digit, drop
/// stop words, and stem so "running", "runs" and "run" become the same term. Indexing and querying must use the same
/// pipeline or nothing will match.
/// </summary>
public static class Tokenizer
{
    private static readonly HashSet<string> StopWords =
    [
        "a", "an", "and", "are", "as", "at", "be", "by", "for", "from", "has", "in", "is", "it",
        "of", "on", "or", "that", "the", "to", "was", "were", "will", "with",
    ];

    /// <summary>Terms with their word positions (positions count stop words too, so phrases stay aligned).</summary>
    public static IEnumerable<(string Term, int Position)> Tokenize(string text, bool stem = true)
    {
        var position = 0;
        foreach (var word in Words(RemoveAccents(text.ToLowerInvariant())))
        {
            if (!StopWords.Contains(word)) yield return (stem ? Stem(word) : word, position);
            position++;
        }
    }

    public static IEnumerable<string> Terms(string text, bool stem = true) => Tokenize(text, stem).Select(t => t.Term);

    /// <summary>
    /// A light suffix-stripping stemmer (a small subset of Porter's rules): plurals, -ing, -ed, -ly. Real stemmers
    /// have many more rules; the point is that index and query map word variants to one key.
    /// </summary>
    public static string Stem(string word)
    {
        if (word.Length <= 3) return word;
        string[][] rules =
        [
            ["sses", "ss"], ["ies", "y"], ["ing", ""], ["edly", ""], ["ed", ""], ["ly", ""], ["s", ""],
        ];
        foreach (var rule in rules)
        {
            if (!word.EndsWith(rule[0], StringComparison.Ordinal) || word.EndsWith("ss", StringComparison.Ordinal) && rule[0] == "s") continue;
            var stem = word[..^rule[0].Length] + rule[1];
            if (stem.Length < 3) continue;
            // "running" → "runn" → "run": undo a doubled final consonant left behind by -ing / -ed.
            if (rule[1].Length == 0 && rule[0] is "ing" or "ed" && stem.Length > 3 && stem[^1] == stem[^2] && stem[^1] is not ('l' or 's' or 'z'))
                stem = stem[..^1];
            return stem;
        }
        return word;
    }

    private static IEnumerable<string> Words(string text)
    {
        var sb = new StringBuilder();
        foreach (var c in text)
        {
            if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (sb.Length > 0) { yield return sb.ToString(); sb.Clear(); }
        }
        if (sb.Length > 0) yield return sb.ToString();
    }

    private static string RemoveAccents(string text) =>
        new(text.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
}
