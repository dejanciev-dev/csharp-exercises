using System.Numerics;
using System.Text;

namespace CSharpExercises.Algorithms;

/// <summary>
/// Sudoku solver: backtracking plus two ideas that make it fast.
/// <list type="bullet">
/// <item>The digits used in each row, column and box are kept as bit masks, so "which digits can go here?" is a few bitwise operations.</item>
/// <item>It always fills the empty cell with the fewest options first. A cell with one option is a forced move,
/// and a cell with none means the current guess is wrong, found as early as possible.</item>
/// </list>
/// </summary>
public static class Sudoku
{
    private const int AllDigits = 0b11_1111_1110; // bits 1-9

    /// <summary>
    /// Parses 81 characters (digits, with '0' or '.' for empty cells; whitespace and '|' and '-' are ignored for layout)
    /// and returns the solved grid, or null if there is no solution.
    /// </summary>
    public static int[,]? Solve(string puzzle)
    {
        var grid = Parse(puzzle);
        return Solve(grid) ? grid : null;
    }

    /// <summary>Solves <paramref name="grid"/> in place (0 = empty). Returns false if it has no solution.</summary>
    public static bool Solve(int[,] grid)
    {
        if (grid.GetLength(0) != 9 || grid.GetLength(1) != 9) throw new ArgumentException("A sudoku grid is 9×9.", nameof(grid));

        var rows = new int[9];
        var columns = new int[9];
        var boxes = new int[9];

        for (var r = 0; r < 9; r++)
        for (var c = 0; c < 9; c++)
        {
            var digit = grid[r, c];
            if (digit == 0) continue;
            if (digit is < 1 or > 9) throw new ArgumentException($"Invalid digit {digit} at row {r + 1}, column {c + 1}.", nameof(grid));

            var bit = 1 << digit;
            // A digit repeated in the givens makes the puzzle unsolvable before we even start.
            if (((rows[r] | columns[c] | boxes[Box(r, c)]) & bit) != 0) return false;
            rows[r] |= bit;
            columns[c] |= bit;
            boxes[Box(r, c)] |= bit;
        }

        return Fill(grid, rows, columns, boxes);
    }

    public static string Format(int[,] grid)
    {
        var text = new StringBuilder();
        for (var r = 0; r < 9; r++)
        {
            if (r is 3 or 6) text.AppendLine("------+-------+------");
            for (var c = 0; c < 9; c++)
            {
                if (c is 3 or 6) text.Append("| ");
                text.Append(grid[r, c] == 0 ? '.' : (char)('0' + grid[r, c])).Append(c == 8 ? "" : " ");
            }
            text.AppendLine();
        }
        return text.ToString();
    }

    private static bool Fill(int[,] grid, int[] rows, int[] columns, int[] boxes)
    {
        // Find the empty cell with the fewest candidates.
        int bestRow = -1, bestColumn = -1, bestOptions = 0, fewest = 10;
        for (var r = 0; r < 9; r++)
        for (var c = 0; c < 9; c++)
        {
            if (grid[r, c] != 0) continue;

            var options = AllDigits & ~(rows[r] | columns[c] | boxes[Box(r, c)]);
            var count = BitOperations.PopCount((uint)options);
            if (count == 0) return false; // dead end: undo the last guess
            if (count < fewest) (bestRow, bestColumn, bestOptions, fewest) = (r, c, options, count);
        }

        if (bestRow < 0) return true; // no empty cells left: solved

        var box = Box(bestRow, bestColumn);
        while (bestOptions != 0)
        {
            var bit = bestOptions & -bestOptions; // lowest set bit
            bestOptions &= bestOptions - 1;       // clear it

            grid[bestRow, bestColumn] = BitOperations.TrailingZeroCount(bit);
            rows[bestRow] |= bit;
            columns[bestColumn] |= bit;
            boxes[box] |= bit;

            if (Fill(grid, rows, columns, boxes)) return true;

            rows[bestRow] &= ~bit;
            columns[bestColumn] &= ~bit;
            boxes[box] &= ~bit;
        }

        grid[bestRow, bestColumn] = 0;
        return false;
    }

    private static int Box(int row, int column) => row / 3 * 3 + column / 3;

    private static int[,] Parse(string puzzle)
    {
        var cells = puzzle.Where(ch => !char.IsWhiteSpace(ch) && ch is not '|' and not '-' and not '+').ToArray();
        if (cells.Length != 81) throw new FormatException($"Expected 81 cells but found {cells.Length}.");

        var grid = new int[9, 9];
        for (var i = 0; i < 81; i++)
        {
            grid[i / 9, i % 9] = cells[i] switch
            {
                '.' or '0' => 0,
                >= '1' and <= '9' => cells[i] - '0',
                var other => throw new FormatException($"'{other}' is not a digit or '.'."),
            };
        }
        return grid;
    }
}
