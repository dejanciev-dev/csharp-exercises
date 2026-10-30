namespace CSharpExercises.Geometry;

/// <summary>
/// Andrew's monotone chain: sort the points, then build the lower and upper hulls with a stack, popping every point
/// that would make a right turn. O(n log n), dominated by the sort.
/// </summary>
public static class ConvexHull
{
    /// <summary>Returns the hull counter-clockwise, starting from the lowest-leftmost point, without collinear points.</summary>
    public static IReadOnlyList<Vec> Compute(IEnumerable<Vec> points)
    {
        var sorted = points.Distinct().OrderBy(p => p.X).ThenBy(p => p.Y).ToArray();
        if (sorted.Length < 3) return sorted;

        var hull = new Vec[2 * sorted.Length];
        var k = 0;

        foreach (var p in sorted)
        {
            while (k >= 2 && Vec.Orientation(hull[k - 2], hull[k - 1], p) <= 0) k--;
            hull[k++] = p;
        }

        // The upper hull walks back; it must not pop into the finished lower hull.
        for (int i = sorted.Length - 2, lower = k + 1; i >= 0; i--)
        {
            var p = sorted[i];
            while (k >= lower && Vec.Orientation(hull[k - 2], hull[k - 1], p) <= 0) k--;
            hull[k++] = p;
        }

        return hull[..(k - 1)]; // the last point repeats the first
    }

    /// <summary>The largest distance between two points lies between two hull vertices.</summary>
    public static double Diameter(IEnumerable<Vec> points)
    {
        var hull = Compute(points);
        var best = 0.0;
        for (var i = 0; i < hull.Count; i++)
            for (var j = i + 1; j < hull.Count; j++)
                best = Math.Max(best, hull[i].DistanceTo(hull[j]));
        return best;
    }
}
