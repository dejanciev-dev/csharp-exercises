namespace CSharpExercises.Astronomy;

/// <summary>
/// A simple moon phase model: count days since a known new moon and wrap by the mean synodic month (new moon to new
/// moon, 29.53 days). The real moon wanders up to about half a day from this mean, which is plenty for a calendar
/// icon. The lit fraction follows the angle between sun and moon: (1 - cos θ) / 2.
/// </summary>
public static class MoonPhase
{
    public const double SynodicMonth = 29.530588853;

    private const double ReferenceNewMoon = 2451550.1; // 2000-01-06 ~14:24 UTC

    private static readonly string[] Names =
    [
        "New moon", "Waxing crescent", "First quarter", "Waxing gibbous",
        "Full moon", "Waning gibbous", "Last quarter", "Waning crescent",
    ];

    /// <summary>Days since the last new moon, 0 to 29.53.</summary>
    public static double Age(DateTime utc)
    {
        var age = (JulianDay.FromDateTime(utc) - ReferenceNewMoon) % SynodicMonth;
        return age < 0 ? age + SynodicMonth : age;
    }

    /// <summary>Fraction of the disc that is lit, 0 at new moon and 1 at full moon.</summary>
    public static double Illumination(DateTime utc) =>
        (1 - Math.Cos(2 * Math.PI * Age(utc) / SynodicMonth)) / 2;

    /// <summary>One of eight named phases, each centred on its moment (full moon is ±1.85 days around day 14.77).</summary>
    public static string Name(DateTime utc)
    {
        var eighth = (int)Math.Floor(Age(utc) / SynodicMonth * 8 + 0.5) % 8;
        return Names[eighth];
    }

    /// <summary>The next moment the mean model puts at the given age, e.g. 0 for new moon or half a month for full.</summary>
    public static DateTime Next(DateTime fromUtc, double targetAge)
    {
        var wait = (targetAge - Age(fromUtc) + SynodicMonth) % SynodicMonth;
        return fromUtc.AddDays(wait);
    }
}
