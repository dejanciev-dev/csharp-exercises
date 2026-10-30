namespace CSharpExercises.TextAlgorithms;

/// <summary>
/// The Z-array: z[i] is the length of the longest substring starting at i that is also a prefix of the string.
/// Computed in O(n) by reusing the rightmost match found so far (the "Z-box").
/// </summary>
public static class ZFunction
{
    public static int[] Compute(string s)
    {
        var z = new int[s.Length];
        if (s.Length == 0) return z;
        z[0] = s.Length;

        for (int i = 1, left = 0, right = 0; i < s.Length; i++)
        {
            // Inside the Z-box, s[i..] starts like s[i - left..], so start from what we already know.
            if (i < right) z[i] = Math.Min(right - i, z[i - left]);
            while (i + z[i] < s.Length && s[z[i]] == s[i + z[i]]) z[i]++;
            if (i + z[i] > right) (left, right) = (i, i + z[i]);
        }
        return z;
    }

    /// <summary>Search by computing Z of pattern + separator + text: every z equal to the pattern length is a match.</summary>
    public static IEnumerable<int> FindAll(string text, string pattern)
    {
        if (pattern.Length == 0) yield break;
        var z = Compute(pattern + '\0' + text);
        for (var i = pattern.Length + 1; i < z.Length; i++)
            if (z[i] >= pattern.Length) yield return i - pattern.Length - 1;
    }

    /// <summary>The shortest unit that repeats to build the string, e.g. "abab" → "ab", "abc" → "abc".</summary>
    public static string SmallestPeriod(string s)
    {
        var z = Compute(s);
        for (var p = 1; p < s.Length; p++)
            if (s.Length % p == 0 && p + z[p] == s.Length) return s[..p];
        return s;
    }
}
