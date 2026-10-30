using System.Text;
using System.Text.RegularExpressions;

namespace CSharpExercises.Networking;

/// <summary>
/// URI templates (RFC 6570, levels 1–3 without modifiers): <c>/users/{id}{?page,sort}</c> expands with variables into
/// a correctly escaped URL. Operators control the style: none for simple strings, + for reserved characters kept,
/// # for fragments, / and . for path segments, ? and &amp; for query parameters. Undefined variables are skipped.
/// </summary>
public static partial class UriTemplate
{
    public static string Expand(string template, IReadOnlyDictionary<string, object?> variables) =>
        Expression().Replace(template, match =>
        {
            var body = match.Groups[1].Value;
            var op = body.Length > 0 && "+#./;?&".Contains(body[0]) ? body[0] : '\0';
            var names = (op == '\0' ? body : body[1..]).Split(',');

            var (first, separator, named, allowReserved) = op switch
            {
                '+' => ("", ",", false, true),
                '#' => ("#", ",", false, true),
                '.' => (".", ".", false, false),
                '/' => ("/", "/", false, false),
                ';' => (";", ";", true, false),
                '?' => ("?", "&", true, false),
                '&' => ("&", "&", true, false),
                _ => ("", ",", false, false),
            };

            var parts = new List<string>();
            foreach (var name in names)
            {
                if (!variables.TryGetValue(name, out var value) || value is null) continue;
                var text = value is IEnumerable<string> list && value is not string
                    ? string.Join(",", list.Select(v => Encode(v, allowReserved)))
                    : Encode(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!, allowReserved);
                parts.Add(named ? (text.Length == 0 && op == ';' ? name : $"{name}={text}") : text);
            }
            return parts.Count == 0 ? "" : first + string.Join(separator, parts);
        });

    private static string Encode(string value, bool allowReserved)
    {
        var sb = new StringBuilder();
        foreach (var b in Encoding.UTF8.GetBytes(value))
        {
            var c = (char)b;
            var unreserved = char.IsAsciiLetterOrDigit(c) || c is '-' or '.' or '_' or '~';
            var reserved = c is ':' or '/' or '?' or '#' or '[' or ']' or '@' or '!' or '$' or '&' or '\'' or '(' or ')' or '*' or '+' or ',' or ';' or '=';
            if (unreserved || (allowReserved && reserved)) sb.Append(c);
            else sb.Append('%').Append(b.ToString("X2"));
        }
        return sb.ToString();
    }

    [GeneratedRegex(@"\{([^}]*)\}")]
    private static partial Regex Expression();
}
