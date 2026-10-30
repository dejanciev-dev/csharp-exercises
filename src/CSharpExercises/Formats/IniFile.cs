namespace CSharpExercises.Formats;

/// <summary>
/// INI configuration files: [sections], key = value pairs, ; and # comments. Keys are case-insensitive, values
/// can be quoted to keep leading spaces or comment characters, and ${section.key} references are expanded.
/// </summary>
public sealed class IniFile
{
    private readonly Dictionary<string, Dictionary<string, string>> _sections = new(StringComparer.OrdinalIgnoreCase);

    public static IniFile Parse(string text)
    {
        var ini = new IniFile();
        var section = ini.Section("");
        var lineNumber = 0;

        foreach (var rawLine in text.Split('\n'))
        {
            lineNumber++;
            var line = rawLine.Trim();
            if (line.Length == 0 || line[0] is ';' or '#') continue;

            if (line[0] == '[')
            {
                if (line[^1] != ']') throw new FormatException($"Line {lineNumber}: unclosed section header.");
                section = ini.Section(line[1..^1].Trim());
                continue;
            }

            var equals = line.IndexOf('=');
            if (equals <= 0) throw new FormatException($"Line {lineNumber}: expected 'key = value'.");
            section[line[..equals].Trim()] = ParseValue(line[(equals + 1)..].Trim());
        }
        return ini;
    }

    public IEnumerable<string> Sections => _sections.Keys.Where(s => s.Length > 0);

    public string? Get(string section, string key)
    {
        if (!_sections.TryGetValue(section, out var values) || !values.TryGetValue(key, out var value)) return null;
        return Expand(value, depth: 0);
    }

    public T Get<T>(string section, string key, T defaultValue) where T : IParsable<T> =>
        Get(section, key) is { } text && T.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : defaultValue;

    private Dictionary<string, string> Section(string name)
    {
        if (!_sections.TryGetValue(name, out var section))
            _sections[name] = section = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        return section;
    }

    private static string ParseValue(string value)
    {
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"') return value[1..^1];

        // An unquoted value ends at an inline comment that follows whitespace.
        foreach (var marker in new[] { " ;", " #" })
        {
            var index = value.IndexOf(marker, StringComparison.Ordinal);
            if (index >= 0) value = value[..index].TrimEnd();
        }
        return value;
    }

    private string Expand(string value, int depth)
    {
        if (depth > 10) throw new InvalidOperationException("Reference cycle while expanding ${…} values.");
        var start = value.IndexOf("${", StringComparison.Ordinal);
        if (start < 0) return value;
        var end = value.IndexOf('}', start);
        if (end < 0) return value;

        var reference = value[(start + 2)..end];
        var dot = reference.LastIndexOf('.');
        var (section, key) = dot < 0 ? ("", reference) : (reference[..dot], reference[(dot + 1)..]);
        var replacement = _sections.TryGetValue(section, out var s) && s.TryGetValue(key, out var v)
            ? Expand(v, depth + 1)
            : throw new KeyNotFoundException($"Unknown reference '${{{reference}}}'.");

        return Expand(value[..start] + replacement + value[(end + 1)..], depth + 1);
    }
}
