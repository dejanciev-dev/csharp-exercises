using System.Numerics;
using System.Text;

namespace CSharpExercises.Numerics;

/// <summary>
/// Positional number systems beyond 2, 10 and 16: any base from 2 to 36 (with BigInteger, any size), negative bases
/// (negabinary, base -2, represents negative numbers without a sign), balanced ternary (digits -1, 0, 1, written
/// T, 0, 1: negation is just swapping T and 1) and bijective base 26, the spreadsheet column scheme A…Z, AA, AB.
/// </summary>
public static class BaseConversion
{
    private const string Digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public static string ToBase(BigInteger value, int radix)
    {
        if (radix is < 2 or > 36) throw new ArgumentOutOfRangeException(nameof(radix));
        if (value.IsZero) return "0";
        var negative = value.Sign < 0;
        value = BigInteger.Abs(value);
        var sb = new StringBuilder();
        while (!value.IsZero)
        {
            value = BigInteger.DivRem(value, radix, out var digit);
            sb.Insert(0, Digits[(int)digit]);
        }
        return negative ? "-" + sb : sb.ToString();
    }

    public static BigInteger FromBase(string text, int radix)
    {
        var negative = text.StartsWith('-');
        BigInteger value = 0;
        foreach (var c in negative ? text[1..] : text)
        {
            var digit = Digits.IndexOf(char.ToUpperInvariant(c));
            if (digit < 0 || digit >= radix) throw new FormatException($"'{c}' is not a base-{radix} digit.");
            value = value * radix + digit;
        }
        return negative ? -value : value;
    }

    /// <summary>Base -2: the remainder must be made non-negative, adjusting the quotient to match.</summary>
    public static string ToNegabinary(long value)
    {
        if (value == 0) return "0";
        var sb = new StringBuilder();
        while (value != 0)
        {
            var remainder = value % -2;
            value /= -2;
            if (remainder < 0) (remainder, value) = (remainder + 2, value + 1);
            sb.Insert(0, remainder);
        }
        return sb.ToString();
    }

    public static long FromNegabinary(string digits) => digits.Aggregate(0L, (acc, c) => acc * -2 + (c - '0'));

    public static string ToBalancedTernary(long value)
    {
        if (value == 0) return "0";
        var sb = new StringBuilder();
        while (value != 0)
        {
            var remainder = (int)(((value % 3) + 3) % 3);
            if (remainder == 2) { sb.Insert(0, 'T'); value = (value + 1) / 3; } // 2 = 3 - 1: write -1, carry one
            else { sb.Insert(0, remainder == 1 ? '1' : '0'); value = (value - remainder) / 3; }
        }
        return sb.ToString();
    }

    public static long FromBalancedTernary(string digits) =>
        digits.Aggregate(0L, (acc, c) => acc * 3 + c switch { 'T' => -1, '0' => 0, '1' => 1, _ => throw new FormatException($"'{c}'") });

    /// <summary>Spreadsheet columns: there's no zero digit, so 26 is Z and 27 is AA.</summary>
    public static string ToColumnName(int column)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(column, 1);
        var sb = new StringBuilder();
        for (; column > 0; column = (column - 1) / 26) sb.Insert(0, (char)('A' + (column - 1) % 26));
        return sb.ToString();
    }

    public static int FromColumnName(string name) => name.ToUpperInvariant().Aggregate(0, (acc, c) => acc * 26 + (c - 'A' + 1));
}
