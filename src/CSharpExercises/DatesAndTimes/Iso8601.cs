using System.Globalization;
using System.Text.RegularExpressions;

namespace CSharpExercises.DatesAndTimes;

/// <summary>
/// Two parts of ISO 8601 that trip people up. Week numbers: week 1 is the week containing the year's first Thursday, so
/// 29 December can be in week 1 of the next year and 1 January in week 52 or 53 of the previous one (.NET's
/// <see cref="ISOWeek"/> does this correctly; this is the arithmetic behind it). Durations: "P1DT2H30M" style, with
/// years and months kept separate because their length depends on the date they're added to.
/// </summary>
public static partial class Iso8601
{
    public static (int Year, int Week) WeekOf(DateOnly date)
    {
        // The Thursday of the same Monday-based week decides which year the week belongs to.
        var dayOfWeek = ((int)date.DayOfWeek + 6) % 7; // Monday = 0
        var thursday = date.AddDays(3 - dayOfWeek);
        return (thursday.Year, (thursday.DayOfYear - 1) / 7 + 1);
    }

    public static DateOnly MondayOfWeek(int year, int week)
    {
        var jan4 = new DateOnly(year, 1, 4); // always in week 1
        var mondayOfWeek1 = jan4.AddDays(-(((int)jan4.DayOfWeek + 6) % 7));
        return mondayOfWeek1.AddDays(7 * (week - 1));
    }

    public static int WeeksInYear(int year) => WeekOf(new DateOnly(year, 12, 28)).Week; // 28 Dec is always in the last week

    public static string FormatWeek(DateOnly date) => WeekOf(date) is var (y, w) ? $"{y}-W{w:00}-{((int)date.DayOfWeek + 6) % 7 + 1}" : "";

    public sealed record Duration(int Years, int Months, int Days, TimeSpan Time)
    {
        /// <summary>Adds calendar parts first (month ends clamp: 31 Jan + P1M = 28/29 Feb), then the exact time.</summary>
        public DateTime AddTo(DateTime start) => start.AddYears(Years).AddMonths(Months).AddDays(Days).Add(Time);

        public override string ToString()
        {
            var date = $"{(Years > 0 ? $"{Years}Y" : "")}{(Months > 0 ? $"{Months}M" : "")}{(Days > 0 ? $"{Days}D" : "")}";
            var time = $"{(Time.Hours > 0 ? $"{Time.Hours}H" : "")}{(Time.Minutes > 0 ? $"{Time.Minutes}M" : "")}{(Time.Seconds > 0 ? $"{Time.Seconds}S" : "")}";
            return date.Length + time.Length == 0 ? "PT0S" : $"P{date}{(time.Length > 0 ? "T" + time : "")}";
        }
    }

    public static Duration ParseDuration(string text)
    {
        var m = DurationPattern().Match(text);
        if (!m.Success || text is "P" or "PT" || text.EndsWith('T')) throw new FormatException($"'{text}' is not an ISO 8601 duration.");

        int Int(string group) => m.Groups[group].Success ? int.Parse(m.Groups[group].Value) : 0;
        double Num(string group) => m.Groups[group].Success ? double.Parse(m.Groups[group].Value, CultureInfo.InvariantCulture) : 0;

        var time = TimeSpan.FromHours(Num("h")) + TimeSpan.FromMinutes(Num("min")) + TimeSpan.FromSeconds(Num("s"));
        return new Duration(Int("y"), Int("mo"), Int("d") + 7 * Int("w"), time);
    }

    [GeneratedRegex(@"^P(?:(?<y>\d+)Y)?(?:(?<mo>\d+)M)?(?:(?<w>\d+)W)?(?:(?<d>\d+)D)?(?:T(?:(?<h>\d+(?:\.\d+)?)H)?(?:(?<min>\d+(?:\.\d+)?)M)?(?:(?<s>\d+(?:\.\d+)?)S)?)?$")]
    private static partial Regex DurationPattern();
}
