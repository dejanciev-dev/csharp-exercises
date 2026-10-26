namespace CSharpExercises.ModernCSharp;

/// <summary>
/// <see cref="Span{T}"/> and <see cref="ReadOnlySpan{T}"/> are views over existing memory: slicing them
/// copies nothing, so text and arrays can be processed without allocating substrings or new arrays.
/// </summary>
public static class SpanExercises
{
    /// <summary>Sums a line like "10, 20,30" without allocating a string per value.</summary>
    public static int SumCsv(ReadOnlySpan<char> line)
    {
        var total = 0;
        foreach (var range in line.Split(','))
        {
            var field = line[range].Trim();
            if (!field.IsEmpty) total += int.Parse(field);
        }

        return total;
    }

    public static int CountWords(ReadOnlySpan<char> text)
    {
        var count = 0;
        var inWord = false;
        foreach (var c in text)
        {
            var isWordChar = !char.IsWhiteSpace(c);
            if (isWordChar && !inWord) count++;
            inWord = isWordChar;
        }

        return count;
    }

    /// <summary>Capitalizes the first letter of each word, writing straight into the new string's memory.</summary>
    public static string TitleCase(string text) =>
        string.Create(text.Length, text, static (span, source) =>
        {
            var startOfWord = true;
            for (var i = 0; i < source.Length; i++)
            {
                var c = source[i];
                span[i] = startOfWord ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c);
                startOfWord = char.IsWhiteSpace(c);
            }
        });

    /// <summary>Rotates items right by <paramref name="k"/> in place, using three reversals and no extra array.</summary>
    public static void RotateRight<T>(Span<T> items, int k)
    {
        if (items.Length == 0) return;

        k = ((k % items.Length) + items.Length) % items.Length;
        items.Reverse();
        items[..k].Reverse();
        items[k..].Reverse();
    }
}
