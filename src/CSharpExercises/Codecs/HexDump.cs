using System.Text;

namespace CSharpExercises.Codecs;

/// <summary>
/// Hexadecimal encoding with plain nibble arithmetic, and a classic hex dump like `xxd` or `hexdump -C`:
/// an offset column, 16 bytes in hex, then the printable ASCII characters.
/// </summary>
public static class HexDump
{
    public static string ToHex(ReadOnlySpan<byte> data, bool upperCase = false)
    {
        var letterOffset = upperCase ? 'A' - 10 : 'a' - 10;
        return string.Create(data.Length * 2, (data.ToArray(), letterOffset), static (span, state) =>
        {
            for (var i = 0; i < state.Item1.Length; i++)
            {
                span[2 * i] = Digit(state.Item1[i] >> 4, state.letterOffset);
                span[2 * i + 1] = Digit(state.Item1[i] & 0xF, state.letterOffset);
            }
        });
    }

    public static byte[] FromHex(string hex)
    {
        if (hex.Length % 2 != 0) throw new FormatException("Hex strings need an even number of digits.");
        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)(Nibble(hex[2 * i]) << 4 | Nibble(hex[2 * i + 1]));
        return bytes;
    }

    public static string Dump(ReadOnlySpan<byte> data, int bytesPerLine = 16)
    {
        var sb = new StringBuilder();
        for (var offset = 0; offset < data.Length; offset += bytesPerLine)
        {
            var line = data.Slice(offset, Math.Min(bytesPerLine, data.Length - offset));
            sb.Append(offset.ToString("x8")).Append("  ");
            for (var i = 0; i < bytesPerLine; i++)
            {
                sb.Append(i < line.Length ? ToHex(line.Slice(i, 1)) : "  ").Append(' ');
                if (i == bytesPerLine / 2 - 1) sb.Append(' ');
            }
            sb.Append(" |");
            foreach (var b in line) sb.Append(b is >= 0x20 and < 0x7F ? (char)b : '.');
            sb.Append('|').Append('\n');
        }
        return sb.ToString();
    }

    private static char Digit(int value, int letterOffset) => (char)(value < 10 ? '0' + value : letterOffset + value);

    private static int Nibble(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => throw new FormatException($"'{c}' is not a hex digit."),
    };
}
