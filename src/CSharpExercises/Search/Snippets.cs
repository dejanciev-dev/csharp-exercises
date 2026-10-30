using System.Text;

namespace CSharpExercises.Search;

/// <summary>
/// Search result snippets: find the window of words that contains the most query terms, cut it out with ellipses, and
/// wrap the matches in highlight markers, matching on the same stemmed terms the index uses so "running" highlights
/// for a search on "run".
/// </summary>
public static class Snippets
{
    public static string Highlight(string text, string query, int windowWords = 10, string open = "[", string close = "]")
    {
        var queryTerms = Tokenizer.Terms(query).ToHashSet();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var isMatch = words.Select(w => Tokenizer.Terms(w).Any(queryTerms.Contains)).ToArray();

        // Best window: sliding count of matches, earliest window wins ties.
        var size = Math.Min(windowWords, words.Length);
        int count = isMatch.Take(size).Count(m => m), best = count, bestStart = 0;
        for (var start = 1; start + size <= words.Length; start++)
        {
            count += (isMatch[start + size - 1] ? 1 : 0) - (isMatch[start - 1] ? 1 : 0);
            if (count > best) (best, bestStart) = (count, start);
        }

        var sb = new StringBuilder();
        if (bestStart > 0) sb.Append("… ");
        for (var i = bestStart; i < bestStart + size; i++)
        {
            if (i > bestStart) sb.Append(' ');
            sb.Append(isMatch[i] ? WrapWord(words[i], open, close) : words[i]);
        }
        if (bestStart + size < words.Length) sb.Append(" …");
        return sb.ToString();
    }

    /// <summary>Keeps punctuation outside the markers: "dogs," → "[dogs],".</summary>
    private static string WrapWord(string word, string open, string close)
    {
        var start = 0;
        var end = word.Length;
        while (start < end && !char.IsLetterOrDigit(word[start])) start++;
        while (end > start && !char.IsLetterOrDigit(word[end - 1])) end--;
        return word[..start] + open + word[start..end] + close + word[end..];
    }
}
