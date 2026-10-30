namespace CSharpExercises.DatesAndTimes;

public enum Frequency { Daily, Weekly, Monthly }

/// <summary>
/// A subset of iCalendar's RRULE (RFC 5545), the format behind every calendar's repeating events:
/// "FREQ=WEEKLY;INTERVAL=2;BYDAY=MO,WE;COUNT=6". Monthly rules on the 31st skip months that don't have one, as the RFC
/// requires, instead of silently moving to the 30th. Occurrences are generated lazily so open-ended rules are fine.
/// </summary>
public sealed record RecurrenceRule(Frequency Frequency, int Interval = 1, IReadOnlySet<DayOfWeek>? ByDay = null, int? Count = null, DateOnly? Until = null)
{
    private static readonly Dictionary<string, DayOfWeek> Days = new()
    {
        ["MO"] = DayOfWeek.Monday, ["TU"] = DayOfWeek.Tuesday, ["WE"] = DayOfWeek.Wednesday, ["TH"] = DayOfWeek.Thursday,
        ["FR"] = DayOfWeek.Friday, ["SA"] = DayOfWeek.Saturday, ["SU"] = DayOfWeek.Sunday,
    };

    public static RecurrenceRule Parse(string rule)
    {
        var parts = rule.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=') is [var k, var v] ? (k.ToUpperInvariant(), v) : throw new FormatException($"Bad part '{p}'."))
            .ToDictionary(p => p.Item1, p => p.v);

        var frequency = parts.GetValueOrDefault("FREQ") switch
        {
            "DAILY" => Frequency.Daily,
            "WEEKLY" => Frequency.Weekly,
            "MONTHLY" => Frequency.Monthly,
            var f => throw new FormatException($"Unsupported FREQ '{f}'."),
        };
        if (parts.ContainsKey("COUNT") && parts.ContainsKey("UNTIL")) throw new FormatException("COUNT and UNTIL can't both be set.");

        return new RecurrenceRule(
            frequency,
            parts.TryGetValue("INTERVAL", out var i) ? int.Parse(i) : 1,
            parts.TryGetValue("BYDAY", out var d) ? d.Split(',').Select(x => Days.TryGetValue(x, out var day) ? day : throw new FormatException($"Bad day '{x}'.")).ToHashSet() : null,
            parts.TryGetValue("COUNT", out var c) ? int.Parse(c) : null,
            parts.TryGetValue("UNTIL", out var u) ? DateOnly.ParseExact(u, "yyyyMMdd") : null);
    }

    public IEnumerable<DateOnly> Occurrences(DateOnly start)
    {
        var produced = 0;
        foreach (var date in Candidates(start))
        {
            if (Until is { } until && date > until) yield break;
            yield return date;
            if (++produced == Count) yield break;
        }
    }

    private IEnumerable<DateOnly> Candidates(DateOnly start)
    {
        switch (Frequency)
        {
            case Frequency.Daily:
                for (var d = start; ; d = d.AddDays(Interval))
                    if (ByDay is null || ByDay.Contains(d.DayOfWeek)) yield return d;

            case Frequency.Weekly:
                var days = ByDay ?? new HashSet<DayOfWeek> { start.DayOfWeek };
                var weekStart = start.AddDays(-(((int)start.DayOfWeek + 6) % 7)); // weeks start on Monday
                for (var week = weekStart; ; week = week.AddDays(7 * Interval))
                    for (var offset = 0; offset < 7; offset++)
                    {
                        var d = week.AddDays(offset);
                        if (d >= start && days.Contains(d.DayOfWeek)) yield return d;
                    }

            case Frequency.Monthly:
                for (var month = 0; ; month += Interval)
                {
                    var first = new DateOnly(start.Year, start.Month, 1).AddMonths(month);
                    if (start.Day <= DateTime.DaysInMonth(first.Year, first.Month)) yield return first.AddDays(start.Day - 1);
                }
        }
    }
}
