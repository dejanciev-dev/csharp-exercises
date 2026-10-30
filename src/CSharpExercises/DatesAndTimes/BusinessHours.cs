namespace CSharpExercises.DatesAndTimes;

/// <summary>
/// Support SLAs count working time, not wall-clock time: "respond within 8 business hours" for a ticket opened Friday
/// at 16:00 means Monday afternoon. Adding working time walks day by day, consuming only the open hours and skipping
/// weekends and holidays; the reverse measures how much working time passed between two moments.
/// </summary>
public sealed class BusinessHours(TimeOnly open, TimeOnly close, IEnumerable<DateOnly>? holidays = null)
{
    private readonly HashSet<DateOnly> _holidays = [.. holidays ?? []];

    public bool IsWorkingDay(DateOnly day) => day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !_holidays.Contains(day);

    public DateTime Add(DateTime start, TimeSpan workingTime)
    {
        if (workingTime < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(workingTime));
        var day = DateOnly.FromDateTime(start);
        var time = TimeOnly.FromDateTime(start);

        while (true)
        {
            if (IsWorkingDay(day))
            {
                var from = time < open ? open : time;
                if (from < close)
                {
                    var available = close - from;
                    if (workingTime <= available) return day.ToDateTime(from.Add(workingTime));
                    workingTime -= available;
                }
            }
            day = day.AddDays(1);
            time = open;
        }
    }

    public TimeSpan Between(DateTime from, DateTime to)
    {
        if (to < from) return -Between(to, from);
        var total = TimeSpan.Zero;
        for (var day = DateOnly.FromDateTime(from); day <= DateOnly.FromDateTime(to); day = day.AddDays(1))
        {
            if (!IsWorkingDay(day)) continue;
            var start = day.ToDateTime(open) > from ? day.ToDateTime(open) : from;
            var end = day.ToDateTime(close) < to ? day.ToDateTime(close) : to;
            if (end > start) total += end - start;
        }
        return total;
    }

    /// <summary>Whether a ticket has breached its SLA by <paramref name="now"/>.</summary>
    public bool IsBreached(DateTime opened, TimeSpan sla, DateTime now) => now > Add(opened, sla);
}
