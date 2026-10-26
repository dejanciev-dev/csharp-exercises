using System.Numerics;

namespace CSharpExercises.ModernCSharp;

/// <summary>
/// Generic math: one implementation works for int, long, decimal, double, BigInteger and your own types,
/// because operators and constants like <c>T.Zero</c> are declared as static abstract interface members.
/// </summary>
public static class GenericMath
{
    /// <summary>Needs only "+" and a zero value, so it also works for <see cref="Vector2D{T}"/>.</summary>
    public static T Sum<T>(IEnumerable<T> values)
        where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
    {
        var total = T.AdditiveIdentity;
        foreach (var value in values) total += value;
        return total;
    }

    /// <summary>For integer types this uses integer division, just like the built-in operators.</summary>
    public static T Average<T>(IReadOnlyCollection<T> values) where T : INumber<T>
    {
        if (values.Count == 0) throw new InvalidOperationException("Cannot average an empty collection.");
        return Sum(values) / T.CreateChecked(values.Count);
    }

    public static T Gcd<T>(T a, T b) where T : IBinaryInteger<T>
    {
        (a, b) = (T.Abs(a), T.Abs(b));
        while (b != T.Zero) (a, b) = (b, a % b);
        return a;
    }

    /// <summary>Population standard deviation for any floating-point type (float, double, Half, ...).</summary>
    public static T StandardDeviation<T>(IReadOnlyCollection<T> values)
        where T : IFloatingPoint<T>, IRootFunctions<T>
    {
        var mean = Average(values);
        var squaredDiffs = values.Select(v => (v - mean) * (v - mean));
        return T.Sqrt(Sum(squaredDiffs) / T.CreateChecked(values.Count));
    }
}

/// <summary>A 2D vector over any numeric type that plugs into <see cref="GenericMath.Sum{T}"/> by implementing the operator interfaces.</summary>
public readonly record struct Vector2D<T>(T X, T Y) :
    IAdditionOperators<Vector2D<T>, Vector2D<T>, Vector2D<T>>,
    IAdditiveIdentity<Vector2D<T>, Vector2D<T>>
    where T : INumber<T>
{
    public static Vector2D<T> AdditiveIdentity => new(T.Zero, T.Zero);

    public static Vector2D<T> operator +(Vector2D<T> left, Vector2D<T> right) => new(left.X + right.X, left.Y + right.Y);

    public static Vector2D<T> operator *(Vector2D<T> vector, T scalar) => new(vector.X * scalar, vector.Y * scalar);

    public T Dot(Vector2D<T> other) => X * other.X + Y * other.Y;
}
