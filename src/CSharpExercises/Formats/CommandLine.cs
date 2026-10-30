namespace CSharpExercises.Formats;

/// <summary>
/// Parsing command-line arguments: --name value, --name=value, boolean --flags, bundled short flags (-vf),
/// and "--" to stop option parsing. Also the opposite direction, splitting one command string into arguments with
/// shell-like quoting.
/// </summary>
public sealed class CommandLine
{
    private readonly Dictionary<string, List<string>> _options = new(StringComparer.Ordinal);
    private readonly HashSet<string> _flags = new(StringComparer.Ordinal);

    public IReadOnlyList<string> Positional { get; }

    private CommandLine(List<string> positional) => Positional = positional;

    /// <param name="booleanFlags">Options that never take a value, so "--verbose file.txt" doesn't swallow the file name.</param>
    public static CommandLine Parse(IReadOnlyList<string> args, IEnumerable<string>? booleanFlags = null)
    {
        var flags = new HashSet<string>(booleanFlags ?? []);
        var positional = new List<string>();
        var result = new CommandLine(positional);

        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            if (arg == "--")
            {
                positional.AddRange(args.Skip(i + 1));
                break;
            }

            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                var (name, value) = arg.IndexOf('=') is var eq and > 0 ? (arg[2..eq], arg[(eq + 1)..]) : (arg[2..], null);
                if (value is null && !flags.Contains(name) && i + 1 < args.Count && !IsOption(args[i + 1]))
                    value = args[++i];
                result.Set(name, value);
            }
            else if (IsOption(arg))
            {
                foreach (var c in arg[1..]) result.Set(c.ToString(), null);
            }
            else
            {
                positional.Add(arg);
            }
        }
        return result;
    }

    public bool Has(string name) => _flags.Contains(name) || _options.ContainsKey(name);

    public string? Get(string name) => _options.TryGetValue(name, out var values) ? values[^1] : null;

    public IReadOnlyList<string> GetAll(string name) => _options.TryGetValue(name, out var values) ? values : [];

    public T Get<T>(string name, T defaultValue) where T : IParsable<T> =>
        Get(name) is { } text
            ? T.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, out var value)
                ? value
                : throw new FormatException($"Option --{name}: '{text}' is not a valid {typeof(T).Name}.")
            : defaultValue;

    /// <summary>
    /// Splits "copy 'my file.txt' \"other dir\"/x" into arguments: whitespace separates, quotes group, a backslash
    /// escapes the next character outside single quotes.
    /// </summary>
    public static IReadOnlyList<string> Split(string commandLine)
    {
        var args = new List<string>();
        var current = new System.Text.StringBuilder();
        var inArgument = false;
        char? quote = null;

        for (var i = 0; i < commandLine.Length; i++)
        {
            var c = commandLine[i];
            if (quote is { } q)
            {
                if (c == q) quote = null;
                else if (c == '\\' && q == '"' && i + 1 < commandLine.Length) current.Append(commandLine[++i]);
                else current.Append(c);
            }
            else if (c is '"' or '\'') { quote = c; inArgument = true; }
            else if (c == '\\' && i + 1 < commandLine.Length) { current.Append(commandLine[++i]); inArgument = true; }
            else if (char.IsWhiteSpace(c))
            {
                if (inArgument) args.Add(current.ToString());
                current.Clear();
                inArgument = false;
            }
            else { current.Append(c); inArgument = true; }
        }

        if (quote is not null) throw new FormatException("Unterminated quote.");
        if (inArgument) args.Add(current.ToString());
        return args;
    }

    /// <summary>-5 is a negative number, not an option.</summary>
    private static bool IsOption(string arg) =>
        arg.StartsWith('-') && arg.Length > 1 && !double.TryParse(arg, System.Globalization.CultureInfo.InvariantCulture, out _);

    private void Set(string name, string? value)
    {
        if (value is null)
        {
            _flags.Add(name);
            return;
        }
        if (!_options.TryGetValue(name, out var values)) _options[name] = values = [];
        values.Add(value);
    }
}
