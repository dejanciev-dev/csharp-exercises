namespace CSharpExercises.TextFormatting;

/// <summary>
/// "Natural" sort order, as file managers use: "file2" before "file10", because runs of digits compare by numeric
/// value instead of character by character. Text runs compare case-insensitively; leading zeros and length break ties
/// so the order is total and stable. Numbers of any length work because digits are compared as strings, not parsed.
/// </summary>
public sealed class NaturalComparer : IComparer<string>
{
    public static NaturalComparer Instance { get; } = new();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        int i = 0, j = 0;
        while (i < x.Length && j < y.Length)
        {
            if (char.IsAsciiDigit(x[i]) && char.IsAsciiDigit(y[j]))
            {
                var (numX, nextI) = DigitRun(x, i);
                var (numY, nextJ) = DigitRun(y, j);
                var cmp = CompareNumbers(numX, numY);
                if (cmp != 0) return cmp;
                (i, j) = (nextI, nextJ);
            }
            else
            {
                var cmp = char.ToLowerInvariant(x[i]).CompareTo(char.ToLowerInvariant(y[j]));
                if (cmp != 0) return cmp;
                (i, j) = (i + 1, j + 1);
            }
        }

        var lengthOrder = (x.Length - i).CompareTo(y.Length - j);
        return lengthOrder != 0 ? lengthOrder : string.CompareOrdinal(x, y); // final tie-break: exact characters
    }

    private static (string Digits, int End) DigitRun(string s, int start)
    {
        var end = start;
        while (end < s.Length && char.IsAsciiDigit(s[end])) end++;
        return (s[start..end], end);
    }

    /// <summary>Compare digit strings numerically without parsing: strip leading zeros, then longer wins, then lexically.</summary>
    private static int CompareNumbers(string a, string b)
    {
        var (ta, tb) = (a.TrimStart('0'), b.TrimStart('0'));
        if (ta.Length != tb.Length) return ta.Length.CompareTo(tb.Length);
        var cmp = string.CompareOrdinal(ta, tb);
        return cmp != 0 ? cmp : a.Length.CompareTo(b.Length); // "007" after "7"
    }
}
