namespace CSharpExercises.TextFormatting;

/// <summary>
/// Wildcard matching as in shells and .gitignore: '?' matches one character, '*' any run within a path segment,
/// '**' any number of segments, and [abc] / [a-z] character classes. Implemented with the classic two-pointer
/// backtracking that remembers only the most recent star (and the most recent globstar), which runs in polynomial time
/// instead of the exponential time of naive recursion.
/// </summary>
public static class Glob
{
    public static bool IsMatch(string pattern, string text)
    {
        int p = 0, t = 0;
        int starP = -1, starT = -1;          // last '*': may swallow characters within one segment
        int globP = -1, globT = -1;          // last '**': may swallow whole segments
        var globNeedsSlash = false;          // "**/" resumes only at the start of a segment

        while (t < text.Length)
        {
            if (p < pattern.Length && pattern[p] == '*')
            {
                if (p + 1 < pattern.Length && pattern[p + 1] == '*')
                {
                    p += 2;
                    globNeedsSlash = p < pattern.Length && pattern[p] == '/';
                    if (globNeedsSlash) p++; // "**/" may match zero folders
                    (globP, globT, starP) = (p, t, -1);
                }
                else
                {
                    p++;
                    (starP, starT) = (p, t);
                }
                continue;
            }
            if (p < pattern.Length && MatchesOne(pattern, ref p, text[t], out var consumed))
            {
                p = consumed;
                t++;
                continue;
            }
            // Mismatch: first let the last '*' swallow one more character (never a '/')…
            if (starP >= 0 && text[starT] != '/')
            {
                starT++;
                (p, t) = (starP, starT);
                continue;
            }
            // …otherwise let the last '**' swallow more: a whole segment for "**/", any character for a trailing "**".
            if (globP >= 0)
            {
                globT = globNeedsSlash ? text.IndexOf('/', globT) + 1 : globT + 1;
                if (globT <= 0) return false;
                (p, t, starP) = (globP, globT, -1);
                continue;
            }
            return false;
        }

        while (p < pattern.Length && pattern[p] == '*') p++;
        return p == pattern.Length;
    }

    public static IEnumerable<string> Filter(string pattern, IEnumerable<string> paths) => paths.Where(path => IsMatch(pattern, path));

    private static bool MatchesOne(string pattern, ref int p, char c, out int next)
    {
        next = p + 1;
        switch (pattern[p])
        {
            case '?':
                return c != '/';
            case '[':
                var close = pattern.IndexOf(']', p + 2);
                if (close < 0) return pattern[p] == c; // a lone '[' is literal
                var set = pattern[(p + 1)..close];
                var negate = set.StartsWith('!');
                if (negate) set = set[1..];
                var hit = false;
                for (var i = 0; i < set.Length; i++)
                {
                    if (i + 2 < set.Length && set[i + 1] == '-') { hit |= c >= set[i] && c <= set[i + 2]; i += 2; }
                    else hit |= c == set[i];
                }
                next = close + 1;
                return hit != negate;
            case '\\' when p + 1 < pattern.Length:
                next = p + 2;
                return pattern[p + 1] == c;
            default:
                return pattern[p] == c;
        }
    }
}
