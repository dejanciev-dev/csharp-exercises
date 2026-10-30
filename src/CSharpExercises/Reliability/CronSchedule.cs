namespace CSharpExercises.Reliability;

/// <summary>
/// A parser for standard five-field cron expressions ("minute hour day-of-month month day-of-week"), supporting
/// *, lists (1,15), ranges (9-17) and steps (*/15, 0-30/10). Each field becomes a set of allowed values; the next
/// run time is found by stepping forward, skipping whole days or hours when they can't match.
/// </summary>
public sealed class CronSchedule
{
    private readonly bool[] _minutes, _hours, _days, _months, _weekdays;
    private readonly bool _anyDay, _anyWeekday;

    private CronSchedule(string expression)
    {
        var fields = expression.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length != 5) throw new FormatException($"Expected 5 fields but found {fields.Length} in '{expression}'.");

        _minutes = ParseField(fields[0], 0, 59, "minute");
        _hours = ParseField(fields[1], 0, 23, "hour");
        _days = ParseField(fields[2], 1, 31, "day of month");
        _months = ParseField(fields[3], 1, 12, "month");
        _weekdays = ParseField(fields[4].Replace("7", "0"), 0, 6, "day of week"); // 7 is also Sunday
        _anyDay = fields[2] == "*";
        _anyWeekday = fields[4] == "*";
    }

    public static CronSchedule Parse(string expression) => new(expression);

    /// <summary>The first matching minute strictly after <paramref name="after"/>, or null if none within 5 years.</summary>
    public DateTime? Next(DateTime after)
    {
        var t = new DateTime(after.Year, after.Month, after.Day, after.Hour, after.Minute, 0, after.Kind).AddMinutes(1);
        var limit = after.AddYears(5);

        while (t < limit)
        {
            if (!_months[t.Month]) { t = new DateTime(t.Year, t.Month, 1, 0, 0, 0, t.Kind).AddMonths(1); continue; }
            if (!DayMatches(t)) { t = t.Date.AddDays(1); continue; }
            if (!_hours[t.Hour]) { t = t.Date.AddHours(t.Hour + 1); continue; }
            if (!_minutes[t.Minute]) { t = t.AddMinutes(1); continue; }
            return t;
        }
        return null;
    }

    public IEnumerable<DateTime> Occurrences(DateTime after)
    {
        for (var next = Next(after); next is { } n; next = Next(n)) yield return n;
    }

    /// <summary>
    /// Cron's odd rule: if both day-of-month and day-of-week are restricted, a day matching either one counts.
    /// "0 0 13 * 5" runs on every 13th and every Friday, not only on Friday the 13th.
    /// </summary>
    private bool DayMatches(DateTime t)
    {
        var day = _days[t.Day];
        var weekday = _weekdays[(int)t.DayOfWeek];
        return (_anyDay, _anyWeekday) switch
        {
            (true, true) => true,
            (false, true) => day,
            (true, false) => weekday,
            (false, false) => day || weekday,
        };
    }

    private static bool[] ParseField(string field, int min, int max, string name)
    {
        var allowed = new bool[max + 1];
        foreach (var part in field.Split(','))
        {
            var (rangePart, step) = part.Split('/') switch
            {
                [var r] => (r, 1),
                [var r, var s] when int.TryParse(s, out var n) && n > 0 => (r, n),
                _ => throw new FormatException($"Invalid step in {name} field '{part}'."),
            };

            var (from, to) = rangePart switch
            {
                "*" => (min, max),
                _ when rangePart.Split('-') is [var a, var b] => (ParseNumber(a), ParseNumber(b)),
                _ => (ParseNumber(rangePart), step > 1 ? max : ParseNumber(rangePart)),
            };
            if (from < min || to > max || from > to) throw new FormatException($"{name} value '{part}' is outside {min}-{max}.");

            for (var v = from; v <= to; v += step) allowed[v] = true;
        }
        return allowed;

        int ParseNumber(string s) => int.TryParse(s, out var n) ? n : throw new FormatException($"Invalid {name} value '{s}'.");
    }
}
