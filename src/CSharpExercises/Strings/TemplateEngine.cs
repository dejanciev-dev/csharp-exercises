using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace CSharpExercises.Strings;

/// <summary>
/// A small template language: <c>{{ name }}</c> placeholders with filters chained by <c>|</c>, e.g.
/// <c>Hello {{ name | trim | upper }}, you have {{ count }} {{ count | plural:message,messages }}.</c>
/// Built on <see cref="Regex.Replace(string, MatchEvaluator)"/>: the regex finds each placeholder and a function decides
/// what replaces it.
/// </summary>
public static partial class TemplateEngine
{
    [GeneratedRegex("""\{\{\s*(?<name>\w+)\s*(?<filters>(?:\|\s*\w+(?::[^|}]*)?\s*)*)\}\}""")]
    private static partial Regex Placeholder();

    /// <summary>
    /// Fills in the template. A value that is missing (and not given a <c>default</c>) throws, so a typo in a
    /// placeholder name fails loudly instead of sending an email that says "Hello , ...".
    /// </summary>
    public static string Render(string template, IReadOnlyDictionary<string, object?> values) =>
        Placeholder().Replace(template, match =>
        {
            var name = match.Groups["name"].Value;
            values.TryGetValue(name, out var value);
            var text = value switch
            {
                null => null,
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString(),
            };

            foreach (var filter in match.Groups["filters"].Value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var (filterName, argument) = filter.IndexOf(':') is var colon and >= 0
                    ? (filter[..colon].Trim(), filter[(colon + 1)..].Trim())
                    : (filter, "");
                text = Apply(filterName, argument, text, value);
            }

            return text ?? throw new KeyNotFoundException($"Template value '{name}' is missing.");
        });

    /// <summary>The names a template uses, so callers can check they have all the values before rendering.</summary>
    public static IReadOnlySet<string> PlaceholderNames(string template) =>
        Placeholder().Matches(template).Select(m => m.Groups["name"].Value).ToHashSet();

    private static string? Apply(string filter, string argument, string? text, object? value) => filter switch
    {
        "upper" => text?.ToUpperInvariant(),
        "lower" => text?.ToLowerInvariant(),
        "trim" => text?.Trim(),
        "default" => string.IsNullOrEmpty(text) ? argument : text,
        "html" => text is null ? null : WebUtility.HtmlEncode(text),
        "truncate" when int.TryParse(argument, out var length) => text is null ? null : UnicodeText.Truncate(text, length),
        "plural" when argument.Split(',') is [var one, var many] => IsOne(value) ? one : many,
        _ => throw new FormatException($"Unknown filter or bad argument: '{filter}{(argument.Length > 0 ? ":" + argument : "")}'."),
    };

    private static bool IsOne(object? value) => value switch
    {
        int i => i == 1,
        long l => l == 1,
        decimal d => d == 1,
        double d => d == 1,
        string s => s == "1",
        _ => false,
    };
}
