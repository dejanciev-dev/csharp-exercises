using System.Text;

namespace CSharpExercises.Formats;

/// <summary>
/// logfmt, the structured log line format used by Heroku, Grafana Loki and many Go services:
/// <c>level=info msg="user logged in" user_id=42 admin</c>. Readable by humans and trivially parsable by machines.
/// Values with spaces or quotes are quoted and escaped; a bare key means true. Round-tripping is the test.
/// </summary>
public static class Logfmt
{
    public static IReadOnlyList<KeyValuePair<string, string>> Parse(string line)
    {
        var pairs = new List<KeyValuePair<string, string>>();
        var i = 0;
        while (i < line.Length)
        {
            while (i < line.Length && line[i] == ' ') i++;
            if (i >= line.Length) break;

            var keyStart = i;
            while (i < line.Length && line[i] is not (' ' or '=')) i++;
            var key = line[keyStart..i];
            if (key.Length == 0) throw new FormatException($"Missing key at position {i}.");

            if (i >= line.Length || line[i] == ' ')
            {
                pairs.Add(new(key, "true")); // a bare key is a boolean flag
                continue;
            }

            i++; // '='
            if (i < line.Length && line[i] == '"')
            {
                var value = new StringBuilder();
                for (i++; ; i++)
                {
                    if (i >= line.Length) throw new FormatException($"Unterminated quote for '{key}'.");
                    if (line[i] == '"') { i++; break; }
                    if (line[i] == '\\' && i + 1 < line.Length)
                    {
                        i++;
                        value.Append(line[i] switch { 'n' => '\n', 't' => '\t', var c => c });
                    }
                    else value.Append(line[i]);
                }
                pairs.Add(new(key, value.ToString()));
            }
            else
            {
                var valueStart = i;
                while (i < line.Length && line[i] != ' ') i++;
                pairs.Add(new(key, line[valueStart..i]));
            }
        }
        return pairs;
    }

    public static string Format(IEnumerable<KeyValuePair<string, object?>> fields) =>
        string.Join(' ', fields.Select(f => $"{f.Key}={Value(f.Value)}"));

    private static string Value(object? value)
    {
        var text = value switch
        {
            null => "",
            bool b => b ? "true" : "false",
            IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "",
        };
        var needsQuotes = text.Length == 0 || text.Any(c => c is ' ' or '"' or '=' or '\\' or '\n' or '\t');
        if (!needsQuotes) return text;
        return "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\t", "\\t") + "\"";
    }
}
