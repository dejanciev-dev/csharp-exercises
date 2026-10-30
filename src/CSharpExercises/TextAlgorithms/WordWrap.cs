namespace CSharpExercises.TextAlgorithms;

/// <summary>
/// Breaking a paragraph into lines. Greedy (fill each line as much as possible) is what most editors do; the
/// "minimum raggedness" version is the dynamic programming idea behind TeX, minimising the sum of squared leftover
/// space so no line ends up conspicuously short.
/// </summary>
public static class WordWrap
{
    public static IReadOnlyList<string> Greedy(string text, int width)
    {
        var lines = new List<string>();
        var line = new System.Text.StringBuilder();
        foreach (var word in Words(text, width))
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > width)
            {
                lines.Add(line.ToString());
                line.Clear();
            }
            if (line.Length > 0) line.Append(' ');
            line.Append(word);
        }
        if (line.Length > 0) lines.Add(line.ToString());
        return lines;
    }

    public static IReadOnlyList<string> Balanced(string text, int width)
    {
        var words = Words(text, width);
        var n = words.Length;

        // cost[i]: best total for words[i..]; the last line is free, since it is allowed to be short.
        var cost = new long[n + 1];
        var breakAfter = new int[n];
        for (var i = n - 1; i >= 0; i--)
        {
            cost[i] = long.MaxValue;
            var length = -1;
            for (var j = i; j < n; j++)
            {
                length += words[j].Length + 1;
                if (length > width) break;
                var slack = width - length;
                var lineCost = j == n - 1 ? 0 : (long)slack * slack;
                if (lineCost + cost[j + 1] < cost[i]) (cost[i], breakAfter[i]) = (lineCost + cost[j + 1], j);
            }
        }

        var lines = new List<string>();
        for (var i = 0; i < n; i = breakAfter[i] + 1) lines.Add(string.Join(' ', words[i..(breakAfter[i] + 1)]));
        return lines;
    }

    /// <summary>Raggedness score of a wrapped paragraph, to compare the two strategies.</summary>
    public static long Raggedness(IReadOnlyList<string> lines, int width) =>
        lines.SkipLast(1).Sum(l => (long)(width - l.Length) * (width - l.Length));

    private static string[] Words(string text, int width)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.FirstOrDefault(w => w.Length > width) is { } tooLong)
            throw new ArgumentException($"The word '{tooLong}' is longer than the line width {width}.", nameof(text));
        return words;
    }
}
