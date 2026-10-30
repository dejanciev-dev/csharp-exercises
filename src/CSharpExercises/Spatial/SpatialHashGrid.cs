namespace CSharpExercises.Spatial;

public readonly record struct Circle2(int Id, double X, double Y, double Radius)
{
    public bool Overlaps(Circle2 other)
    {
        var (dx, dy, r) = (X - other.X, Y - other.Y, Radius + other.Radius);
        return dx * dx + dy * dy < r * r;
    }
}

/// <summary>
/// Collision detection's "broad phase": instead of testing every pair of objects (n² checks), drop each into the
/// uniform grid cells its bounding box touches, and only test pairs that share a cell. With a cell size near the
/// typical object size, the work is close to linear. Good for many similar-sized moving objects; quadtrees suit
/// uneven sizes better.
/// </summary>
public sealed class SpatialHashGrid(double cellSize)
{
    private readonly Dictionary<(int, int), List<Circle2>> _cells = new();

    public int PairChecks { get; private set; }

    public void Clear() => _cells.Clear();

    public void Insert(Circle2 circle)
    {
        foreach (var cell in CellsCovering(circle))
        {
            if (!_cells.TryGetValue(cell, out var list)) _cells[cell] = list = [];
            list.Add(circle);
        }
    }

    /// <summary>Every overlapping pair, each reported once even when the two share several cells.</summary>
    public IReadOnlySet<(int, int)> CollidingPairs()
    {
        var pairs = new HashSet<(int, int)>();
        PairChecks = 0;
        foreach (var list in _cells.Values)
            for (var i = 0; i < list.Count; i++)
                for (var j = i + 1; j < list.Count; j++)
                {
                    var (a, b) = (list[i], list[j]);
                    var key = a.Id < b.Id ? (a.Id, b.Id) : (b.Id, a.Id);
                    if (pairs.Contains(key)) continue;
                    PairChecks++;
                    if (a.Overlaps(b)) pairs.Add(key);
                }
        return pairs;
    }

    public static IReadOnlySet<(int, int)> BruteForce(IReadOnlyList<Circle2> circles)
    {
        var pairs = new HashSet<(int, int)>();
        for (var i = 0; i < circles.Count; i++)
            for (var j = i + 1; j < circles.Count; j++)
                if (circles[i].Overlaps(circles[j]))
                    pairs.Add(circles[i].Id < circles[j].Id ? (circles[i].Id, circles[j].Id) : (circles[j].Id, circles[i].Id));
        return pairs;
    }

    private IEnumerable<(int, int)> CellsCovering(Circle2 c)
    {
        int Cell(double v) => (int)Math.Floor(v / cellSize);
        for (var x = Cell(c.X - c.Radius); x <= Cell(c.X + c.Radius); x++)
            for (var y = Cell(c.Y - c.Radius); y <= Cell(c.Y + c.Radius); y++)
                yield return (x, y);
    }
}
