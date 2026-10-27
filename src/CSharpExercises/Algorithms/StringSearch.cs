namespace CSharpExercises.Algorithms;

/// <summary>
/// Knuth–Morris–Pratt substring search. The naive search restarts one position later after every mismatch, which is
/// O(n·m) on inputs like "aaaa…ab". KMP never moves backwards in the text: O(n + m).
/// </summary>
public static class StringSearch
{
    /// <summary>Every position where <paramref name="pattern"/> occurs, overlapping matches included ("aa" in "aaa" is at 0 and 1).</summary>
    public static List<int> IndexesOf(string text, string pattern)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        var fallback = Fallback(pattern);
        List<int> matches = [];
        var matched = 0; // how many pattern characters currently match

        foreach (var (i, c) in text.Index())
        {
            // On a mismatch, fall back to the longest shorter prefix that still matches, instead of starting over.
            while (matched > 0 && c != pattern[matched]) matched = fallback[matched - 1];
            if (c == pattern[matched]) matched++;

            if (matched == pattern.Length)
            {
                matches.Add(i - pattern.Length + 1);
                matched = fallback[matched - 1];
            }
        }

        return matches;
    }

    /// <summary>
    /// The "failure function": for each prefix of the pattern, the length of its longest proper prefix that is also a suffix.
    /// For "abab" that is [0, 0, 1, 2]: after matching "abab", the last "ab" can already serve as the start of the next match.
    /// </summary>
    public static int[] Fallback(string pattern)
    {
        var fallback = new int[pattern.Length];
        var length = 0;

        for (var i = 1; i < pattern.Length; i++)
        {
            while (length > 0 && pattern[i] != pattern[length]) length = fallback[length - 1];
            if (pattern[i] == pattern[length]) length++;
            fallback[i] = length;
        }

        return fallback;
    }
}
