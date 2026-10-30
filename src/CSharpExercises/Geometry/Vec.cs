namespace CSharpExercises.Geometry;

/// <summary>
/// A 2D point or vector. Almost every geometry algorithm reduces to two products: the dot product (how much two
/// vectors point the same way) and the cross product (which side of one vector the other lies on).
/// </summary>
public readonly record struct Vec(double X, double Y)
{
    public static Vec operator +(Vec a, Vec b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec operator -(Vec a, Vec b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec operator *(Vec a, double k) => new(a.X * k, a.Y * k);

    public double Dot(Vec other) => X * other.X + Y * other.Y;

    /// <summary>The z component of the 3D cross product: positive if <paramref name="other"/> is counter-clockwise from this.</summary>
    public double Cross(Vec other) => X * other.Y - Y * other.X;

    public double Length => Math.Sqrt(Dot(this));

    public double DistanceTo(Vec other) => (other - this).Length;

    /// <summary>Positive for a left turn a → b → c, negative for a right turn, zero when the three points are collinear.</summary>
    public static double Orientation(Vec a, Vec b, Vec c) => (b - a).Cross(c - a);
}

public static class Segments
{
    /// <summary>
    /// Two segments intersect when each one's endpoints lie on opposite sides of the other, plus the collinear
    /// special cases where an endpoint lies on the other segment. No division, so no precision trouble with integers.
    /// </summary>
    public static bool Intersect(Vec p1, Vec p2, Vec q1, Vec q2)
    {
        var d1 = Math.Sign(Vec.Orientation(q1, q2, p1));
        var d2 = Math.Sign(Vec.Orientation(q1, q2, p2));
        var d3 = Math.Sign(Vec.Orientation(p1, p2, q1));
        var d4 = Math.Sign(Vec.Orientation(p1, p2, q2));

        if (d1 * d2 < 0 && d3 * d4 < 0) return true;

        return (d1 == 0 && OnSegment(q1, q2, p1))
            || (d2 == 0 && OnSegment(q1, q2, p2))
            || (d3 == 0 && OnSegment(p1, p2, q1))
            || (d4 == 0 && OnSegment(p1, p2, q2));
    }

    /// <summary>The intersection point of the infinite lines through the segments, or null when they are parallel.</summary>
    public static Vec? LineIntersection(Vec p1, Vec p2, Vec q1, Vec q2)
    {
        var r = p2 - p1;
        var s = q2 - q1;
        var denominator = r.Cross(s);
        if (Math.Abs(denominator) < 1e-12) return null;

        var t = (q1 - p1).Cross(s) / denominator;
        return p1 + r * t;
    }

    /// <summary>Shortest distance from <paramref name="p"/> to segment a–b: project, then clamp to the endpoints.</summary>
    public static double DistanceToSegment(Vec p, Vec a, Vec b)
    {
        var ab = b - a;
        var lengthSquared = ab.Dot(ab);
        if (lengthSquared == 0) return p.DistanceTo(a);

        var t = Math.Clamp((p - a).Dot(ab) / lengthSquared, 0, 1);
        return p.DistanceTo(a + ab * t);
    }

    private static bool OnSegment(Vec a, Vec b, Vec p) =>
        p.X >= Math.Min(a.X, b.X) && p.X <= Math.Max(a.X, b.X) &&
        p.Y >= Math.Min(a.Y, b.Y) && p.Y <= Math.Max(a.Y, b.Y);
}
