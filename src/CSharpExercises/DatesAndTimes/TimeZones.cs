namespace CSharpExercises.DatesAndTimes;

public enum LocalTimeKind { Normal, Skipped, Ambiguous }

/// <summary>
/// Converting local times to instants safely. Around daylight-saving changes some local times do not exist
/// (clocks jump from 02:00 to 03:00, so 02:30 is skipped) and some exist twice (clocks fall back from 03:00 to 02:00,
/// so 02:30 happens twice). <see cref="TimeZoneInfo.ConvertTimeToUtc(DateTime, TimeZoneInfo)"/> throws for the first
/// and silently picks one for the second.
/// </summary>
public static class TimeZones
{
    /// <summary>Accepts IANA ids such as <c>Europe/Berlin</c> on every OS (.NET 6+ converts them on Windows).</summary>
    public static TimeZoneInfo Find(string id) => TimeZoneInfo.FindSystemTimeZoneById(id);

    public static LocalTimeKind Classify(DateTime local, TimeZoneInfo zone) =>
        zone.IsInvalidTime(local) ? LocalTimeKind.Skipped
        : zone.IsAmbiguousTime(local) ? LocalTimeKind.Ambiguous
        : LocalTimeKind.Normal;

    /// <summary>
    /// Turns a wall-clock time in <paramref name="zone"/> into an exact instant, with explicit rules for the DST edge cases:
    /// a skipped time moves forward by the size of the gap (02:30 becomes 03:30, what a person whose alarm was set for
    /// 02:30 would experience), and an ambiguous time takes the earlier of the two instants.
    /// </summary>
    public static DateTimeOffset ToInstant(DateTime local, TimeZoneInfo zone)
    {
        local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);

        switch (Classify(local, zone))
        {
            case LocalTimeKind.Skipped:
                // The offset before the gap tells us which instant the clock "should" have shown.
                var before = zone.GetUtcOffset(local.AddHours(-12));
                var instant = new DateTimeOffset(local, before);
                return TimeZoneInfo.ConvertTime(instant, zone);

            case LocalTimeKind.Ambiguous:
                // The earlier instant is the one with the larger offset (still on summer time).
                var offset = zone.GetAmbiguousTimeOffsets(local).Max();
                return new DateTimeOffset(local, offset);

            default:
                return new DateTimeOffset(local, zone.GetUtcOffset(local));
        }
    }

    /// <summary>
    /// "Same time tomorrow" in local time. Across a DST change this is 23 or 25 hours later, not 24,
    /// which is what adding <c>TimeSpan.FromDays(1)</c> to an instant would give.
    /// </summary>
    public static DateTimeOffset AddLocalDays(DateTimeOffset instant, int days, TimeZoneInfo zone)
    {
        var local = TimeZoneInfo.ConvertTime(instant, zone).DateTime;
        return ToInstant(local.AddDays(days), zone);
    }

    /// <summary>
    /// The hours (on the hour, in UTC) of <paramref name="date"/> when every zone is inside its local working hours:
    /// the slots in which a meeting is fair to everyone.
    /// </summary>
    public static IReadOnlyList<DateTimeOffset> SharedWorkingHours(
        DateOnly date, IEnumerable<TimeZoneInfo> zones, TimeOnly workStart, TimeOnly workEnd)
    {
        if (workEnd - workStart < TimeSpan.FromHours(1) || workEnd < workStart)
            throw new ArgumentException("Working hours must span at least one hour within a day.");

        var zoneList = zones.ToList();
        var latestStart = workEnd.AddHours(-1); // a one-hour meeting must end by workEnd
        var dayStart = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        // Look at a 48-hour window: the given date in UTC can fall on the previous or next day locally.
        return [.. Enumerable.Range(-12, 48)
            .Select(h => dayStart.AddHours(h))
            .Where(slot => zoneList.All(zone =>
            {
                var local = TimeZoneInfo.ConvertTime(slot, zone).DateTime;
                var time = TimeOnly.FromDateTime(local);
                return DateOnly.FromDateTime(local) == date && time >= workStart && time <= latestStart;
            }))];
    }
}

/// <summary>
/// A repeating local-time schedule, e.g. "Mondays and Wednesdays at 09:30, Berlin time". The time is a wall-clock time,
/// so the meeting stays at 09:30 local all year while its UTC time moves when the clocks change.
/// </summary>
public sealed class WeeklySchedule(IEnumerable<DayOfWeek> days, TimeOnly time, TimeZoneInfo zone)
{
    private readonly HashSet<DayOfWeek> _days = [.. days];

    /// <summary>The next <paramref name="count"/> occurrences strictly after <paramref name="after"/>.</summary>
    public IEnumerable<DateTimeOffset> Occurrences(DateTimeOffset after, int count)
    {
        if (_days.Count == 0) yield break;

        var date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(after, zone).DateTime);
        for (var found = 0; found < count; date = date.AddDays(1))
        {
            if (!_days.Contains(date.DayOfWeek)) continue;

            var occurrence = TimeZones.ToInstant(date.ToDateTime(time), zone);
            if (occurrence <= after) continue;

            found++;
            yield return occurrence;
        }
    }
}
