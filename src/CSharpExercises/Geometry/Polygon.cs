namespace CSharpExercises.Geometry;

/// <summary>
/// A simple polygon given by its vertices in order. Area with the shoelace formula, containment by casting a ray and
/// counting crossings, and the centroid from the same signed triangle areas.
/// </summary>
public sealed class Polygon
{
    private readonly Vec[] _vertices;

    public Polygon(IEnumerable<Vec> vertices)
    {
        _vertices = [.. vertices];
        if (_vertices.Length < 3) throw new ArgumentException("A polygon needs at least three vertices.", nameof(vertices));
    }

    public IReadOnlyList<Vec> Vertices => _vertices;

    /// <summary>Positive when the vertices run counter-clockwise, negative when clockwise.</summary>
    public double SignedArea
    {
        get
        {
            var sum = 0.0;
            for (var i = 0; i < _vertices.Length; i++)
                sum += _vertices[i].Cross(_vertices[(i + 1) % _vertices.Length]);
            return sum / 2;
        }
    }

    public double Area => Math.Abs(SignedArea);

    public bool IsCounterClockwise => SignedArea > 0;

    public double Perimeter
    {
        get
        {
            var sum = 0.0;
            for (var i = 0; i < _vertices.Length; i++)
                sum += _vertices[i].DistanceTo(_vertices[(i + 1) % _vertices.Length]);
            return sum;
        }
    }

    public Vec Centroid
    {
        get
        {
            double cx = 0, cy = 0;
            for (var i = 0; i < _vertices.Length; i++)
            {
                var a = _vertices[i];
                var b = _vertices[(i + 1) % _vertices.Length];
                var cross = a.Cross(b);
                cx += (a.X + b.X) * cross;
                cy += (a.Y + b.Y) * cross;
            }
            var factor = 1 / (6 * SignedArea);
            return new Vec(cx * factor, cy * factor);
        }
    }

    /// <summary>
    /// Casts a ray to the right and counts edge crossings: odd means inside. The half-open test
    /// (a.Y > p.Y) != (b.Y > p.Y) counts a vertex exactly on the ray once, not twice.
    /// </summary>
    public bool Contains(Vec p)
    {
        var inside = false;
        for (int i = 0, j = _vertices.Length - 1; i < _vertices.Length; j = i++)
        {
            var a = _vertices[i];
            var b = _vertices[j];
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X)
                inside = !inside;
        }
        return inside;
    }

    /// <summary>Convex when every turn goes the same way.</summary>
    public bool IsConvex
    {
        get
        {
            var sign = 0;
            for (var i = 0; i < _vertices.Length; i++)
            {
                var turn = Math.Sign(Vec.Orientation(
                    _vertices[i], _vertices[(i + 1) % _vertices.Length], _vertices[(i + 2) % _vertices.Length]));
                if (turn == 0) continue;
                if (sign != 0 && turn != sign) return false;
                sign = turn;
            }
            return true;
        }
    }
}
