namespace CSharpExercises.Codecs;

/// <summary>
/// UTF-8 by hand. Code points up to U+007F take one byte (so ASCII is valid UTF-8), and larger ones take 2–4 bytes:
/// a lead byte whose high bits say how many follow, then continuation bytes starting with 10. The decoder rejects
/// what a careless one would accept: overlong encodings, surrogates and values above U+10FFFF.
/// </summary>
public static class Utf8Codec
{
    public static byte[] Encode(string text)
    {
        var output = new List<byte>(text.Length);
        foreach (var rune in text.EnumerateRunes()) // pairs up UTF-16 surrogates into one code point
        {
            var cp = rune.Value;
            switch (cp)
            {
                case < 0x80:
                    output.Add((byte)cp);
                    break;
                case < 0x800:
                    output.Add((byte)(0xC0 | cp >> 6));
                    output.Add((byte)(0x80 | cp & 0x3F));
                    break;
                case < 0x10000:
                    output.Add((byte)(0xE0 | cp >> 12));
                    output.Add((byte)(0x80 | cp >> 6 & 0x3F));
                    output.Add((byte)(0x80 | cp & 0x3F));
                    break;
                default:
                    output.Add((byte)(0xF0 | cp >> 18));
                    output.Add((byte)(0x80 | cp >> 12 & 0x3F));
                    output.Add((byte)(0x80 | cp >> 6 & 0x3F));
                    output.Add((byte)(0x80 | cp & 0x3F));
                    break;
            }
        }
        return [.. output];
    }

    public static string Decode(ReadOnlySpan<byte> bytes)
    {
        var sb = new System.Text.StringBuilder(bytes.Length);
        for (var i = 0; i < bytes.Length;)
        {
            var lead = bytes[i];
            var (length, minimum, cp) = lead switch
            {
                < 0x80 => (1, 0, lead),
                >= 0xC2 and < 0xE0 => (2, 0x80, lead & 0x1F),   // C0 and C1 could only start overlong forms
                >= 0xE0 and < 0xF0 => (3, 0x800, lead & 0x0F),
                >= 0xF0 and < 0xF5 => (4, 0x10000, lead & 0x07),
                _ => throw Invalid(i, $"invalid lead byte 0x{lead:X2}"),
            };
            if (i + length > bytes.Length) throw Invalid(i, "truncated sequence");

            for (var k = 1; k < length; k++)
            {
                var b = bytes[i + k];
                if ((b & 0xC0) != 0x80) throw Invalid(i + k, "expected a continuation byte");
                cp = cp << 6 | b & 0x3F;
            }

            if (cp < minimum) throw Invalid(i, "overlong encoding");
            if (cp is >= 0xD800 and <= 0xDFFF) throw Invalid(i, "encoded surrogate");
            if (cp > 0x10FFFF) throw Invalid(i, "code point above U+10FFFF");

            sb.Append(char.ConvertFromUtf32(cp));
            i += length;
        }
        return sb.ToString();
    }

    /// <summary>
    /// Cuts to at most <paramref name="maxBytes"/> without splitting a character: back up while the byte at the cut
    /// is a continuation byte. Needed for database columns and protocol fields limited in bytes, not characters.
    /// </summary>
    public static ReadOnlySpan<byte> TruncateToBytes(ReadOnlySpan<byte> utf8, int maxBytes)
    {
        if (utf8.Length <= maxBytes) return utf8;
        var cut = maxBytes;
        while (cut > 0 && (utf8[cut] & 0xC0) == 0x80) cut--;
        return utf8[..cut];
    }

    private static FormatException Invalid(int position, string reason) => new($"Invalid UTF-8 at byte {position}: {reason}.");
}
