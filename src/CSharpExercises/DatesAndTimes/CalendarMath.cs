namespace CSharpExercises.DatesAndTimes;

/// <summary>
/// Calendar calculations on <see cref="DateOnly"/>: a date with no time and no time zone, so none of the
/// "midnight in which zone?" bugs that come with <see cref="DateTime"/>.
/// </summary>
public static class CalendarMath
{
    /// <summary>
    /// Full years between <paramref name="birthDate"/> and <paramref name="on"/>. Subtracting years alone is wrong
    /// before the birthday; and someone born on 29 February has their birthday on 1 March in common years
    /// (the convention in many countries' law; others use 28 February).
    /// </summary>
    public static int Age(DateOnly birthDate, DateOnly on)
    {
        if (on < birthDate) throw new ArgumentOutOfRangeException(nameof(on), "The date is before the birth date.");

        var age = on.Year - birthDate.Year;
        var birthdayThisYear = birthDate is { Month: 2, Day: 29 } && !DateTime.IsLeapYear(on.Year)
            ? new DateOnly(on.Year, 3, 1)
            : new DateOnly(on.Year, birthDate.Month, birthDate.Day);

        return on < birthdayThisYear ? age - 1 : age;
    }

    /// <summary>
    /// The <paramref name="n"/>th <paramref name="day"/> of a month; negative counts from the end, so -1 is the last.
    /// E.g. US Thanksgiving is the 4th Thursday of November, and the EU switches to summer time on the last Sunday of March.
    /// </summary>
    public static DateOnly NthWeekdayOfMonth(int year, int month, DayOfWeek day, int n)
    {
        if (n is 0 or < -5 or > 5) throw new ArgumentOutOfRangeException(nameof(n), "Use 1 to 5, or -1 to -5 to count from the end.");

        DateOnly result;
        if (n > 0)
        {
            var first = new DateOnly(year, month, 1);
            var offset = ((int)day - (int)first.DayOfWeek + 7) % 7;
            result = first.AddDays(offset + (n - 1) * 7);
        }
        else
        {
            var last = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
            var offset = ((int)last.DayOfWeek - (int)day + 7) % 7;
            result = last.AddDays(-offset + (n + 1) * 7);
        }

        return result.Month == month
            ? result
            : throw new ArgumentOutOfRangeException(nameof(n), $"{new DateOnly(year, month, 1):MMMM yyyy} has no {Ordinal(n)} {day}.");
    }

    /// <summary>
    /// Western (Gregorian) Easter Sunday, with the "anonymous Gregorian algorithm" (Meeus/Jones/Butcher):
    /// the first Sunday after the first ecclesiastical full moon on or after 21 March.
    /// </summary>
    public static DateOnly Easter(int year)
    {
        var a = year % 19;              // position in the 19-year lunar cycle
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30; // days from 21 March to the full moon
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7; // days from the full moon to the next Sunday
        var m = (a + 11 * h + 22 * l) / 451;
        var month = (h + l - 7 * m + 114) / 31;
        var day = (h + l - 7 * m + 114) % 31 + 1;
        return new DateOnly(year, month, day);
    }

    public static bool IsBusinessDay(DateOnly date, IReadOnlySet<DateOnly>? holidays = null) =>
        date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && holidays?.Contains(date) != true;

    /// <summary>
    /// Moves <paramref name="days"/> business days forward (or backward if negative), skipping weekends and holidays.
    /// "Delivery in 3 business days" from a Friday is the next Wednesday.
    /// </summary>
    public static DateOnly AddBusinessDays(DateOnly start, int days, IReadOnlySet<DateOnly>? holidays = null)
    {
        var step = Math.Sign(days);
        var date = start;
        for (var remaining = Math.Abs(days); remaining > 0;)
        {
            date = date.AddDays(step);
            if (IsBusinessDay(date, holidays)) remaining--;
        }
        return date;
    }

    /// <summary>Business days in the half-open range [<paramref name="from"/>, <paramref name="to"/>).</summary>
    public static int BusinessDaysBetween(DateOnly from, DateOnly to, IReadOnlySet<DateOnly>? holidays = null)
    {
        if (to < from) return -BusinessDaysBetween(to, from, holidays);

        // Whole weeks contribute 5 days each without a loop; only the leftover days (fewer than 7) are counted one by one.
        var totalDays = to.DayNumber - from.DayNumber;
        var count = totalDays / 7 * 5;
        for (var date = from.AddDays(totalDays / 7 * 7); date < to; date = date.AddDays(1))
        {
            if (IsBusinessDay(date)) count++;
        }

        return count - (holidays?.Count(h => h >= from && h < to && IsBusinessDay(h)) ?? 0);
    }

    private static string Ordinal(int n) => n switch
    {
        -1 => "last",
        < 0 => $"{Ordinal(-n)}-from-last",
        1 => "1st",
        2 => "2nd",
        3 => "3rd",
        _ => $"{n}th",
    };
}
