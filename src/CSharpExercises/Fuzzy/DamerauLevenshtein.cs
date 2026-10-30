namespace CSharpExercises.Fuzzy;

/// <summary>
/// Edit distance that also counts swapping two neighbouring characters as one edit ("teh" → "the"), the most common
/// typing error, which plain Levenshtein charges as two. This is the "optimal string alignment" variant. Also a
/// bounded version that gives up early once the distance must exceed a limit, which is what makes fuzzy search fast.
/// </summary>
public static class DamerauLevenshtein
{
    public static int Distance(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;

        for (var i = 1; i <= a.Length; i++)
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1); // transposition
            }
        return d[a.Length, b.Length];
    }

    /// <summary>
    /// Returns the distance if it is at most <paramref name="limit"/>, otherwise null. Only a diagonal band of width
    /// 2·limit + 1 can hold small distances, and once whole rows exceed the limit, so will the answer.
    /// </summary>
    public static int? DistanceWithin(string a, string b, int limit)
    {
        if (Math.Abs(a.Length - b.Length) > limit) return null;
        const int Far = int.MaxValue / 2;
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++)
            for (var j = 0; j <= b.Length; j++) d[i, j] = Far;
        for (var i = 0; i <= Math.Min(a.Length, limit); i++) d[i, 0] = i;
        for (var j = 0; j <= Math.Min(b.Length, limit); j++) d[0, j] = j;

        var previousRowMin = 0;
        for (var i = 1; i <= a.Length; i++)
        {
            var rowMin = d[i, 0];
            for (int j = Math.Max(1, i - limit), end = Math.Min(b.Length, i + limit); j <= end; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                var value = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1]) value = Math.Min(value, d[i - 2, j - 2] + 1);
                d[i, j] = value;
                rowMin = Math.Min(rowMin, value);
            }
            // A transposition jumps from row i - 2 to row i, so only two hopeless rows in a row prove the answer is too big.
            if (rowMin > limit && previousRowMin > limit) return null;
            previousRowMin = rowMin;
        }
        return d[a.Length, b.Length] <= limit ? d[a.Length, b.Length] : null;
    }
}
