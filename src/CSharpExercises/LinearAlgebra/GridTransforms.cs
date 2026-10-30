namespace CSharpExercises.LinearAlgebra;

/// <summary>
/// Index gymnastics on 2D arrays: rotating an image in place, reading it in a spiral, and searching a matrix that
/// is sorted along both rows and columns.
/// </summary>
public static class GridTransforms
{
    /// <summary>
    /// Rotate a square grid 90° clockwise in place, without a second array: transpose, then reverse each row.
    /// </summary>
    public static void RotateClockwise<T>(T[,] grid)
    {
        var n = grid.GetLength(0);
        if (grid.GetLength(1) != n) throw new ArgumentException("Only square grids can rotate in place.", nameof(grid));

        for (var r = 0; r < n; r++)
            for (var c = r + 1; c < n; c++)
                (grid[r, c], grid[c, r]) = (grid[c, r], grid[r, c]);

        for (var r = 0; r < n; r++)
            for (var c = 0; c < n / 2; c++)
                (grid[r, c], grid[r, n - 1 - c]) = (grid[r, n - 1 - c], grid[r, c]);
    }

    /// <summary>Clockwise spiral from the top-left, shrinking four boundaries as each edge is consumed.</summary>
    public static IEnumerable<T> Spiral<T>(T[,] grid)
    {
        int top = 0, bottom = grid.GetLength(0) - 1, left = 0, right = grid.GetLength(1) - 1;
        while (top <= bottom && left <= right)
        {
            for (var c = left; c <= right; c++) yield return grid[top, c];
            top++;
            for (var r = top; r <= bottom; r++) yield return grid[r, right];
            right--;
            if (top <= bottom)
            {
                for (var c = right; c >= left; c--) yield return grid[bottom, c];
                bottom--;
            }
            if (left <= right)
            {
                for (var r = bottom; r >= top; r--) yield return grid[r, left];
                left++;
            }
        }
    }

    /// <summary>
    /// Search a matrix whose rows and columns are both sorted in O(rows + cols): start at the top-right corner;
    /// everything below is bigger and everything to the left is smaller, so each comparison discards a row or a column.
    /// </summary>
    public static (int Row, int Col)? FindInSorted(int[,] grid, int target)
    {
        int r = 0, c = grid.GetLength(1) - 1;
        while (r < grid.GetLength(0) && c >= 0)
        {
            if (grid[r, c] == target) return (r, c);
            if (grid[r, c] > target) c--;
            else r++;
        }
        return null;
    }

    /// <summary>Zeroes the row and column of every zero cell. Find them all first, or the new zeros would spread.</summary>
    public static void ZeroRowsAndColumns(int[,] grid)
    {
        var zeroRows = new HashSet<int>();
        var zeroCols = new HashSet<int>();
        for (var r = 0; r < grid.GetLength(0); r++)
            for (var c = 0; c < grid.GetLength(1); c++)
                if (grid[r, c] == 0) { zeroRows.Add(r); zeroCols.Add(c); }

        for (var r = 0; r < grid.GetLength(0); r++)
            for (var c = 0; c < grid.GetLength(1); c++)
                if (zeroRows.Contains(r) || zeroCols.Contains(c)) grid[r, c] = 0;
    }
}
