using System.Numerics;

namespace CSharpExercises.Numerics;

/// <summary>
/// An exact fraction. Always stored in lowest terms with a positive denominator, so equal values have equal fields
/// and the record's equality and hash code just work. <see cref="BigInteger"/> parts mean it never overflows,
/// at the cost of speed.
/// </summary>
public readonly record struct Rational :
    IComparable<Rational>,
    IAdditionOperators<Rational, Rational, Rational>,
    ISubtractionOperators<Rational, Rational, Rational>,
    IMultiplyOperators<Rational, Rational, Rational>,
    IDivisionOperators<Rational, Rational, Rational>,
    IUnaryNegationOperators<Rational, Rational>,
    IComparisonOperators<Rational, Rational, bool>
{
    // Stored as denominator - 1, so default(Rational) is 0/1 (zero) rather than an invalid 0/0.
    private readonly BigInteger _denominatorMinusOne;

    public BigInteger Numerator { get; }
    public BigInteger Denominator => _denominatorMinusOne + 1;

    public Rational(BigInteger numerator, BigInteger denominator)
    {
        if (denominator.IsZero) throw new DivideByZeroException("The denominator cannot be zero.");

        var gcd = BigInteger.GreatestCommonDivisor(numerator, denominator);
        if (denominator.Sign < 0) gcd = -gcd; // move the sign to the numerator
        Numerator = numerator / gcd;
        _denominatorMinusOne = denominator / gcd - 1;
    }

    public static Rational Zero => new(0, 1);
    public static Rational One => new(1, 1);

    public static implicit operator Rational(long value) => new(value, 1);
    public static explicit operator double(Rational value) => (double)value.Numerator / (double)value.Denominator;
    public static explicit operator decimal(Rational value) => (decimal)value.Numerator / (decimal)value.Denominator;

    public static Rational operator +(Rational a, Rational b) =>
        new(a.Numerator * b.Denominator + b.Numerator * a.Denominator, a.Denominator * b.Denominator);

    public static Rational operator -(Rational a, Rational b) => a + -b;
    public static Rational operator -(Rational value) => new(-value.Numerator, value.Denominator);
    public static Rational operator *(Rational a, Rational b) => new(a.Numerator * b.Numerator, a.Denominator * b.Denominator);
    public static Rational operator /(Rational a, Rational b) => new(a.Numerator * b.Denominator, a.Denominator * b.Numerator);

    // Denominators are positive, so cross-multiplying keeps the direction of the comparison.
    public int CompareTo(Rational other) => (Numerator * other.Denominator).CompareTo(other.Numerator * Denominator);

    public static bool operator <(Rational a, Rational b) => a.CompareTo(b) < 0;
    public static bool operator >(Rational a, Rational b) => a.CompareTo(b) > 0;
    public static bool operator <=(Rational a, Rational b) => a.CompareTo(b) <= 0;
    public static bool operator >=(Rational a, Rational b) => a.CompareTo(b) >= 0;

    /// <summary>Parses "3/4", "-2" or "0.375" (decimals become exact fractions: 0.375 is 3/8).</summary>
    public static Rational Parse(string text)
    {
        text = text.Trim();
        if (text.Split('/') is [var top, var bottom]) return new(BigInteger.Parse(top), BigInteger.Parse(bottom));

        var point = text.IndexOf('.');
        if (point < 0) return new(BigInteger.Parse(text), 1);

        var digitsAfterPoint = text.Length - point - 1;
        return new(BigInteger.Parse(text.Remove(point, 1)), BigInteger.Pow(10, digitsAfterPoint));
    }

    public override string ToString() => Denominator.IsOne ? Numerator.ToString() : $"{Numerator}/{Denominator}";
}
