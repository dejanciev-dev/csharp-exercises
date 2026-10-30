namespace CSharpExercises.Codecs;

/// <summary>
/// Base64 by hand: every 3 bytes (24 bits) become 4 characters of 6 bits each, with '=' padding when the input
/// isn't a multiple of 3. The URL-safe variant swaps '+' and '/' for '-' and '_' and usually drops the padding,
/// as in JWTs. Checked against <see cref="Convert.ToBase64String(byte[])"/>.
/// </summary>
public static class Base64Codec
{
    private const string Standard = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    private const string UrlSafe = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

    public static string Encode(ReadOnlySpan<byte> data, bool urlSafe = false)
    {
        var alphabet = urlSafe ? UrlSafe : Standard;
        var output = new System.Text.StringBuilder((data.Length + 2) / 3 * 4);

        for (var i = 0; i < data.Length; i += 3)
        {
            var remaining = Math.Min(3, data.Length - i);
            var chunk = data[i] << 16 | (remaining > 1 ? data[i + 1] << 8 : 0) | (remaining > 2 ? data[i + 2] : 0);

            output.Append(alphabet[(chunk >> 18) & 63]);
            output.Append(alphabet[(chunk >> 12) & 63]);
            if (remaining > 1) output.Append(alphabet[(chunk >> 6) & 63]);
            else if (!urlSafe) output.Append('=');
            if (remaining > 2) output.Append(alphabet[chunk & 63]);
            else if (!urlSafe) output.Append('=');
        }
        return output.ToString();
    }

    /// <summary>Accepts either alphabet, with or without padding, and rejects anything else.</summary>
    public static byte[] Decode(string text)
    {
        var trimmed = text.TrimEnd('=');
        if (text.Length - trimmed.Length > 2 || trimmed.Length % 4 == 1) throw new FormatException("Invalid Base64 length.");

        var output = new List<byte>(trimmed.Length * 3 / 4);
        int buffer = 0, bits = 0;
        foreach (var c in trimmed)
        {
            buffer = (buffer << 6) | ValueOf(c);
            bits += 6;
            if (bits >= 8)
            {
                bits -= 8;
                output.Add((byte)(buffer >> bits));
                buffer &= (1 << bits) - 1;
            }
        }
        if (buffer != 0) throw new FormatException("Non-zero padding bits: not canonical Base64.");
        return [.. output];
    }

    private static int ValueOf(char c) => c switch
    {
        >= 'A' and <= 'Z' => c - 'A',
        >= 'a' and <= 'z' => c - 'a' + 26,
        >= '0' and <= '9' => c - '0' + 52,
        '+' or '-' => 62,
        '/' or '_' => 63,
        _ => throw new FormatException($"'{c}' is not a Base64 character."),
    };
}
