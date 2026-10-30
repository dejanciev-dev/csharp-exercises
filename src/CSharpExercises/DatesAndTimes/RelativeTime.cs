namespace CSharpExercises.DatesAndTimes;

/// <summary>
/// "just now", "5 minutes ago", "yesterday", "in 3 weeks": the human-friendly timestamps in feeds and chats. The
/// thresholds round the way people speak (45 minutes is "an hour ago" territory), and calendar words like "yesterday"
/// compare dates, not 24-hour spans, so 23:59 → 00:01 is already "yesterday".
/// </summary>
public static class RelativeTime
{
    public static string Describe(DateTimeOffset time, DateTimeOffset now)
    {
        var delta = now - time;
        var future = delta < TimeSpan.Zero;
        var span = future ? -delta : delta;

        string Phrase(double value, string unit)
        {
            var n = (int)Math.Round(value);
            var text = $"{n} {unit}{(n == 1 ? "" : "s")}";
            return future ? $"in {text}" : $"{text} ago";
        }

        if (span < TimeSpan.FromSeconds(45)) return future ? "in a moment" : "just now";
        if (span < TimeSpan.FromMinutes(45)) return Phrase(Math.Max(1, span.TotalMinutes), "minute");
        if (span < TimeSpan.FromHours(22)) return Phrase(Math.Max(1, span.TotalHours), "hour");

        var days = DateOnly.FromDateTime(now.DateTime).DayNumber - DateOnly.FromDateTime(time.DateTime).DayNumber;
        if (days == 1) return "yesterday";
        if (days == -1) return "tomorrow";
        if (Math.Abs(days) < 7) return Phrase(Math.Abs(days), "day");
        if (Math.Abs(days) < 30) return Phrase(Math.Abs(days) / 7.0, "week");
        if (Math.Abs(days) < 365) return Phrase(Math.Abs(days) / 30.44, "month");
        return Phrase(Math.Abs(days) / 365.25, "year");
    }

    /// <summary>A compact form for tight spaces: "5m", "3h", "2d".</summary>
    public static string Short(TimeSpan span) => span.Duration() switch
    {
        var s when s < TimeSpan.FromMinutes(1) => $"{(int)s.TotalSeconds}s",
        var s when s < TimeSpan.FromHours(1) => $"{(int)s.TotalMinutes}m",
        var s when s < TimeSpan.FromDays(1) => $"{(int)s.TotalHours}h",
        var s when s < TimeSpan.FromDays(365) => $"{(int)s.TotalDays}d",
        var s => $"{(int)(s.TotalDays / 365.25)}y",
    };
}
