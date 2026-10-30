namespace CSharpExercises.Puzzles;

/// <summary>
/// The 8-puzzle (tiles 1–8 in a 3×3 frame with one gap) solved optimally with A*. The heuristic is the sum of each
/// tile's Manhattan distance to its goal square, which never overestimates (every move shifts one tile by one), so the
/// first solution found is the shortest. Half of all arrangements are unsolvable, detectable up front by counting
/// inversions: a move never changes their parity.
/// </summary>
public static class SlidingPuzzle
{
    public const string Goal = "123456780"; // 0 is the gap

    public static bool IsSolvable(string board)
    {
        var tiles = board.Where(c => c != '0').ToArray();
        var inversions = 0;
        for (var i = 0; i < tiles.Length; i++)
            for (var j = i + 1; j < tiles.Length; j++)
                if (tiles[i] > tiles[j]) inversions++;
        return inversions % 2 == 0;
    }

    /// <returns>The tiles moved, in order, or null if unsolvable.</returns>
    public static IReadOnlyList<char>? Solve(string start)
    {
        if (start.Length != 9 || start.Order().SequenceEqual(Goal.Order()) is false) throw new ArgumentException("Need the digits 0–8 once each.", nameof(start));
        if (!IsSolvable(start)) return null;

        var cost = new Dictionary<string, int> { [start] = 0 };
        var cameFrom = new Dictionary<string, (string Board, char Tile)>();
        var open = new PriorityQueue<string, int>();
        open.Enqueue(start, Heuristic(start));

        while (open.TryDequeue(out var board, out _))
        {
            if (board == Goal)
            {
                var tiles = new List<char>();
                for (var b = board; cameFrom.TryGetValue(b, out var step); b = step.Board) tiles.Add(step.Tile);
                tiles.Reverse();
                return tiles;
            }

            var gap = board.IndexOf('0');
            foreach (var neighbour in Neighbours(gap))
            {
                var next = board.ToCharArray();
                (next[gap], next[neighbour]) = (next[neighbour], next[gap]);
                var nextBoard = new string(next);
                var nextCost = cost[board] + 1;
                if (cost.TryGetValue(nextBoard, out var known) && known <= nextCost) continue;
                cost[nextBoard] = nextCost;
                cameFrom[nextBoard] = (board, board[neighbour]);
                open.Enqueue(nextBoard, nextCost + Heuristic(nextBoard));
            }
        }
        return null;
    }

    public static string Apply(string board, IEnumerable<char> tiles)
    {
        var cells = board.ToCharArray();
        foreach (var tile in tiles)
        {
            var (gap, at) = (Array.IndexOf(cells, '0'), Array.IndexOf(cells, tile));
            if (!Neighbours(gap).Contains(at)) throw new InvalidOperationException($"Tile {tile} isn't next to the gap.");
            (cells[gap], cells[at]) = (cells[at], cells[gap]);
        }
        return new string(cells);
    }

    private static int Heuristic(string board)
    {
        var total = 0;
        for (var i = 0; i < 9; i++)
        {
            if (board[i] == '0') continue;
            var goal = board[i] - '1';
            total += Math.Abs(i / 3 - goal / 3) + Math.Abs(i % 3 - goal % 3);
        }
        return total;
    }

    private static IEnumerable<int> Neighbours(int gap)
    {
        if (gap >= 3) yield return gap - 3;
        if (gap < 6) yield return gap + 3;
        if (gap % 3 > 0) yield return gap - 1;
        if (gap % 3 < 2) yield return gap + 1;
    }
}
