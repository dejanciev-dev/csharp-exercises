namespace CSharpExercises.Algorithms;

/// <summary>
/// Palindrome problems solved with interval DP. The longest palindromic subsequence (characters need not be adjacent)
/// equals the longest common subsequence of a string and its reverse; the fewest insertions to make a palindrome is the
/// length minus that. The fewest cuts to split a string into palindromes uses a table of which substrings are
/// palindromes.
/// </summary>
public static class PalindromeDp
{
    public static string LongestSubsequence(string s)
    {
        var n = s.Length;
        if (n == 0) return "";
        var len = new int[n, n]; // len[i, j]: longest palindromic subsequence of s[i..j]
        for (var i = n - 1; i >= 0; i--)
        {
            len[i, i] = 1;
            for (var j = i + 1; j < n; j++)
                len[i, j] = s[i] == s[j] ? len[i + 1, j - 1] + 2 : Math.Max(len[i + 1, j], len[i, j - 1]);
        }

        // Rebuild from the outside in.
        var left = new System.Text.StringBuilder();
        int a = 0, b = n - 1;
        var middle = "";
        while (a <= b)
        {
            if (a == b) { middle = s[a].ToString(); break; }
            if (s[a] == s[b]) { left.Append(s[a]); a++; b--; }
            else if (len[a + 1, b] >= len[a, b - 1]) a++;
            else b--;
        }
        var l = left.ToString();
        return l + middle + new string(l.Reverse().ToArray());
    }

    public static int MinInsertionsToPalindrome(string s) => s.Length - LongestSubsequence(s).Length;

    /// <summary>Fewest cuts so every piece is a palindrome, and the pieces.</summary>
    public static IReadOnlyList<string> MinCutPartition(string s)
    {
        var n = s.Length;
        if (n == 0) return [];
        var isPal = new bool[n, n];
        for (var i = n - 1; i >= 0; i--)
            for (var j = i; j < n; j++)
                isPal[i, j] = s[i] == s[j] && (j - i < 2 || isPal[i + 1, j - 1]);

        var cuts = new int[n];
        var startOfLast = new int[n];
        for (var j = 0; j < n; j++)
        {
            cuts[j] = int.MaxValue;
            for (var i = 0; i <= j; i++)
            {
                if (!isPal[i, j]) continue;
                var candidate = i == 0 ? 0 : cuts[i - 1] + 1;
                if (candidate < cuts[j]) (cuts[j], startOfLast[j]) = (candidate, i);
            }
        }

        var pieces = new List<string>();
        for (var end = n - 1; end >= 0; end = startOfLast[end] - 1) pieces.Add(s[startOfLast[end]..(end + 1)]);
        pieces.Reverse();
        return pieces;
    }
}
