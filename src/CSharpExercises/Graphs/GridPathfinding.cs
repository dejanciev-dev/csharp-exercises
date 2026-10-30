namespace CSharpExercises.Graphs;

/// <summary>
/// A* on a grid of walls ('#') and open cells, where the cost of a cell is 1, or its digit if it has one (mud, hills).
/// Dijkstra explores outward in every direction; A* adds a heuristic (Manhattan distance, which never overestimates)
/// that pulls the search toward the goal and expands far fewer cells, while still finding an optimal path.
/// </summary>
public sealed class GridPathfinding(IReadOnlyList<string> grid)
{
    private static readonly (int Dr, int Dc)[] Moves = [(-1, 0), (1, 0), (0, -1), (0, 1)];

    public int Rows => grid.Count;
    public int Cols => grid[0].Length;

    /// <summary>Number of cells taken off the open set by the last search, to compare A* against Dijkstra.</summary>
    public int Expanded { get; private set; }

    public (int Cost, IReadOnlyList<(int Row, int Col)> Path)? FindPath((int Row, int Col) start, (int Row, int Col) goal, bool useHeuristic = true)
    {
        var cost = new Dictionary<(int, int), int> { [start] = 0 };
        var cameFrom = new Dictionary<(int, int), (int, int)>();
        var open = new PriorityQueue<(int Row, int Col), int>();
        open.Enqueue(start, 0);
        Expanded = 0;

        while (open.TryDequeue(out var current, out var priority))
        {
            var g = cost[current];
            if (priority > g + Heuristic(current)) continue; // stale entry: a cheaper route was found later
            Expanded++;

            if (current == goal) return (g, Reconstruct(cameFrom, goal));

            foreach (var (dr, dc) in Moves)
            {
                var next = (Row: current.Row + dr, Col: current.Col + dc);
                if (next.Row < 0 || next.Row >= Rows || next.Col < 0 || next.Col >= Cols) continue;
                var cell = grid[next.Row][next.Col];
                if (cell == '#') continue;

                var nextCost = g + (char.IsAsciiDigit(cell) ? cell - '0' : 1);
                if (cost.TryGetValue(next, out var known) && known <= nextCost) continue;

                cost[next] = nextCost;
                cameFrom[next] = current;
                open.Enqueue(next, nextCost + Heuristic(next));
            }
        }
        return null;

        int Heuristic((int Row, int Col) cell) =>
            useHeuristic ? Math.Abs(cell.Row - goal.Row) + Math.Abs(cell.Col - goal.Col) : 0;
    }

    private static List<(int, int)> Reconstruct(Dictionary<(int, int), (int, int)> cameFrom, (int, int) goal)
    {
        var path = new List<(int, int)> { goal };
        for (var cell = goal; cameFrom.TryGetValue(cell, out var previous); cell = previous) path.Add(previous);
        path.Reverse();
        return path;
    }
}
