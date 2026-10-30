using System.Text;

namespace CSharpExercises.Networking;

public sealed record HttpRequest(string Method, string Target, string Version, IReadOnlyDictionary<string, IReadOnlyList<string>> Headers, byte[] Body)
{
    public string? Header(string name) => Headers.TryGetValue(name, out var values) ? string.Join(", ", values) : null;
}

/// <summary>
/// Parsing an HTTP/1.1 request from raw bytes the way a server must: CRLF-separated request line and headers,
/// case-insensitive header names, a body framed by Content-Length or by chunked transfer encoding, and firm limits so
/// a hostile client can't send a 10 GB header. Returns null while more bytes are needed, as data arrives in pieces.
/// </summary>
public static class HttpMessageParser
{
    public const int MaxHeaderBytes = 8 * 1024;

    /// <returns>The request and how many bytes it used, or null if the message is incomplete.</returns>
    public static (HttpRequest Request, int Consumed)? TryParse(ReadOnlySpan<byte> data)
    {
        var headerEnd = data.IndexOf("\r\n\r\n"u8);
        if (headerEnd < 0)
        {
            if (data.Length > MaxHeaderBytes) throw new InvalidDataException("Request headers too large (431).");
            return null;
        }

        var lines = Encoding.ASCII.GetString(data[..headerEnd]).Split("\r\n");
        var requestLine = lines[0].Split(' ');
        if (requestLine.Length != 3 || !requestLine[2].StartsWith("HTTP/1.", StringComparison.Ordinal))
            throw new InvalidDataException($"Malformed request line '{lines[0]}' (400).");

        var headers = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines.Skip(1))
        {
            var colon = line.IndexOf(':');
            if (colon <= 0 || line[..colon].Any(char.IsWhiteSpace)) throw new InvalidDataException($"Malformed header '{line}' (400).");
            var name = line[..colon];
            if (!headers.TryGetValue(name, out var values)) headers[name] = values = [];
            values.Add(line[(colon + 1)..].Trim());
        }

        var bodyStart = headerEnd + 4;
        var rest = data[bodyStart..];
        byte[] body;
        int bodyLength;

        if (headers.TryGetValue("Transfer-Encoding", out var te) && te.Any(v => v.Contains("chunked", StringComparison.OrdinalIgnoreCase)))
        {
            if (headers.ContainsKey("Content-Length")) throw new InvalidDataException("Both Content-Length and chunked: request smuggling risk (400).");
            if (DecodeChunked(rest) is not { } chunked) return null;
            (body, bodyLength) = chunked;
        }
        else if (headers.TryGetValue("Content-Length", out var cl))
        {
            if (cl.Distinct().Count() != 1 || !int.TryParse(cl[0], out var length) || length < 0)
                throw new InvalidDataException("Invalid Content-Length (400).");
            if (rest.Length < length) return null;
            body = rest[..length].ToArray();
            bodyLength = length;
        }
        else
        {
            body = [];
            bodyLength = 0;
        }

        var request = new HttpRequest(requestLine[0], requestLine[1], requestLine[2],
            headers.ToDictionary(h => h.Key, h => (IReadOnlyList<string>)h.Value, StringComparer.OrdinalIgnoreCase), body);
        return (request, bodyStart + bodyLength);
    }

    /// <summary>Chunked encoding: "size-in-hex CRLF data CRLF" repeated, ending with a zero-size chunk.</summary>
    private static (byte[] Body, int Consumed)? DecodeChunked(ReadOnlySpan<byte> data)
    {
        var body = new List<byte>();
        var position = 0;
        while (true)
        {
            var lineEnd = data[position..].IndexOf("\r\n"u8);
            if (lineEnd < 0) return null;
            var sizeText = Encoding.ASCII.GetString(data.Slice(position, lineEnd)).Split(';')[0]; // ignore chunk extensions
            if (!int.TryParse(sizeText, System.Globalization.NumberStyles.HexNumber, null, out var size) || size < 0)
                throw new InvalidDataException($"Invalid chunk size '{sizeText}' (400).");
            position += lineEnd + 2;

            if (data.Length < position + size + 2) return null;
            if (size == 0) return ([.. body], position + 2);
            body.AddRange(data.Slice(position, size));
            if (!data.Slice(position + size, 2).SequenceEqual("\r\n"u8)) throw new InvalidDataException("Chunk not followed by CRLF (400).");
            position += size + 2;
        }
    }
}
