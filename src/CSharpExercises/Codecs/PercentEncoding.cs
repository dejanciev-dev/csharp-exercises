using System.Text;

namespace CSharpExercises.Codecs;

/// <summary>
/// Percent-encoding for URLs (RFC 3986): unreserved characters stay as they are, everything else becomes %XX for
/// each UTF-8 byte. Query strings from HTML forms additionally use '+' for spaces, which is why decoding
/// needs to know which flavour it's reading.
/// </summary>
public static class PercentEncoding
{
    public static string Encode(string text, bool spaceAsPlus = false)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var b in Encoding.UTF8.GetBytes(text))
        {
            if (IsUnreserved(b)) sb.Append((char)b);
            else if (b == ' ' && spaceAsPlus) sb.Append('+');
            else sb.Append('%').Append(b.ToString("X2"));
        }
        return sb.ToString();
    }

    /// <summary>Collects bytes first and decodes UTF-8 at the end, since one character can span several %XX escapes.</summary>
    public static string Decode(string text, bool plusAsSpace = false)
    {
        var bytes = new List<byte>(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '%')
            {
                if (i + 2 >= text.Length || !Uri.IsHexDigit(text[i + 1]) || !Uri.IsHexDigit(text[i + 2]))
                    throw new FormatException($"Incomplete escape at position {i}.");
                bytes.Add(Convert.ToByte(text.Substring(i + 1, 2), 16));
                i += 2;
            }
            else if (c == '+' && plusAsSpace) bytes.Add((byte)' ');
            else bytes.AddRange(Encoding.UTF8.GetBytes(c.ToString()));
        }
        return new UTF8Encoding(false, throwOnInvalidBytes: true).GetString([.. bytes]);
    }

    /// <summary>Parses "a=1&amp;b=two+words&amp;a=3" into a lookup, keeping repeated keys.</summary>
    public static ILookup<string, string> ParseQuery(string query) =>
        query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .ToLookup(p => Decode(p[0], plusAsSpace: true), p => p.Length > 1 ? Decode(p[1], plusAsSpace: true) : "");

    public static string BuildQuery(IEnumerable<KeyValuePair<string, string>> parameters) =>
        string.Join('&', parameters.Select(p => $"{Encode(p.Key, true)}={Encode(p.Value, true)}"));

    private static bool IsUnreserved(byte b) =>
        b is >= (byte)'A' and <= (byte)'Z' or >= (byte)'a' and <= (byte)'z' or >= (byte)'0' and <= (byte)'9'
            or (byte)'-' or (byte)'.' or (byte)'_' or (byte)'~';
}
