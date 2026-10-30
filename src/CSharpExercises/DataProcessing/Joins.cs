namespace CSharpExercises.DataProcessing;

/// <summary>
/// The three join algorithms a database chooses between. Nested loop compares every pair: O(n·m), but fine when one
/// side is tiny. Hash join builds a hash table on the smaller side and probes it with the other: O(n + m), the
/// default for equality joins. Merge join walks two inputs already sorted by the key in step: O(n + m) with no extra
/// memory. All three must return the same rows, including every combination when a key repeats on both sides.
/// </summary>
public static class Joins
{
    public static IEnumerable<(TLeft Left, TRight Right)> NestedLoop<TLeft, TRight, TKey>(
        IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TKey> leftKey, Func<TRight, TKey> rightKey)
    {
        var rightList = right.ToList(); // the inner side is scanned once per outer row
        var comparer = EqualityComparer<TKey>.Default;
        foreach (var l in left)
            foreach (var r in rightList)
                if (comparer.Equals(leftKey(l), rightKey(r))) yield return (l, r);
    }

    public static IEnumerable<(TLeft Left, TRight Right)> Hash<TLeft, TRight, TKey>(
        IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TKey> leftKey, Func<TRight, TKey> rightKey) where TKey : notnull
    {
        var table = new Dictionary<TKey, List<TRight>>();
        foreach (var r in right)
        {
            if (!table.TryGetValue(rightKey(r), out var bucket)) table[rightKey(r)] = bucket = [];
            bucket.Add(r);
        }
        foreach (var l in left)
            if (table.TryGetValue(leftKey(l), out var matches))
                foreach (var r in matches) yield return (l, r);
    }

    /// <summary>Both inputs must be sorted by key. Equal-key runs on both sides produce their cross product.</summary>
    public static IEnumerable<(TLeft Left, TRight Right)> Merge<TLeft, TRight, TKey>(
        IReadOnlyList<TLeft> left, IReadOnlyList<TRight> right, Func<TLeft, TKey> leftKey, Func<TRight, TKey> rightKey)
        where TKey : IComparable<TKey>
    {
        int i = 0, j = 0;
        while (i < left.Count && j < right.Count)
        {
            var cmp = leftKey(left[i]).CompareTo(rightKey(right[j]));
            if (cmp < 0) { i++; continue; }
            if (cmp > 0) { j++; continue; }

            var key = leftKey(left[i]);
            var runEnd = j;
            while (runEnd < right.Count && rightKey(right[runEnd]).CompareTo(key) == 0) runEnd++;
            for (; i < left.Count && leftKey(left[i]).CompareTo(key) == 0; i++)
                for (var k = j; k < runEnd; k++) yield return (left[i], right[k]);
            j = runEnd;
        }
    }

    /// <summary>A left outer join: every left row, with default when nothing matches (SQL's NULL).</summary>
    public static IEnumerable<(TLeft Left, TRight? Right)> LeftOuter<TLeft, TRight, TKey>(
        IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TKey> leftKey, Func<TRight, TKey> rightKey) where TKey : notnull
    {
        var lookup = right.ToLookup(rightKey);
        foreach (var l in left)
        {
            var matches = lookup[leftKey(l)];
            if (!matches.Any()) yield return (l, default);
            foreach (var r in matches) yield return (l, r);
        }
    }
}
