using System.Globalization;
using System.Text.RegularExpressions;

namespace CSharpExercises.Formats;

/// <summary>
/// Sizes and durations for people: "1.5 GiB" and "2h30m" in both directions. Decimal units (kB = 1000) are what
/// disk vendors use, binary units (KiB = 1024) what operating systems usually mean, and mixing them up is why a
/// "1 TB" drive shows up as 931 GB.
/// </summary>
public static partial class HumanReadable
{
    private static readonly string[] DecimalUnits = ["B", "kB", "MB", "GB", "TB", "PB", "EB"];
    private static readonly string[] BinaryUnits = ["B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB"];

    public static string FormatBytes(long bytes, bool binary = true)
    {
        var (step, units) = binary ? (1024.0, BinaryUnits) : (1000.0, DecimalUnits);
        var value = (double)Math.Abs(bytes);
        var unit = 0;
        while (value >= step && unit < units.Length - 1)
        {
            value /= step;
            unit++;
        }
        var sign = bytes < 0 ? "-" : "";
        return unit == 0
            ? $"{sign}{value} B"
            : $"{sign}{value.ToString(value < 10 ? "0.##" : value < 100 ? "0.#" : "0", CultureInfo.InvariantCulture)} {units[unit]}";
    }

    public static long ParseBytes(string text)
    {
        var match = SizePattern().Match(text.Trim());
        if (!match.Success) throw new FormatException($"'{text}' is not a size.");

        var number = double.Parse(match.Groups["number"].Value, CultureInfo.InvariantCulture);
        var unit = match.Groups["unit"].Value;
        var power = unit.Length == 0 ? 0 : "BKMGTPE".IndexOf(char.ToUpperInvariant(unit[0]));
        if (unit.Equals("B", StringComparison.OrdinalIgnoreCase)) power = 0;
        var multiplier = Math.Pow(unit.Contains('i') ? 1024 : 1000, power);
        return checked((long)Math.Round(number * multiplier));
    }

    /// <summary>"1d2h3m4s" style, largest unit first, skipping zero parts.</summary>
    public static string FormatDuration(TimeSpan duration)
    {
        if (duration == TimeSpan.Zero) return "0s";
        var parts = new List<string>();
        if (duration < TimeSpan.Zero)
        {
            parts.Add("-");
            duration = duration.Negate();
        }
        if (duration.Days > 0) parts.Add($"{duration.Days}d");
        if (duration.Hours > 0) parts.Add($"{duration.Hours}h");
        if (duration.Minutes > 0) parts.Add($"{duration.Minutes}m");
        if (duration.Seconds > 0) parts.Add($"{duration.Seconds}s");
        if (duration.Milliseconds > 0) parts.Add($"{duration.Milliseconds}ms");
        return string.Concat(parts);
    }

    public static TimeSpan ParseDuration(string text)
    {
        var matches = DurationPattern().Matches(text.Replace(" ", ""));
        if (matches.Count == 0 || matches.Sum(m => m.Length) != text.Replace(" ", "").Length)
            throw new FormatException($"'{text}' is not a duration like 1h30m.");

        return matches.Aggregate(TimeSpan.Zero, (total, m) =>
        {
            var value = double.Parse(m.Groups["value"].Value, CultureInfo.InvariantCulture);
            return total + m.Groups["unit"].Value switch
            {
                "d" => TimeSpan.FromDays(value),
                "h" => TimeSpan.FromHours(value),
                "m" => TimeSpan.FromMinutes(value),
                "s" => TimeSpan.FromSeconds(value),
                "ms" => TimeSpan.FromMilliseconds(value),
                var u => throw new FormatException($"Unknown unit '{u}'."),
            };
        });
    }

    [GeneratedRegex(@"^(?<number>\d+(\.\d+)?)\s*(?<unit>([kKMGTPE]i?)?B?)$")]
    private static partial Regex SizePattern();

    [GeneratedRegex(@"(?<value>\d+(\.\d+)?)(?<unit>ms|d|h|m|s)")]
    private static partial Regex DurationPattern();
}
