namespace CSharpExercises.Compression;

/// <summary>
/// The Burrows–Wheeler transform (the first stage of bzip2) doesn't compress anything by itself: it reorders the text so
/// that characters with similar contexts end up next to each other, creating long runs that later stages compress well.
/// Sort all rotations of the text, keep the last column and the row where the original sits; the inverse follows the
/// "last-to-first" mapping back through the text.
/// </summary>
public static class BurrowsWheeler
{
    public static (string Last, int Index) Transform(string text)
    {
        if (text.Length == 0) return ("", 0);
        var n = text.Length;
        var rotations = Enumerable.Range(0, n).ToArray();
        Array.Sort(rotations, (a, b) =>
        {
            for (var k = 0; k < n; k++)
            {
                var cmp = text[(a + k) % n].CompareTo(text[(b + k) % n]);
                if (cmp != 0) return cmp;
            }
            return 0;
        });

        var last = new string(rotations.Select(r => text[(r + n - 1) % n]).ToArray());
        return (last, Array.IndexOf(rotations, 0));
    }

    public static string Inverse(string last, int index)
    {
        var n = last.Length;
        if (n == 0) return "";

        // The first column is the last column sorted. The k-th occurrence of a character in the last column is the
        // k-th occurrence in the first column, and stepping between them walks the text backwards.
        var order = Enumerable.Range(0, n).OrderBy(i => last[i]).ThenBy(i => i).ToArray(); // stable sort
        var result = new char[n];
        var row = order[index];
        for (var i = 0; i < n; i++)
        {
            result[i] = last[row];
            row = order[row];
        }
        return new string(result);
    }

    /// <summary>Number of runs of equal characters: lower means more compressible.</summary>
    public static int Runs(string s) => s.Length == 0 ? 0 : 1 + Enumerable.Range(1, s.Length - 1).Count(i => s[i] != s[i - 1]);
}
