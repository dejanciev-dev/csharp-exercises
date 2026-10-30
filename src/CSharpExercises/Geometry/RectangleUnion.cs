namespace CSharpExercises.Geometry;

public readonly record struct Box(int Left, int Bottom, int Right, int Top)
{
    public long Area => (long)Math.Max(0, Right - Left) * Math.Max(0, Top - Bottom);

    public bool Overlaps(Box other) =>
        Left < other.Right && other.Left < Right && Bottom < other.Top && other.Bottom < Top;

    public Box? Intersect(Box other)
    {
        var box = new Box(Math.Max(Left, other.Left), Math.Max(Bottom, other.Bottom),
                          Math.Min(Right, other.Right), Math.Min(Top, other.Top));
        return box.Left < box.Right && box.Bottom < box.Top ? box : null;
    }
}

/// <summary>
/// The area covered by overlapping rectangles, counting shared parts once. A sweep line moves left to right over
/// the x coordinates where rectangles start or end; between two stops the covered height is constant.
/// </summary>
public static class RectangleUnion
{
    public static long Area(IReadOnlyList<Box> boxes)
    {
        var events = boxes
            .Where(b => b.Area > 0)
            .SelectMany(b => new[] { (X: b.Left, Box: b, Delta: +1), (X: b.Right, Box: b, Delta: -1) })
            .OrderBy(e => e.X)
            .ToArray();

        var active = new List<Box>();
        long total = 0;
        var previousX = 0;

        foreach (var (x, box, delta) in events)
        {
            total += (long)(x - previousX) * CoveredHeight(active);
            if (delta > 0) active.Add(box);
            else active.Remove(box);
            previousX = x;
        }
        return total;
    }

    /// <summary>Length of the union of the y-intervals of the active rectangles: merge overlapping intervals.</summary>
    private static long CoveredHeight(List<Box> active)
    {
        long covered = 0;
        long end = long.MinValue;
        foreach (var box in active.OrderBy(b => b.Bottom))
        {
            var start = Math.Max(box.Bottom, end);
            if (box.Top > start) covered += box.Top - start;
            end = Math.Max(end, box.Top);
        }
        return covered;
    }
}
