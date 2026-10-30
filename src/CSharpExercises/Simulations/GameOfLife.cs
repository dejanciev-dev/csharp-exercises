namespace CSharpExercises.Simulations;

/// <summary>
/// Conway's Game of Life on an unbounded board, stored as the set of live cells instead of a fixed grid: patterns
/// can travel forever, and the cost depends on the population, not the board size. Only live cells and their
/// neighbours can change, so those are the only ones counted.
/// </summary>
public sealed class GameOfLife
{
    private static readonly (int Dx, int Dy)[] Offsets =
        [(-1, -1), (0, -1), (1, -1), (-1, 0), (1, 0), (-1, 1), (0, 1), (1, 1)];

    private HashSet<(int X, int Y)> _alive;

    public GameOfLife(IEnumerable<(int X, int Y)> alive) => _alive = [.. alive];

    /// <summary>Reads a pattern drawn with '#' or 'O' for live cells, one string per row.</summary>
    public static GameOfLife FromPattern(params string[] rows) =>
        new(rows.SelectMany((row, y) => row.Select((c, x) => (c, x)).Where(p => p.c is '#' or 'O').Select(p => (p.x, y))));

    public IReadOnlySet<(int X, int Y)> Alive => _alive;

    public int Generation { get; private set; }

    public void Step()
    {
        var neighbourCounts = new Dictionary<(int, int), int>();
        foreach (var (x, y) in _alive)
            foreach (var (dx, dy) in Offsets)
            {
                var cell = (x + dx, y + dy);
                neighbourCounts[cell] = neighbourCounts.GetValueOrDefault(cell) + 1;
            }

        // Born with exactly 3 neighbours, survives with 2 or 3.
        _alive = neighbourCounts
            .Where(p => p.Value == 3 || (p.Value == 2 && _alive.Contains(p.Key)))
            .Select(p => p.Key)
            .ToHashSet();
        Generation++;
    }

    public void Run(int generations)
    {
        for (var i = 0; i < generations; i++) Step();
    }

    /// <summary>The live cells moved so the top-left of their bounding box is at (0, 0), to compare shapes.</summary>
    public IReadOnlySet<(int X, int Y)> Normalised()
    {
        if (_alive.Count == 0) return _alive;
        var minX = _alive.Min(c => c.X);
        var minY = _alive.Min(c => c.Y);
        return _alive.Select(c => (c.X - minX, c.Y - minY)).ToHashSet();
    }

    public string Render()
    {
        if (_alive.Count == 0) return "";
        int minX = _alive.Min(c => c.X), maxX = _alive.Max(c => c.X);
        int minY = _alive.Min(c => c.Y), maxY = _alive.Max(c => c.Y);
        return string.Join('\n', Enumerable.Range(minY, maxY - minY + 1).Select(y =>
            new string(Enumerable.Range(minX, maxX - minX + 1).Select(x => _alive.Contains((x, y)) ? '#' : '.').ToArray())));
    }
}
