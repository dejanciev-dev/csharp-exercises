namespace CSharpExercises.Simulations;

/// <summary>
/// Flood fill, the paint bucket tool: replace a connected region of one colour. Iterative with a queue (a recursive
/// version overflows the stack on a large image). The same traversal counts islands on a map and finds the largest
/// one, and filling from the borders finds regions that are fully enclosed.
/// </summary>
public static class FloodFill
{
    private static readonly (int Dr, int Dc)[] Neighbours = [(-1, 0), (1, 0), (0, -1), (0, 1)];

    public static int Fill(char[,] image, int row, int col, char replacement)
    {
        var target = image[row, col];
        if (target == replacement) return 0;

        var filled = 0;
        var queue = new Queue<(int R, int C)>([(row, col)]);
        image[row, col] = replacement;
        while (queue.TryDequeue(out var cell))
        {
            filled++;
            foreach (var (dr, dc) in Neighbours)
            {
                var (r, c) = (cell.R + dr, cell.C + dc);
                if (r < 0 || r >= image.GetLength(0) || c < 0 || c >= image.GetLength(1) || image[r, c] != target) continue;
                image[r, c] = replacement; // mark when enqueuing, or a cell can be queued several times
                queue.Enqueue((r, c));
            }
        }
        return filled;
    }

    /// <summary>Number of islands ('#' regions) and the size of the largest, without modifying the map.</summary>
    public static (int Count, int Largest) Islands(string[] map)
    {
        var grid = ToGrid(map);
        int count = 0, largest = 0;
        for (var r = 0; r < grid.GetLength(0); r++)
            for (var c = 0; c < grid.GetLength(1); c++)
            {
                if (grid[r, c] != '#') continue;
                count++;
                largest = Math.Max(largest, Fill(grid, r, c, '~'));
            }
        return (count, largest);
    }

    /// <summary>
    /// "Capture surrounded regions": 'O' regions that don't touch the border become 'X'. Rather than test each
    /// region, mark everything reachable from the border as safe, then flip the rest.
    /// </summary>
    public static string[] CaptureSurrounded(string[] board)
    {
        var grid = ToGrid(board);
        int rows = grid.GetLength(0), cols = grid.GetLength(1);

        for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++)
                if ((r == 0 || c == 0 || r == rows - 1 || c == cols - 1) && grid[r, c] == 'O') Fill(grid, r, c, 'S');

        return Enumerable.Range(0, rows).Select(r => new string(Enumerable.Range(0, cols)
            .Select(c => grid[r, c] switch { 'S' => 'O', 'O' => 'X', var x => x }).ToArray())).ToArray();
    }

    public static char[,] ToGrid(string[] rows)
    {
        var grid = new char[rows.Length, rows.Length == 0 ? 0 : rows[0].Length];
        for (var r = 0; r < rows.Length; r++)
            for (var c = 0; c < rows[r].Length; c++) grid[r, c] = rows[r][c];
        return grid;
    }
}
