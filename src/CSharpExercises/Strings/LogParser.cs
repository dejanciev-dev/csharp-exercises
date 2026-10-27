using System.Globalization;
using System.Text.RegularExpressions;

namespace CSharpExercises.Strings;

public enum LogLevel { Trace, Debug, Info, Warn, Error, Fatal }

public sealed record LogEntry(
    DateTime Timestamp, LogLevel Level, string Source, string Message, IReadOnlyDictionary<string, string> Properties);

/// <summary>
/// Parses lines such as <c>2026-10-27 14:03:12.123 [ERROR] Payments: Charge failed (orderId=42, user="Ana M")</c>.
/// </summary>
/// <remarks>
/// <see cref="GeneratedRegexAttribute"/> makes the compiler emit the matching code at build time: no parsing of the
/// pattern at startup, no runtime compilation, and a pattern typo is a build error instead of a crash.
/// </remarks>
public static partial class LogParser
{
    [GeneratedRegex("""^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}(?:\.\d{1,3})?) \[(?<level>[A-Za-z]+)\] (?<source>[\w.]+): (?<message>.*)$""")]
    private static partial Regex LinePattern();

    // key=value or key="value with spaces"; both alternatives use the same group name, so either fills "value".
    [GeneratedRegex("""(?<key>\w+)=(?:"(?<value>[^"]*)"|(?<value>[^\s,)]+))""")]
    private static partial Regex PropertyPattern();

    [GeneratedRegex("""\b(?<first>[A-Za-z0-9])[A-Za-z0-9._%+-]*@(?<domain>[A-Za-z0-9.-]+\.[A-Za-z]{2,})\b""")]
    private static partial Regex EmailPattern();

    // 16 digits, optionally grouped with spaces or dashes; the last 4 are kept.
    [GeneratedRegex("""\b(?:\d[ -]?){12}(?<last>\d{4})\b""")]
    private static partial Regex CardNumberPattern();

    private static readonly string[] TimestampFormats = ["yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm:ss.FFF"];

    /// <summary>Returns null for lines that do not match, such as stack trace continuation lines.</summary>
    public static LogEntry? TryParse(string line)
    {
        var match = LinePattern().Match(line);
        if (!match.Success
            || !Enum.TryParse<LogLevel>(match.Groups["level"].Value, ignoreCase: true, out var level)
            || !DateTime.TryParseExact(match.Groups["timestamp"].Value, TimestampFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp))
        {
            return null;
        }

        var message = match.Groups["message"].Value;
        var properties = PropertyPattern().Matches(message)
            .GroupBy(m => m.Groups["key"].Value)
            .ToDictionary(g => g.Key, g => g.Last().Groups["value"].Value); // a repeated key keeps its last value

        return new LogEntry(timestamp, level, match.Groups["source"].Value, message, properties);
    }

    /// <summary>Parses every line, and reports the 1-based numbers of the lines it could not parse.</summary>
    public static (List<LogEntry> Entries, List<int> UnparsedLines) ParseAll(IEnumerable<string> lines)
    {
        List<LogEntry> entries = [];
        List<int> unparsed = [];
        var number = 0;

        foreach (var line in lines)
        {
            number++;
            if (TryParse(line) is { } entry) entries.Add(entry);
            else if (!string.IsNullOrWhiteSpace(line)) unparsed.Add(number);
        }

        return (entries, unparsed);
    }

    /// <summary>Errors per source, worst first: a quick "where is it breaking?" summary.</summary>
    public static IReadOnlyList<(string Source, int Errors)> ErrorHotspots(IEnumerable<LogEntry> entries) =>
        [.. entries.Where(e => e.Level >= LogLevel.Error)
                   .CountBy(e => e.Source)
                   .OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal)
                   .Select(p => (p.Key, p.Value))];

    /// <summary>Masks email addresses and card numbers before a line is stored or shared.</summary>
    public static string Redact(string line)
    {
        var withoutEmails = EmailPattern().Replace(line, "${first}***@${domain}");
        return CardNumberPattern().Replace(withoutEmails, "**** **** **** ${last}");
    }
}
