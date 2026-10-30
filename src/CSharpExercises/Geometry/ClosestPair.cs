namespace CSharpExercises.Geometry;

/// <summary>
/// The closest pair of points in O(n log n) by divide and conquer: solve each half, then only check a narrow strip
/// around the dividing line, where each point has at most a handful of neighbours worth comparing.
/// </summary>
public static class ClosestPair
{
    public static (Vec A, Vec B, double Distance) Find(IReadOnlyList<Vec> points)
    {
        if (points.Count < 2) throw new ArgumentException("Need at least two points.", nameof(points));

        var byX = points.OrderBy(p => p.X).ThenBy(p => p.Y).ToArray();
        return Solve(byX, 0, byX.Length);
    }

    public static (Vec A, Vec B, double Distance) BruteForce(IReadOnlyList<Vec> points)
    {
        var best = (A: points[0], B: points[1], Distance: points[0].DistanceTo(points[1]));
        for (var i = 0; i < points.Count; i++)
            for (var j = i + 1; j < points.Count; j++)
            {
                var d = points[i].DistanceTo(points[j]);
                if (d < best.Distance) best = (points[i], points[j], d);
            }
        return best;
    }

    private static (Vec A, Vec B, double Distance) Solve(Vec[] byX, int from, int to)
    {
        if (to - from <= 3) return BruteForce(byX[from..to]);

        var mid = (from + to) / 2;
        var midX = byX[mid].X;
        var left = Solve(byX, from, mid);
        var right = Solve(byX, mid, to);
        var best = left.Distance <= right.Distance ? left : right;

        var strip = byX[from..to].Where(p => Math.Abs(p.X - midX) < best.Distance).OrderBy(p => p.Y).ToArray();
        for (var i = 0; i < strip.Length; i++)
        {
            // Sorted by y, so once the gap in y alone is too big, nothing further up can be closer.
            for (var j = i + 1; j < strip.Length && strip[j].Y - strip[i].Y < best.Distance; j++)
            {
                var d = strip[i].DistanceTo(strip[j]);
                if (d < best.Distance) best = (strip[i], strip[j], d);
            }
        }
        return best;
    }
}
