namespace CSharpExercises.Puzzles;

/// <summary>
/// A knight's tour visits every square of the board exactly once. Plain backtracking explodes on an 8×8 board, but
/// Warnsdorff's heuristic (always jump to the square with the fewest onward moves, so dead ends get used up early)
/// finds a tour almost always without backtracking at all. A small amount of backtracking covers the rare failures.
/// </summary>
public static class KnightsTour
{
    private static readonly (int Dr, int Dc)[] Jumps = [(2, 1), (1, 2), (-1, 2), (-2, 1), (-2, -1), (-1, -2), (1, -2), (2, -1)];

    /// <returns>board[r, c] = the move number (1-based) on which the square is visited, or null if no tour exists.</returns>
    public static int[,]? Find(int size, int startRow = 0, int startCol = 0)
    {
        var board = new int[size, size];
        board[startRow, startCol] = 1;
        return Extend(board, size, startRow, startCol, 1) ? board : null;
    }

    private static bool Extend(int[,] board, int n, int row, int col, int move)
    {
        if (move == n * n) return true;

        var candidates = Moves(board, n, row, col)
            .OrderBy(m => Moves(board, n, m.Row, m.Col).Count()) // Warnsdorff: fewest onward moves first
            .ToList();

        foreach (var (r, c) in candidates)
        {
            board[r, c] = move + 1;
            if (Extend(board, n, r, c, move + 1)) return true;
            board[r, c] = 0; // backtrack
        }
        return false;
    }

    private static IEnumerable<(int Row, int Col)> Moves(int[,] board, int n, int row, int col) =>
        Jumps.Select(j => (Row: row + j.Dr, Col: col + j.Dc))
             .Where(p => p.Row >= 0 && p.Row < n && p.Col >= 0 && p.Col < n && board[p.Row, p.Col] == 0);

    /// <summary>Every square visited once, and consecutive numbers a knight's move apart.</summary>
    public static bool IsValidTour(int[,] board)
    {
        var n = board.GetLength(0);
        var positions = new (int, int)[n * n + 1];
        foreach (var r in Enumerable.Range(0, n))
            foreach (var c in Enumerable.Range(0, n))
            {
                var step = board[r, c];
                if (step < 1 || step > n * n || positions[step] != default && step != 1) return false;
                positions[step] = (r, c);
            }
        for (var s = 2; s <= n * n; s++)
        {
            var (dr, dc) = (Math.Abs(positions[s].Item1 - positions[s - 1].Item1), Math.Abs(positions[s].Item2 - positions[s - 1].Item2));
            if (!((dr == 1 && dc == 2) || (dr == 2 && dc == 1))) return false;
        }
        return true;
    }
}
