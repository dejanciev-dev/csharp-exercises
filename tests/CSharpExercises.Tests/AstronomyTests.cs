using CSharpExercises.Astronomy;

namespace CSharpExercises.Tests;

public class AstronomyTests
{
    [Fact]
    public void JulianDay_MatchesKnownValues()
    {
        Assert.Equal(JulianDay.J2000, JulianDay.FromDate(2000, 1, 1.5), 9);
        Assert.Equal(2436116.31, JulianDay.FromDate(1957, 10, 4.81), 9); // Sputnik 1
        Assert.Equal(2446822.5, JulianDay.FromDate(1987, 1, 27), 9);
        Assert.Equal(2299160.5, JulianDay.FromDate(1582, 10, 15), 9); // first day of the Gregorian calendar
    }

    [Fact]
    public void JulianDay_AgreesWithDateTimeArithmetic()
    {
        var epoch = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var random = new Random(7);
        for (var i = 0; i < 1000; i++)
        {
            var date = new DateTime(1600, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(random.Next(0, 300_000_000));
            var jd = JulianDay.FromDateTime(date);

            Assert.Equal(JulianDay.J2000 + (date - epoch).TotalDays, jd, 6);
            var (year, month, day) = JulianDay.ToDate(jd);
            Assert.Equal((date.Year, date.Month), (year, month));
            Assert.Equal(date.Day + date.TimeOfDay.TotalDays, day, 6);
            Assert.Equal(date.DayOfWeek, JulianDay.DayOfWeek(jd));
        }
    }

    [Fact]
    public void MoonPhase_RecognisesKnownMoons()
    {
        var eclipse = new DateTime(2024, 4, 8, 18, 21, 0, DateTimeKind.Utc); // total solar eclipse: a new moon
        var fullMoon = new DateTime(2024, 4, 23, 23, 49, 0, DateTimeKind.Utc);

        Assert.Equal("New moon", MoonPhase.Name(eclipse));
        Assert.True(MoonPhase.Illumination(eclipse) < 0.01);
        Assert.Equal("Full moon", MoonPhase.Name(fullMoon));
        Assert.True(MoonPhase.Illumination(fullMoon) > 0.99);
        Assert.Equal("First quarter", MoonPhase.Name(eclipse.AddDays(7.4)));
    }

    [Fact]
    public void MoonPhase_PredictsTheNextFullMoonWithinHalfADay()
    {
        var next = MoonPhase.Next(new DateTime(2024, 4, 10, 0, 0, 0, DateTimeKind.Utc), MoonPhase.SynodicMonth / 2);

        var actual = new DateTime(2024, 4, 23, 23, 49, 0, DateTimeKind.Utc);
        Assert.True(Math.Abs((next - actual).TotalHours) < 12, $"Predicted {next:u}");
    }

    [Fact]
    public void Kepler_SolvesTheEquationEvenForEccentricOrbits()
    {
        foreach (var eccentricity in new[] { 0, 0.0167, 0.5, 0.967, 0.999 })
            for (var m = 0.0; m < 2 * Math.PI; m += 0.1)
            {
                var (e, iterations) = Kepler.EccentricAnomaly(m, eccentricity);
                Assert.Equal(m, e - eccentricity * Math.Sin(e), 9);
                Assert.True(iterations < 50);
            }

        Assert.Equal(1.2, Kepler.EccentricAnomaly(1.2, 0).E, 12); // a circle needs no correction
    }

    [Fact]
    public void Kepler_FollowsTheThirdLaw()
    {
        Assert.Equal(1, Kepler.PeriodYears(1), 12);
        Assert.Equal(1.881, Kepler.PeriodYears(1.524), 3); // Mars
    }

    [Fact]
    public void Kepler_HalleysCometSwingsFarOut()
    {
        const double a = 17.8, e = 0.967;
        var period = Kepler.PeriodYears(a);

        var perihelion = Kepler.Position(a, e, 0);
        var aphelion = Kepler.Position(a, e, period / 2);
        var quarter = Kepler.Position(a, e, period / 4);

        Assert.Equal(a * (1 - e), perihelion.Distance, 9);
        Assert.Equal(a * (1 + e), aphelion.Distance, 9);
        Assert.Equal(-a * (1 + e), aphelion.X, 9);
        Assert.True(quarter.Distance > a, "A quarter of the period in, the comet is already well past the end of the minor axis.");
        Assert.Equal(quarter.Distance, Math.Sqrt(quarter.X * quarter.X + quarter.Y * quarter.Y), 9);
    }
}
