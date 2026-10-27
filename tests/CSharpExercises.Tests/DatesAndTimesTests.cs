using CSharpExercises.DatesAndTimes;

namespace CSharpExercises.Tests;

public class DatesAndTimesTests
{
    private static readonly TimeZoneInfo Berlin = TimeZones.Find("Europe/Berlin");
    private static readonly TimeZoneInfo NewYork = TimeZones.Find("America/New_York");
    private static readonly TimeZoneInfo Tokyo = TimeZones.Find("Asia/Tokyo");

    private static DateOnly D(int year, int month, int day) => new(year, month, day);

    [Theory]
    [InlineData("1990-10-28", "2026-10-27", 35)] // the day before the birthday
    [InlineData("1990-10-28", "2026-10-28", 36)]
    [InlineData("2000-02-29", "2025-02-28", 24)] // leap-day birthday in a common year...
    [InlineData("2000-02-29", "2025-03-01", 25)] // ...counts from 1 March
    [InlineData("2000-02-29", "2028-02-29", 28)]
    [InlineData("2026-10-27", "2026-10-27", 0)]
    public void Age_CountsFullYears(string born, string on, int expected)
    {
        Assert.Equal(expected, CalendarMath.Age(DateOnly.Parse(born), DateOnly.Parse(on)));
    }

    [Fact]
    public void Age_BeforeBirthThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CalendarMath.Age(D(2026, 1, 2), D(2026, 1, 1)));
    }

    [Theory]
    [InlineData(2026, 11, DayOfWeek.Thursday, 4, 26)]  // US Thanksgiving
    [InlineData(2026, 3, DayOfWeek.Sunday, -1, 29)]    // EU summer time starts
    [InlineData(2026, 10, DayOfWeek.Sunday, -1, 25)]   // EU summer time ends
    [InlineData(2026, 3, DayOfWeek.Sunday, 2, 8)]      // US summer time starts
    [InlineData(2026, 10, DayOfWeek.Tuesday, 1, 6)]
    [InlineData(2026, 10, DayOfWeek.Saturday, 5, 31)]
    [InlineData(2026, 10, DayOfWeek.Saturday, -5, 3)]
    public void NthWeekdayOfMonth(int year, int month, DayOfWeek day, int n, int expectedDay)
    {
        Assert.Equal(D(year, month, expectedDay), CalendarMath.NthWeekdayOfMonth(year, month, day, n));
    }

    [Fact]
    public void NthWeekdayOfMonth_ThatDoesNotExistThrows()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CalendarMath.NthWeekdayOfMonth(2026, 2, DayOfWeek.Monday, 5));
        Assert.Contains("no 5th Monday", ex.Message);
    }

    [Theory]
    [InlineData(2024, 3, 31)]
    [InlineData(2025, 4, 20)]
    [InlineData(2026, 4, 5)]
    [InlineData(2000, 4, 23)]
    [InlineData(1818, 3, 22)] // earliest possible date
    [InlineData(2038, 4, 25)] // latest possible date
    public void Easter(int year, int month, int day)
    {
        Assert.Equal(D(year, month, day), CalendarMath.Easter(year));
    }

    [Fact]
    public void Easter_IsAlwaysASundayBetween22MarchAnd25April()
    {
        foreach (var year in Enumerable.Range(1600, 1000))
        {
            var easter = CalendarMath.Easter(year);
            Assert.Equal(DayOfWeek.Sunday, easter.DayOfWeek);
            Assert.InRange(easter, D(year, 3, 22), D(year, 4, 25));
        }
    }

    [Fact]
    public void AddBusinessDays_SkipsWeekends()
    {
        var friday = D(2026, 10, 30);

        Assert.Equal(D(2026, 11, 4), CalendarMath.AddBusinessDays(friday, 3));
        Assert.Equal(D(2026, 10, 26), CalendarMath.AddBusinessDays(friday, -4));
        Assert.Equal(friday, CalendarMath.AddBusinessDays(friday, 0));
    }

    [Fact]
    public void AddBusinessDays_SkipsHolidays()
    {
        HashSet<DateOnly> holidays = [D(2026, 12, 25), D(2026, 12, 28)];

        Assert.Equal(D(2026, 12, 29), CalendarMath.AddBusinessDays(D(2026, 12, 24), 1, holidays));
    }

    [Fact]
    public void BusinessDaysBetween_MatchesCountingDayByDay()
    {
        HashSet<DateOnly> holidays = [D(2026, 1, 1), D(2026, 4, 6), D(2026, 5, 2), D(2026, 12, 25)]; // 2 May is a Saturday
        var from = D(2026, 1, 1);

        foreach (var length in new[] { 0, 1, 6, 7, 8, 100, 365 })
        {
            var to = from.AddDays(length);
            var expected = Enumerable.Range(0, length).Select(from.AddDays).Count(d => CalendarMath.IsBusinessDay(d, holidays));

            Assert.Equal(expected, CalendarMath.BusinessDaysBetween(from, to, holidays));
            Assert.Equal(-expected, CalendarMath.BusinessDaysBetween(to, from, holidays));
        }
    }

    [Fact]
    public void Classify_FindsSkippedAndRepeatedLocalTimes()
    {
        Assert.Equal(LocalTimeKind.Skipped, TimeZones.Classify(new DateTime(2026, 3, 29, 2, 30, 0), Berlin));
        Assert.Equal(LocalTimeKind.Ambiguous, TimeZones.Classify(new DateTime(2026, 10, 25, 2, 30, 0), Berlin));
        Assert.Equal(LocalTimeKind.Normal, TimeZones.Classify(new DateTime(2026, 6, 1, 2, 30, 0), Berlin));
    }

    [Fact]
    public void ConvertTimeToUtc_ThrowsForASkippedTime()
    {
        Assert.Throws<ArgumentException>(() => TimeZoneInfo.ConvertTimeToUtc(new DateTime(2026, 3, 29, 2, 30, 0), Berlin));
    }

    [Fact]
    public void ToInstant_MovesASkippedTimeForwardByTheGap()
    {
        var instant = TimeZones.ToInstant(new DateTime(2026, 3, 29, 2, 30, 0), Berlin);

        Assert.Equal(new DateTimeOffset(2026, 3, 29, 3, 30, 0, TimeSpan.FromHours(2)), instant);
        Assert.Equal(TimeSpan.FromHours(2), instant.Offset);
    }

    [Fact]
    public void ToInstant_PicksTheEarlierOfTwoRepeatedTimes()
    {
        var instant = TimeZones.ToInstant(new DateTime(2026, 10, 25, 2, 30, 0), Berlin);

        Assert.Equal(new DateTime(2026, 10, 25, 0, 30, 0), instant.UtcDateTime);
    }

    [Fact]
    public void ToInstant_NormalTime()
    {
        Assert.Equal(new DateTime(2026, 6, 1, 10, 0, 0), TimeZones.ToInstant(new DateTime(2026, 6, 1, 12, 0, 0), Berlin).UtcDateTime);
    }

    [Fact]
    public void AddLocalDays_KeepsTheWallClockTimeAcrossDst()
    {
        var saturdayBeforeSpring = TimeZones.ToInstant(new DateTime(2026, 3, 28, 9, 0, 0), Berlin);
        var saturdayBeforeAutumn = TimeZones.ToInstant(new DateTime(2026, 10, 24, 9, 0, 0), Berlin);

        var springNext = TimeZones.AddLocalDays(saturdayBeforeSpring, 1, Berlin);
        var autumnNext = TimeZones.AddLocalDays(saturdayBeforeAutumn, 1, Berlin);

        Assert.Equal(TimeSpan.FromHours(23), springNext - saturdayBeforeSpring);
        Assert.Equal(TimeSpan.FromHours(25), autumnNext - saturdayBeforeAutumn);
        Assert.Equal(9, TimeZoneInfo.ConvertTime(springNext, Berlin).Hour);
        Assert.Equal(9, TimeZoneInfo.ConvertTime(autumnNext, Berlin).Hour);
    }

    [Fact]
    public void SharedWorkingHours_BerlinAndNewYork()
    {
        var slots = TimeZones.SharedWorkingHours(D(2026, 6, 15), [Berlin, NewYork], new TimeOnly(9, 0), new TimeOnly(17, 0));

        // 09:00 and 10:00 in New York are 15:00 and 16:00 in Berlin.
        Assert.Equal([new DateTime(2026, 6, 15, 13, 0, 0), new DateTime(2026, 6, 15, 14, 0, 0)], slots.Select(s => s.UtcDateTime));
    }

    [Fact]
    public void SharedWorkingHours_CanBeEmpty()
    {
        Assert.Empty(TimeZones.SharedWorkingHours(D(2026, 6, 15), [Berlin, NewYork, Tokyo], new TimeOnly(9, 0), new TimeOnly(17, 0)));
    }

    [Fact]
    public void WeeklySchedule_StaysAtTheSameLocalTimeWhenClocksChange()
    {
        var standup = new WeeklySchedule([DayOfWeek.Monday, DayOfWeek.Wednesday], new TimeOnly(9, 30), Berlin);

        var next = standup.Occurrences(new DateTimeOffset(2026, 3, 20, 0, 0, 0, TimeSpan.Zero), 4).ToList();

        Assert.Equal(
            [
                new DateTime(2026, 3, 23, 8, 30, 0), // winter time, UTC+1
                new DateTime(2026, 3, 25, 8, 30, 0),
                new DateTime(2026, 3, 30, 7, 30, 0), // summer time, UTC+2
                new DateTime(2026, 4, 1, 7, 30, 0),
            ],
            next.Select(o => o.UtcDateTime));
        Assert.All(next, o => Assert.Equal(new TimeOnly(9, 30), TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(o, Berlin).DateTime)));
    }

    [Fact]
    public void WeeklySchedule_StartsStrictlyAfterTheGivenInstant()
    {
        var schedule = new WeeklySchedule([DayOfWeek.Tuesday], new TimeOnly(10, 0), Berlin);
        var exactlyAnOccurrence = TimeZones.ToInstant(new DateTime(2026, 10, 27, 10, 0, 0), Berlin);

        var next = Assert.Single(schedule.Occurrences(exactlyAnOccurrence, 1));

        Assert.Equal(exactlyAnOccurrence.AddDays(7), next);
    }

    [Fact]
    public void WeeklySchedule_WithNoDaysIsEmpty()
    {
        Assert.Empty(new WeeklySchedule([], new TimeOnly(9, 0), Berlin).Occurrences(DateTimeOffset.UnixEpoch, 3));
    }
}
