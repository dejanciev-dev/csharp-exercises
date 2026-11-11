namespace CSharpExercises.Bioinformatics;

public sealed record Alignment(string Top, string Bottom, int Score);

/// <summary>
/// Aligning two sequences means inserting gaps so similar parts line up. Needleman–Wunsch fills a table where each
/// cell is the best score for two prefixes, coming from a match/mismatch diagonal or a gap in either sequence, then
/// traces back from the corner. Smith–Waterman is the same table with one change, never going below zero, so it
/// finds the best-matching stretch anywhere (a local alignment) and traces back from the highest cell to a zero.
/// </summary>
public static class SequenceAlignment
{
    public static Alignment Global(string a, string b, int match = 1, int mismatch = -1, int gap = -1) =>
        Align(a, b, match, mismatch, gap, local: false);

    public static Alignment Local(string a, string b, int match = 3, int mismatch = -3, int gap = -2) =>
        Align(a, b, match, mismatch, gap, local: true);

    /// <summary>Re-scores an existing alignment column by column.</summary>
    public static int Score(string top, string bottom, int match, int mismatch, int gap) =>
        top.Zip(bottom).Sum(p => p.First == '-' || p.Second == '-' ? gap : p.First == p.Second ? match : mismatch);

    private static Alignment Align(string a, string b, int match, int mismatch, int gap, bool local)
    {
        var score = new int[a.Length + 1, b.Length + 1];
        for (var i = 1; i <= a.Length; i++) score[i, 0] = local ? 0 : i * gap;
        for (var j = 1; j <= b.Length; j++) score[0, j] = local ? 0 : j * gap;

        var (bestI, bestJ) = (a.Length, b.Length);
        for (var i = 1; i <= a.Length; i++)
            for (var j = 1; j <= b.Length; j++)
            {
                var cell = Math.Max(score[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? match : mismatch),
                           Math.Max(score[i - 1, j] + gap, score[i, j - 1] + gap));
                score[i, j] = local ? Math.Max(0, cell) : cell;
                if (local && score[i, j] > score[bestI, bestJ]) (bestI, bestJ) = (i, j);
            }
        if (local && score[bestI, bestJ] == 0) return new Alignment("", "", 0);

        var top = new List<char>();
        var bottom = new List<char>();
        int x = bestI, y = bestJ;
        while (x > 0 || y > 0)
        {
            if (local && score[x, y] == 0) break;
            if (x > 0 && y > 0 && score[x, y] == score[x - 1, y - 1] + (a[x - 1] == b[y - 1] ? match : mismatch))
            {
                top.Add(a[--x]);
                bottom.Add(b[--y]);
            }
            else if (x > 0 && score[x, y] == score[x - 1, y] + gap)
            {
                top.Add(a[--x]);
                bottom.Add('-');
            }
            else
            {
                top.Add('-');
                bottom.Add(b[--y]);
            }
        }
        top.Reverse();
        bottom.Reverse();
        return new Alignment(new string([.. top]), new string([.. bottom]), score[bestI, bestJ]);
    }
}
