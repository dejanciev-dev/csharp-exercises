using System.Numerics;

namespace CSharpExercises.Algorithms;

/// <summary>
/// Grid DP where you may only move right or down. Counting routes: each cell's count is the sum from above and from the
/// left (blocked cells contribute nothing). Cheapest route: each cell's cost plus the cheaper neighbour. Collecting the
/// most along the way works the same with max.
/// </summary>
public static class GridPaths
{
    /// <param name="grid">'#' marks a blocked cell.</param>
    public static BigInteger CountPaths(IReadOnlyList<string> grid)
    {
        var (rows, cols) = (grid.Count, grid[0].Length);
        var ways = new BigInteger[cols]; // one row at a time is enough
        for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++)
            {
                if (grid[r][c] == '#') ways[c] = 0;
                else if (r == 0 && c == 0) ways[c] = 1;
                else if (c > 0) ways[c] += ways[c - 1]; // ways[c] still holds the value from the row above
            }
        return ways[cols - 1];
    }

    public static (int Cost, string Moves) CheapestPath(int[,] cost)
    {
        var (rows, cols) = (cost.GetLength(0), cost.GetLength(1));
        var best = new int[rows, cols];
        for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++)
            {
                var fromAbove = r > 0 ? best[r - 1, c] : int.MaxValue;
                var fromLeft = c > 0 ? best[r, c - 1] : int.MaxValue;
                best[r, c] = cost[r, c] + (r == 0 && c == 0 ? 0 : Math.Min(fromAbove, fromLeft));
            }

        var moves = new System.Text.StringBuilder();
        for (int r = rows - 1, c = cols - 1; r > 0 || c > 0;)
        {
            if (r > 0 && (c == 0 || best[r - 1, c] <= best[r, c - 1])) { moves.Insert(0, 'D'); r--; }
            else { moves.Insert(0, 'R'); c--; }
        }
        return (best[rows - 1, cols - 1], moves.ToString());
    }
}
