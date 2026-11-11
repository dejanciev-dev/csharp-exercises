namespace CSharpExercises.Astronomy;

/// <summary>
/// Astronomers count time as a single running number of days, the Julian Day, which starts at noon (so a night of
/// observing doesn't change date) on 1 January 4713 BC. Differences between dates become plain subtraction.
/// These are Meeus's formulas for the Gregorian calendar; the month shift treats January and February as months
/// 13 and 14 of the previous year so the leap day falls at the end, and 30.6001 squeezes the month lengths into one
/// expression (the .0001 guards against floating-point floor errors).
/// </summary>
public static class JulianDay
{
    public const double J2000 = 2451545.0; // 2000-01-01 12:00

    public static double FromDate(int year, int month, double day)
    {
        if (month <= 2)
        {
            year--;
            month += 12;
        }
        var a = Math.Floor(year / 100.0);
        var b = 2 - a + Math.Floor(a / 4); // Gregorian correction: skip century leap days except every 400 years
        return Math.Floor(365.25 * (year + 4716)) + Math.Floor(30.6001 * (month + 1)) + day + b - 1524.5;
    }

    public static double FromDateTime(DateTime utc) =>
        FromDate(utc.Year, utc.Month, utc.Day + utc.TimeOfDay.TotalDays);

    public static (int Year, int Month, double Day) ToDate(double jd)
    {
        var shifted = jd + 0.5;
        var z = Math.Floor(shifted);
        var f = shifted - z;
        var alpha = Math.Floor((z - 1867216.25) / 36524.25);
        var a = z + 1 + alpha - Math.Floor(alpha / 4);
        var b = a + 1524;
        var c = Math.Floor((b - 122.1) / 365.25);
        var d = Math.Floor(365.25 * c);
        var e = Math.Floor((b - d) / 30.6001);

        var day = b - d - Math.Floor(30.6001 * e) + f;
        var month = (int)(e < 14 ? e - 1 : e - 13);
        var year = (int)(month > 2 ? c - 4716 : c - 4715);
        return (year, month, day);
    }

    /// <summary>Day of the week for free: whole Julian days cycle through the week.</summary>
    public static DayOfWeek DayOfWeek(double jd) => (DayOfWeek)(int)(Math.Floor(jd + 1.5) % 7);
}
