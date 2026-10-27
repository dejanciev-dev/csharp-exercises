namespace CSharpExercises.Algorithms;

/// <summary>
/// Backtracking: build a solution one choice at a time, and undo the last choice as soon as it cannot lead anywhere.
/// Every method here follows the same shape: choose, recurse, un-choose.
/// </summary>
public static class Backtracking
{
    /// <summary>Every ordering of <paramref name="items"/>. Duplicates in the input give each distinct ordering once.</summary>
    public static List<T[]> Permutations<T>(IReadOnlyList<T> items)
    {
        // Sorting puts equal items next to each other, so duplicates can be skipped with one comparison.
        var sorted = items.Order().ToArray();
        var used = new bool[sorted.Length];
        var current = new List<T>(sorted.Length);
        List<T[]> results = [];

        void Build()
        {
            if (current.Count == sorted.Length)
            {
                results.Add([.. current]);
                return;
            }

            for (var i = 0; i < sorted.Length; i++)
            {
                if (used[i]) continue;
                // Only use the 2nd copy of a value once the 1st copy is already placed: otherwise [a1, a2] and [a2, a1] both appear.
                if (i > 0 && EqualityComparer<T>.Default.Equals(sorted[i], sorted[i - 1]) && !used[i - 1]) continue;

                used[i] = true;
                current.Add(sorted[i]);      // choose
                Build();                     // explore
                current.RemoveAt(current.Count - 1);
                used[i] = false;             // un-choose
            }
        }

        Build();
        return results;
    }

    /// <summary>All 2^n subsets of <paramref name="items"/>, from the empty set upwards.</summary>
    public static List<T[]> Subsets<T>(IReadOnlyList<T> items)
    {
        var current = new List<T>();
        List<T[]> results = [];

        void Build(int start)
        {
            results.Add([.. current]);
            for (var i = start; i < items.Count; i++)
            {
                current.Add(items[i]);
                Build(i + 1);
                current.RemoveAt(current.Count - 1);
            }
        }

        Build(0);
        return results;
    }

    /// <summary>
    /// Every combination of <paramref name="candidates"/> adding up to <paramref name="target"/>, each candidate usable
    /// any number of times. Sorting first lets the search stop as soon as a candidate is too large: that is the pruning.
    /// </summary>
    public static List<int[]> CombinationSum(IEnumerable<int> candidates, int target)
    {
        var sorted = candidates.Where(c => c > 0).Distinct().Order().ToArray();
        var current = new List<int>();
        List<int[]> results = [];

        void Build(int start, int remaining)
        {
            if (remaining == 0)
            {
                results.Add([.. current]);
                return;
            }

            for (var i = start; i < sorted.Length && sorted[i] <= remaining; i++)
            {
                current.Add(sorted[i]);
                Build(i, remaining - sorted[i]); // i, not i + 1: the same candidate may be used again
                current.RemoveAt(current.Count - 1);
            }
        }

        Build(0, target);
        return results;
    }

    /// <summary>
    /// Places <paramref name="n"/> queens on an n×n board so none attack each other. Each solution lists the column
    /// of the queen in every row. Columns and both diagonals are tracked in sets, so checking a square is O(1).
    /// </summary>
    public static List<int[]> NQueens(int n)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(n);

        var queens = new int[n];
        var columns = new bool[n];
        var diagonals = new bool[2 * n - 1];     // row + column is the same along a "/" diagonal
        var antiDiagonals = new bool[2 * n - 1]; // row - column (+ n - 1) is the same along a "\" diagonal
        List<int[]> solutions = [];

        void Place(int row)
        {
            if (row == n)
            {
                solutions.Add([.. queens]);
                return;
            }

            for (var column = 0; column < n; column++)
            {
                var d = row + column;
                var a = row - column + n - 1;
                if (columns[column] || diagonals[d] || antiDiagonals[a]) continue;

                queens[row] = column;
                columns[column] = diagonals[d] = antiDiagonals[a] = true;
                Place(row + 1);
                columns[column] = diagonals[d] = antiDiagonals[a] = false;
            }
        }

        Place(0);
        return solutions;
    }

    /// <summary>Draws a solution, e.g. <c>".Q..\n...Q\nQ...\n..Q."</c>.</summary>
    public static string Draw(int[] queens) =>
        string.Join('\n', queens.Select(column => new string('.', column) + 'Q' + new string('.', queens.Length - column - 1)));
}
