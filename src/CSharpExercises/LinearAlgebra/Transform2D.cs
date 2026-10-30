namespace CSharpExercises.LinearAlgebra;

/// <summary>
/// 2D affine transforms as 3×3 matrices in homogeneous coordinates: adding a constant 1 to each point lets
/// translation be a matrix multiplication too, so any sequence of moves, rotations and scales composes into one
/// matrix. This is what graphics APIs and SVG's transform attribute do.
/// </summary>
public readonly record struct Transform2D(double A, double B, double C, double D, double Tx, double Ty)
{
    // | A  B  Tx |
    // | C  D  Ty |
    // | 0  0  1  |

    public static Transform2D Identity => new(1, 0, 0, 1, 0, 0);

    public static Transform2D Translate(double dx, double dy) => new(1, 0, 0, 1, dx, dy);

    public static Transform2D Scale(double sx, double sy) => new(sx, 0, 0, sy, 0, 0);

    public static Transform2D Rotate(double degrees)
    {
        var radians = degrees * Math.PI / 180;
        var (sin, cos) = Math.SinCos(radians);
        return new(cos, -sin, sin, cos, 0, 0);
    }

    /// <summary>Rotating around a point: move it to the origin, rotate, move back.</summary>
    public static Transform2D RotateAround(double degrees, double x, double y) =>
        Translate(x, y) * Rotate(degrees) * Translate(-x, -y);

    /// <summary>Composition: (a * b) applies b first, then a.</summary>
    public static Transform2D operator *(Transform2D a, Transform2D b) => new(
        a.A * b.A + a.B * b.C,
        a.A * b.B + a.B * b.D,
        a.C * b.A + a.D * b.C,
        a.C * b.B + a.D * b.D,
        a.A * b.Tx + a.B * b.Ty + a.Tx,
        a.C * b.Tx + a.D * b.Ty + a.Ty);

    public (double X, double Y) Apply(double x, double y) => (A * x + B * y + Tx, C * x + D * y + Ty);

    public double Determinant => A * D - B * C;

    public Transform2D? Inverse()
    {
        var det = Determinant;
        if (Math.Abs(det) < 1e-12) return null;
        return new Transform2D(D / det, -B / det, -C / det, A / det,
                               (B * Ty - D * Tx) / det, (C * Tx - A * Ty) / det);
    }
}
