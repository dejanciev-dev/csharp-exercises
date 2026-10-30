using System.Globalization;

namespace CSharpExercises.Numerics;

/// <summary>
/// Q16.16 fixed-point numbers: an int whose lower 16 bits are the fraction, so 1.0 is 65536. Arithmetic is plain
/// integer arithmetic (fast, and identical on every machine, which deterministic game simulations and lockstep
/// networking need), at the cost of range (±32768) and precision (1/65536). Multiplication needs a 64-bit
/// intermediate and a shift back; division shifts first.
/// </summary>
public readonly record struct Fixed16(int Raw) : IComparable<Fixed16>
{
    public const int FractionBits = 16;
    private const int One = 1 << FractionBits;

    /// <summary>Range-checked by hand: shifts are never overflow-checked, even inside checked().</summary>
    public static Fixed16 FromInt(int value) =>
        value is < short.MinValue or > short.MaxValue ? throw new OverflowException($"{value} is outside the Q16.16 range.") : new(value << FractionBits);

    public static Fixed16 FromDouble(double value) => new(checked((int)Math.Round(value * One)));

    public double ToDouble() => (double)Raw / One;

    public static Fixed16 operator +(Fixed16 a, Fixed16 b) => new(checked(a.Raw + b.Raw));
    public static Fixed16 operator -(Fixed16 a, Fixed16 b) => new(checked(a.Raw - b.Raw));
    public static Fixed16 operator -(Fixed16 a) => new(checked(-a.Raw));

    /// <summary>(a·2^16)(b·2^16) = ab·2^32, so shift right by 16 to get back to ab·2^16; round instead of truncating.</summary>
    public static Fixed16 operator *(Fixed16 a, Fixed16 b) => new(checked((int)(((long)a.Raw * b.Raw + (One >> 1)) >> FractionBits)));

    public static Fixed16 operator /(Fixed16 a, Fixed16 b) =>
        b.Raw == 0 ? throw new DivideByZeroException() : new(checked((int)(((long)a.Raw << FractionBits) / b.Raw)));

    public static bool operator <(Fixed16 a, Fixed16 b) => a.Raw < b.Raw;
    public static bool operator >(Fixed16 a, Fixed16 b) => a.Raw > b.Raw;

    public int CompareTo(Fixed16 other) => Raw.CompareTo(other.Raw);

    public int Floor() => Raw >> FractionBits; // arithmetic shift rounds toward minus infinity

    /// <summary>Square root with Newton's method in integer arithmetic only.</summary>
    public Fixed16 Sqrt()
    {
        if (Raw < 0) throw new ArgumentOutOfRangeException(nameof(Raw), "No square root of a negative number.");
        if (Raw == 0) return this;
        var target = (long)Raw << FractionBits; // we want x with x² = Raw · 2^16
        var x = target;
        for (var previous = 0L; x != previous;) (previous, x) = (x, (x + target / x) / 2);
        return new Fixed16((int)x);
    }

    public static Fixed16 Epsilon => new(1);

    public override string ToString() => ToDouble().ToString("0.#####", CultureInfo.InvariantCulture);
}
