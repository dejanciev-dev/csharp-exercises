namespace CSharpExercises.Spatial;

public readonly record struct Point2(double X, double Y)
{
    public double DistanceSquaredTo(Point2 other) => (X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y);
}

public readonly record struct Bounds(double MinX, double MinY, double MaxX, double MaxY)
{
    public bool Contains(Point2 p) => p.X >= MinX && p.X < MaxX && p.Y >= MinY && p.Y < MaxY;
    public bool Intersects(Bounds o) => MinX < o.MaxX && o.MinX < MaxX && MinY < o.MaxY && o.MinY < MaxY;

    /// <summary>Squared distance from a point to the nearest point of the box (0 if inside).</summary>
    public double DistanceSquaredTo(Point2 p)
    {
        var dx = Math.Max(Math.Max(MinX - p.X, 0), p.X - MaxX);
        var dy = Math.Max(Math.Max(MinY - p.Y, 0), p.Y - MaxY);
        return dx * dx + dy * dy;
    }
}

/// <summary>
/// A point quadtree: each node holds a few points and splits into four quadrants when it overflows, so dense areas get
/// finer cells. A range query only visits quadrants that overlap the search box, which is how maps answer "what's
/// visible on screen" over millions of points.
/// </summary>
public sealed class QuadTree<T>(Bounds bounds, int capacity = 8, int maxDepth = 16)
{
    private readonly List<(Point2 Point, T Value)> _items = [];
    private QuadTree<T>[]? _children;

    public Bounds Bounds { get; } = bounds;
    public int Count { get; private set; }

    public bool Insert(Point2 point, T value) => Insert(point, value, 0);

    public IEnumerable<(Point2 Point, T Value)> Query(Bounds area)
    {
        if (!Bounds.Intersects(area)) yield break;
        foreach (var item in _items)
            if (area.Contains(item.Point)) yield return item;
        if (_children is null) yield break;
        foreach (var child in _children)
            foreach (var item in child.Query(area))
                yield return item;
    }

    /// <summary>Points within a radius: query the bounding square, then filter by exact distance.</summary>
    public IEnumerable<(Point2 Point, T Value)> Within(Point2 center, double radius) =>
        Query(new Bounds(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius))
            .Where(i => i.Point.DistanceSquaredTo(center) <= radius * radius);

    /// <summary>Number of nodes, to show that the tree adapts to the data.</summary>
    public int NodeCount => 1 + (_children?.Sum(c => c.NodeCount) ?? 0);

    private bool Insert(Point2 point, T value, int depth)
    {
        if (!Bounds.Contains(point)) return false;
        Count++;

        if (_children is null)
        {
            if (_items.Count < capacity || depth >= maxDepth)
            {
                _items.Add((point, value));
                return true;
            }
            Split(depth);
        }

        foreach (var child in _children!)
            if (child.Insert(point, value, depth + 1)) return true;
        throw new InvalidOperationException("Point fell between quadrants.");
    }

    private void Split(int depth)
    {
        var midX = (Bounds.MinX + Bounds.MaxX) / 2;
        var midY = (Bounds.MinY + Bounds.MaxY) / 2;
        _children =
        [
            new(new Bounds(Bounds.MinX, Bounds.MinY, midX, midY), capacity, maxDepth),
            new(new Bounds(midX, Bounds.MinY, Bounds.MaxX, midY), capacity, maxDepth),
            new(new Bounds(Bounds.MinX, midY, midX, Bounds.MaxY), capacity, maxDepth),
            new(new Bounds(midX, midY, Bounds.MaxX, Bounds.MaxY), capacity, maxDepth),
        ];
        foreach (var (p, v) in _items)
            foreach (var child in _children)
                if (child.Insert(p, v, depth + 1)) break;
        _items.Clear();
    }
}
