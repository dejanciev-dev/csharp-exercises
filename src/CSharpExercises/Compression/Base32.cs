using System.Text;

namespace CSharpExercises.Compression;

/// <summary>
/// Crockford's Base32: 5 bits per character, with an alphabet chosen for humans reading codes aloud or typing them in.
/// It has no I, L, O or U, decoding accepts lowercase, hyphens, and I/L/O as 1/1/0, and numbers can carry a check
/// symbol that catches a mistyped or swapped character.
/// </summary>
public static class Base32
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    // The check symbol is the value mod 37 (a prime, so it catches every single-character error and every swap of two
    // neighbours), which needs five symbols beyond the alphabet.
    private const string CheckSymbols = Alphabet + "*~$=U";

    public static string Encode(ReadOnlySpan<byte> data)
    {
        var text = new StringBuilder((data.Length * 8 + 4) / 5);
        int buffer = 0, bits = 0;
        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                bits -= 5;
                text.Append(Alphabet[(buffer >> bits) & 31]);
            }
            buffer &= (1 << bits) - 1; // keep only the bits not yet written
        }
        if (bits > 0) text.Append(Alphabet[(buffer << (5 - bits)) & 31]);
        return text.ToString();
    }

    /// <summary>
    /// Decodes text from <see cref="Encode"/>. Rejects a length no encoding produces and non-zero padding bits, so every
    /// byte array has exactly one valid encoding.
    /// </summary>
    public static bool TryDecode(string text, out byte[] data)
    {
        data = [];
        var output = new List<byte>(text.Length * 5 / 8);
        int buffer = 0, bits = 0;
        foreach (var c in text)
        {
            if (c == '-') continue;
            var value = ValueOf(c);
            if (value < 0) return false;

            buffer = (buffer << 5) | value;
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                output.Add((byte)(buffer >> bits));
                buffer &= (1 << bits) - 1;
            }
        }

        if (bits >= 5 || buffer != 0) return false;
        data = [.. output];
        return true;
    }

    /// <summary>A number as Base32 digits, e.g. a short order code, optionally followed by its check symbol.</summary>
    public static string EncodeNumber(ulong value, bool withCheck = false)
    {
        Span<char> digits = stackalloc char[14];
        var i = digits.Length;
        if (withCheck) digits[--i] = CheckSymbols[(int)(value % 37)];
        do
        {
            digits[--i] = Alphabet[(int)(value & 31)];
            value >>= 5;
        } while (value > 0);
        return new string(digits[i..]);
    }

    public static bool TryDecodeNumber(string text, bool withCheck, out ulong value)
    {
        value = 0;
        var digits = text.Replace("-", "");
        if (withCheck)
        {
            if (digits.Length < 2) return false;
            var check = CheckSymbols.IndexOf(char.ToUpperInvariant(digits[^1]));
            if (check < 0) return false;
            if (!TryDecodeNumber(digits[..^1], withCheck: false, out value)) return false;
            return (int)(value % 37) == check;
        }

        if (digits.Length == 0) return false;
        foreach (var c in digits)
        {
            var digit = ValueOf(c);
            if (digit < 0 || value > ulong.MaxValue >> 5) return false;
            value = (value << 5) | (uint)digit;
        }
        return true;
    }

    private static int ValueOf(char c) => char.ToUpperInvariant(c) switch
    {
        'O' => 0,
        'I' or 'L' => 1,
        var upper => Alphabet.IndexOf(upper),
    };
}
