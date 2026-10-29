namespace CSharpExercises.Diffing;

public enum EditKind { Equal, Delete, Insert }

/// <summary>
/// One step of an edit script. <paramref name="OldIndex"/> and <paramref name="NewIndex"/> are 0-based positions in the
/// two sequences: for a delete, NewIndex is where the new sequence continues; for an insert, OldIndex is the old item
/// it goes before.
/// </summary>
public readonly record struct Edit<T>(EditKind Kind, T Value, int OldIndex, int NewIndex);

/// <summary>
/// Eugene Myers' O((N+M)·D) diff, the algorithm behind git diff: the shortest edit script turning one sequence into
/// another, where D is the number of inserted and deleted items. It is fast when the sequences are similar, which is
/// the usual case for files under version control.
/// </summary>
/// <remarks>
/// Think of a grid with the old sequence along x and the new one along y. Moving right deletes an old item, moving
/// down inserts a new one, and a diagonal (a "snake") is free when the items are equal. Round d finds, for each
/// diagonal k = x - y, the furthest point reachable with d edits. This version keeps a copy of that frontier per round
/// to walk the path back, which costs O((N+M)·D) memory; the linear-space variant splits the problem in the middle.
/// </remarks>
public static class MyersDiff
{
    public static IReadOnlyList<Edit<T>> Diff<T>(IReadOnlyList<T> oldItems, IReadOnlyList<T> newItems, IEqualityComparer<T>? comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;
        int n = oldItems.Count, m = newItems.Count, max = n + m;

        // furthest[max + k] is the furthest x reached on diagonal k. Starting with x = 0 on diagonal 1 makes the first
        // round begin at (0, 0) without a special case.
        var furthest = new int[2 * max + 2];
        var trace = new List<int[]>();

        for (var d = 0; d <= max; d++)
        {
            trace.Add((int[])furthest.Clone());
            for (var k = -d; k <= d; k += 2)
            {
                // Step down from diagonal k + 1 (an insert) or right from k - 1 (a delete), whichever got further.
                var x = MovesDown(furthest, max, k, d) ? furthest[max + k + 1] : furthest[max + k - 1] + 1;
                var y = x - k;
                while (x < n && y < m && comparer.Equals(oldItems[x], newItems[y]))
                {
                    x++;
                    y++;
                }
                furthest[max + k] = x;

                if (x >= n && y >= m) return Backtrack(trace, oldItems, newItems, max);
            }
        }
        throw new InvalidOperationException("Unreachable: d = n + m always reaches the end.");
    }

    private static bool MovesDown(int[] furthest, int max, int k, int d) =>
        k == -d || (k != d && furthest[max + k - 1] < furthest[max + k + 1]);

    /// <summary>Walks from the end back to (0, 0), round by round, recording the snake and the one edit of each round.</summary>
    private static List<Edit<T>> Backtrack<T>(List<int[]> trace, IReadOnlyList<T> oldItems, IReadOnlyList<T> newItems, int max)
    {
        var edits = new List<Edit<T>>();
        int x = oldItems.Count, y = newItems.Count;

        for (var d = trace.Count - 1; d >= 0; d--)
        {
            var furthest = trace[d];
            var k = x - y;
            var previousK = MovesDown(furthest, max, k, d) ? k + 1 : k - 1;
            var previousX = furthest[max + previousK];
            var previousY = previousX - previousK;

            while (x > previousX && y > previousY)
            {
                x--;
                y--;
                edits.Add(new(EditKind.Equal, oldItems[x], x, y));
            }

            if (d > 0)
            {
                edits.Add(x == previousX
                    ? new(EditKind.Insert, newItems[previousY], previousX, previousY)
                    : new(EditKind.Delete, oldItems[previousX], previousX, previousY));
            }
            (x, y) = (previousX, previousY);
        }

        edits.Reverse();
        return edits;
    }
}
