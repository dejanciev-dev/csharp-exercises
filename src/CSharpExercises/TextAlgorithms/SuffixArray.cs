namespace CSharpExercises.TextAlgorithms;

/// <summary>
/// A suffix array lists the starting positions of all suffixes in sorted order. Built by prefix doubling in
/// O(n log² n): sort by the first 1, 2, 4, 8… characters, each round using the ranks of the previous one. With the
/// LCP array (Kasai's algorithm, O(n)), it answers substring search, repeats and distinct-substring counts.
/// </summary>
public sealed class SuffixArray
{
    public SuffixArray(string text)
    {
        Text = text;
        Suffixes = Build(text);
        Lcp = BuildLcp(text, Suffixes);
    }

    public string Text { get; }
    public int[] Suffixes { get; }

    /// <summary>Lcp[i]: length of the common prefix of the suffixes at Suffixes[i - 1] and Suffixes[i].</summary>
    public int[] Lcp { get; }

    /// <summary>All occurrences via binary search: matching suffixes form one contiguous block of the array.</summary>
    public IReadOnlyList<int> FindAll(string pattern)
    {
        var from = LowerBound(pattern, matchPrefix: false);
        var to = LowerBound(pattern, matchPrefix: true);
        return Suffixes[from..to].Order().ToArray();
    }

    /// <summary>The longest repeated substring is the largest LCP between neighbouring suffixes.</summary>
    public string LongestRepeatedSubstring()
    {
        var best = 0;
        for (var i = 1; i < Lcp.Length; i++)
            if (Lcp[i] > Lcp[best]) best = i;
        return Lcp.Length == 0 ? "" : Text.Substring(Suffixes[best], Lcp[best]);
    }

    /// <summary>Every suffix contributes its length minus the prefix it shares with the previous suffix.</summary>
    public long CountDistinctSubstrings()
    {
        long total = 0;
        for (var i = 0; i < Suffixes.Length; i++) total += Text.Length - Suffixes[i] - Lcp[i];
        return total;
    }

    private int LowerBound(string pattern, bool matchPrefix)
    {
        int low = 0, high = Suffixes.Length;
        while (low < high)
        {
            var mid = (low + high) / 2;
            var suffix = Text.AsSpan(Suffixes[mid]);
            var prefix = suffix[..Math.Min(pattern.Length, suffix.Length)];
            var cmp = prefix.CompareTo(pattern, StringComparison.Ordinal);
            if (cmp < 0 || (matchPrefix && cmp == 0 && prefix.Length == pattern.Length)) low = mid + 1;
            else high = mid;
        }
        return low;
    }

    private static int[] Build(string text)
    {
        var n = text.Length;
        var sa = Enumerable.Range(0, n).ToArray();
        var rank = text.Select(c => (int)c).ToArray();
        var next = new int[n];

        for (var k = 1; ; k *= 2)
        {
            var rk = rank;
            var step = k;
            (int, int) Key(int i) => (rk[i], i + step < n ? rk[i + step] : -1);

            Array.Sort(sa, (a, b) => Key(a).CompareTo(Key(b)));

            next[sa[0]] = 0;
            for (var i = 1; i < n; i++) next[sa[i]] = next[sa[i - 1]] + (Key(sa[i - 1]) == Key(sa[i]) ? 0 : 1);
            (rank, next) = (next, rank);

            if (n == 0 || rank[sa[n - 1]] == n - 1) return sa; // all ranks distinct: fully sorted
        }
    }

    /// <summary>Kasai: the LCP drops by at most one when moving from suffix i to suffix i + 1 in text order.</summary>
    private static int[] BuildLcp(string text, int[] sa)
    {
        var n = text.Length;
        var position = new int[n];
        for (var i = 0; i < n; i++) position[sa[i]] = i;

        var lcp = new int[n];
        for (int i = 0, h = 0; i < n; i++)
        {
            if (position[i] == 0) { h = 0; continue; }
            var j = sa[position[i] - 1];
            while (i + h < n && j + h < n && text[i + h] == text[j + h]) h++;
            lcp[position[i]] = h;
            if (h > 0) h--;
        }
        return lcp;
    }
}
