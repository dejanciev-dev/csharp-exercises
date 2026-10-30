namespace CSharpExercises.Algorithms;

/// <summary>
/// Splitting text without spaces into dictionary words ("applepenapple" → "apple pen apple"), as needed for
/// hashtags, domain names or languages written without spaces. Memoising by start position turns an exponential
/// search into O(n²) substring checks.
/// </summary>
public sealed class WordBreak(IEnumerable<string> dictionary)
{
    private readonly HashSet<string> _words = new(dictionary, StringComparer.OrdinalIgnoreCase);
    private readonly int _longest = dictionary.DefaultIfEmpty("").Max(w => w.Length);

    public bool CanSegment(string text)
    {
        // possible[i]: text[..i] can be split into words.
        var possible = new bool[text.Length + 1];
        possible[0] = true;
        for (var end = 1; end <= text.Length; end++)
            for (var start = Math.Max(0, end - _longest); start < end && !possible[end]; start++)
                possible[end] = possible[start] && _words.Contains(text[start..end]);
        return possible[text.Length];
    }

    /// <summary>Every way to split the text, with a cache so shared suffixes are only solved once.</summary>
    public IReadOnlyList<string> AllSegmentations(string text)
    {
        var memo = new Dictionary<int, List<string>>();

        List<string> From(int start)
        {
            if (start == text.Length) return [""];
            if (memo.TryGetValue(start, out var cached)) return cached;

            var results = new List<string>();
            for (var end = start + 1; end <= Math.Min(text.Length, start + _longest); end++)
            {
                var word = text[start..end];
                if (!_words.Contains(word)) continue;
                foreach (var rest in From(end)) results.Add(rest.Length == 0 ? word : $"{word} {rest}");
            }
            return memo[start] = results;
        }

        return From(0);
    }

    /// <summary>The split with the fewest words, e.g. for turning #ilovenewyork into readable text.</summary>
    public string? FewestWords(string text)
    {
        var best = new (int Words, int Previous)[text.Length + 1];
        Array.Fill(best, (int.MaxValue, -1));
        best[0] = (0, -1);

        for (var end = 1; end <= text.Length; end++)
            for (var start = Math.Max(0, end - _longest); start < end; start++)
                if (best[start].Words != int.MaxValue && best[start].Words + 1 < best[end].Words && _words.Contains(text[start..end]))
                    best[end] = (best[start].Words + 1, start);

        if (best[text.Length].Words == int.MaxValue) return null;

        var parts = new List<string>();
        for (var end = text.Length; end > 0; end = best[end].Previous) parts.Add(text[best[end].Previous..end]);
        parts.Reverse();
        return string.Join(' ', parts);
    }
}
