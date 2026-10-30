namespace CSharpExercises.Algorithms;

/// <summary>
/// Matching with '.' (any character) and 'x*' (zero or more of the preceding element), the core of regular expressions,
/// as a DP table over (text prefix, pattern prefix). Each cell asks: does text[..i] match pattern[..j]? A star either
/// matches nothing (skip "x*") or one more character (if it fits, stay on "x*"). The same table approach matches
/// SQL LIKE patterns with '%' and '_'.
/// </summary>
public static class PatternMatchingDp
{
    public static bool RegexMatch(string text, string pattern)
    {
        var match = new bool[text.Length + 1, pattern.Length + 1];
        match[0, 0] = true;
        for (var j = 2; j <= pattern.Length; j++)
            match[0, j] = pattern[j - 1] == '*' && match[0, j - 2]; // "a*b*" matches the empty string

        for (var i = 1; i <= text.Length; i++)
            for (var j = 1; j <= pattern.Length; j++)
            {
                if (pattern[j - 1] == '*')
                {
                    if (j < 2) throw new FormatException("'*' needs something before it.");
                    var zero = match[i, j - 2];
                    var more = Fits(text[i - 1], pattern[j - 2]) && match[i - 1, j];
                    match[i, j] = zero || more;
                }
                else
                {
                    match[i, j] = Fits(text[i - 1], pattern[j - 1]) && match[i - 1, j - 1];
                }
            }
        return match[text.Length, pattern.Length];
    }

    /// <summary>SQL LIKE: '%' matches any run of characters, '_' exactly one. Case-insensitive like most collations.</summary>
    public static bool Like(string text, string pattern)
    {
        var match = new bool[text.Length + 1, pattern.Length + 1];
        match[0, 0] = true;
        for (var j = 1; j <= pattern.Length && pattern[j - 1] == '%'; j++) match[0, j] = true;

        for (var i = 1; i <= text.Length; i++)
            for (var j = 1; j <= pattern.Length; j++)
                match[i, j] = pattern[j - 1] switch
                {
                    '%' => match[i, j - 1] || match[i - 1, j],
                    '_' => match[i - 1, j - 1],
                    var c => char.ToLowerInvariant(c) == char.ToLowerInvariant(text[i - 1]) && match[i - 1, j - 1],
                };
        return match[text.Length, pattern.Length];
    }

    private static bool Fits(char c, char p) => p == '.' || p == c;
}
