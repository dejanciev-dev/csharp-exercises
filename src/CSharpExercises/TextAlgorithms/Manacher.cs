namespace CSharpExercises.TextAlgorithms;

/// <summary>
/// Manacher's algorithm finds the longest palindromic substring in O(n). Inserting a separator between characters
/// ("abba" → "^#a#b#b#a#$") makes every palindrome odd-length, and each centre starts from its mirror image's
/// radius inside the rightmost palindrome found so far.
/// </summary>
public static class Manacher
{
    public static string LongestPalindrome(string s)
    {
        if (s.Length == 0) return "";

        var t = new char[2 * s.Length + 3];
        t[0] = '^';
        t[^1] = '$';
        for (var i = 0; i < s.Length; i++)
        {
            t[2 * i + 1] = '#';
            t[2 * i + 2] = s[i];
        }
        t[^2] = '#';

        var radius = new int[t.Length];
        int center = 0, right = 0;
        for (var i = 1; i < t.Length - 1; i++)
        {
            if (i < right) radius[i] = Math.Min(right - i, radius[2 * center - i]);
            while (t[i + radius[i] + 1] == t[i - radius[i] - 1]) radius[i]++; // the ^ and $ sentinels stop the loop
            if (i + radius[i] > right) (center, right) = (i, i + radius[i]);
        }

        var bestCenter = 0;
        for (var i = 1; i < radius.Length; i++)
            if (radius[i] > radius[bestCenter]) bestCenter = i; // strictly greater: the first of equal lengths wins

        return s.Substring((bestCenter - radius[bestCenter]) / 2, radius[bestCenter]);
    }

    /// <summary>The quadratic "expand around each centre" version, to check the fast one against.</summary>
    public static string LongestPalindromeSimple(string s)
    {
        var (start, length) = (0, 0);
        for (var c = 0; c < 2 * s.Length - 1; c++)
        {
            int left = c / 2, right = left + c % 2;
            while (left >= 0 && right < s.Length && s[left] == s[right]) (left, right) = (left - 1, right + 1);
            if (right - left - 1 > length) (start, length) = (left + 1, right - left - 1);
        }
        return s.Substring(start, length);
    }
}
