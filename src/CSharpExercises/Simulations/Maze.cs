using System.Text;

namespace CSharpExercises.Simulations;

/// <summary>
/// Generating a maze with the recursive backtracker (a randomised depth-first search that knocks down walls) and
/// solving it with breadth-first search. Because the DFS visits every cell exactly once without forming cycles, the
/// result is a "perfect" maze: exactly one path between any two cells. Uses an explicit stack, so large mazes don't
/// overflow the call stack.
/// </summary>
public sealed class Maze
{
    [Flags]
    private enum Walls : byte { None = 0, North = 1, South = 2, East = 4, West = 8, All = 15 }

    private static readonly (Walls Wall, Walls Opposite, int Dr, int Dc)[] Directions =
    [
        (Walls.North, Walls.South, -1, 0), (Walls.South, Walls.North, 1, 0),
        (Walls.East, Walls.West, 0, 1), (Walls.West, Walls.East, 0, -1),
    ];

    private readonly Walls[,] _cells;

    public int Rows { get; }
    public int Cols { get; }

    public Maze(int rows, int cols, Random random)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rows, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(cols, 1);
        (Rows, Cols) = (rows, cols);
        _cells = new Walls[rows, cols];
        for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++) _cells[r, c] = Walls.All;

        var visited = new bool[rows, cols];
        var stack = new Stack<(int R, int C)>();
        stack.Push((0, 0));
        visited[0, 0] = true;

        while (stack.TryPeek(out var cell))
        {
            var options = Directions
                .Select(d => (d.Wall, d.Opposite, R: cell.R + d.Dr, C: cell.C + d.Dc))
                .Where(n => n.R >= 0 && n.R < rows && n.C >= 0 && n.C < cols && !visited[n.R, n.C])
                .ToArray();

            if (options.Length == 0)
            {
                stack.Pop(); // dead end: backtrack
                continue;
            }

            var next = options[random.Next(options.Length)];
            _cells[cell.R, cell.C] &= ~next.Wall;
            _cells[next.R, next.C] &= ~next.Opposite;
            visited[next.R, next.C] = true;
            stack.Push((next.R, next.C));
        }
    }

    public bool CanMove(int row, int col, int dr, int dc)
    {
        var direction = Directions.Single(d => d.Dr == dr && d.Dc == dc);
        return (_cells[row, col] & direction.Wall) == 0;
    }

    /// <summary>Shortest path from the top-left to the bottom-right corner.</summary>
    public IReadOnlyList<(int Row, int Col)> Solve()
    {
        var previous = new Dictionary<(int, int), (int, int)>();
        var queue = new Queue<(int R, int C)>([(0, 0)]);
        var goal = (Rows - 1, Cols - 1);
        var seen = new HashSet<(int, int)> { (0, 0) };

        while (queue.TryDequeue(out var cell))
        {
            if (cell == goal) break;
            foreach (var (wall, _, dr, dc) in Directions)
            {
                var next = (cell.R + dr, cell.C + dc);
                if ((_cells[cell.R, cell.C] & wall) != 0 || !seen.Add(next)) continue;
                previous[next] = cell;
                queue.Enqueue(next);
            }
        }

        var path = new List<(int, int)> { goal };
        for (var cell = goal; previous.TryGetValue(cell, out var p); cell = p) path.Add(p);
        path.Reverse();
        return path;
    }

    /// <summary>Counts passages: a perfect maze is a spanning tree, so it has exactly cells - 1 of them.</summary>
    public int PassageCount()
    {
        var count = 0;
        for (var r = 0; r < Rows; r++)
            for (var c = 0; c < Cols; c++)
            {
                if ((_cells[r, c] & Walls.South) == 0 && r + 1 < Rows) count++;
                if ((_cells[r, c] & Walls.East) == 0 && c + 1 < Cols) count++;
            }
        return count;
    }

    public string Render(IEnumerable<(int Row, int Col)>? path = null)
    {
        var onPath = path?.ToHashSet() ?? [];
        var sb = new StringBuilder();
        sb.Append('+').Append(string.Concat(Enumerable.Repeat("--+", Cols))).Append('\n');
        for (var r = 0; r < Rows; r++)
        {
            sb.Append('|');
            for (var c = 0; c < Cols; c++)
                sb.Append(onPath.Contains((r, c)) ? "()" : "  ").Append((_cells[r, c] & Walls.East) != 0 ? '|' : ' ');
            sb.Append("\n+");
            for (var c = 0; c < Cols; c++) sb.Append((_cells[r, c] & Walls.South) != 0 ? "--+" : "  +");
            sb.Append('\n');
        }
        return sb.ToString();
    }
}
