using System.Globalization;
using CSharpExercises.DatesAndTimes;

namespace CSharpExercises.Tests;

public class MoreDatesAndTimesTests
{
    [Fact]
    public void Rrule_WeeklyOnSeveralDays()
    {
        var rule = RecurrenceRule.Parse("FREQ=WEEKLY;INTERVAL=2;BYDAY=MO,WE;COUNT=5");

        var dates = rule.Occurrences(new DateOnly(2026, 11, 2)).ToList(); // a Monday

        Assert.Equal(["2026-11-02", "2026-11-04", "2026-11-16", "2026-11-18", "2026-11-30"], dates.Select(d => d.ToString("yyyy-MM-dd")));
    }

    [Fact]
    public void Rrule_MonthlySkipsShortMonths()
    {
        var dates = RecurrenceRule.Parse("FREQ=MONTHLY;COUNT=4").Occurrences(new DateOnly(2026, 1, 31)).ToList();

        Assert.Equal(["2026-01-31", "2026-03-31", "2026-05-31", "2026-07-31"], dates.Select(d => d.ToString("yyyy-MM-dd")));
    }

    [Fact]
    public void Rrule_DailyWeekdaysUntil()
    {
        var dates = RecurrenceRule.Parse("FREQ=DAILY;BYDAY=MO,TU,WE,TH,FR;UNTIL=20261110").Occurrences(new DateOnly(2026, 11, 5)).ToList();

        Assert.Equal([5, 6, 9, 10], dates.Select(d => d.Day));
        Assert.Throws<FormatException>(() => RecurrenceRule.Parse("FREQ=YEARLY"));
        Assert.Throws<FormatException>(() => RecurrenceRule.Parse("FREQ=DAILY;COUNT=2;UNTIL=20260101"));
        Assert.Equal(1000, RecurrenceRule.Parse("FREQ=DAILY").Occurrences(new DateOnly(2026, 1, 1)).Take(1000).Count()); // open-ended
    }

    private static readonly BusinessHours Office = new(new TimeOnly(9, 0), new TimeOnly(17, 0), [new DateOnly(2026, 11, 2)]);

    [Fact]
    public void BusinessHours_AddSkipsNightsWeekendsAndHolidays()
    {
        var friday4pm = new DateTime(2026, 10, 30, 16, 0, 0);

        // 1 hour on Friday, Monday 2 Nov is a holiday, so the remaining 7 hours land on Tuesday at 16:00.
        Assert.Equal(new DateTime(2026, 11, 3, 16, 0, 0), Office.Add(friday4pm, TimeSpan.FromHours(8)));
        Assert.Equal(new DateTime(2026, 10, 30, 10, 30, 0), Office.Add(new DateTime(2026, 10, 30, 6, 0, 0), TimeSpan.FromMinutes(90))); // starts at opening
        Assert.Equal(new DateTime(2026, 11, 3, 9, 0, 0), Office.Add(new DateTime(2026, 10, 31, 12, 0, 0), TimeSpan.Zero));
    }

    [Fact]
    public void BusinessHours_MeasuresWorkingTimeAndBreaches()
    {
        var opened = new DateTime(2026, 10, 30, 16, 0, 0);
        var now = new DateTime(2026, 11, 3, 12, 0, 0);

        Assert.Equal(TimeSpan.FromHours(4), Office.Between(opened, now)); // 1 h Friday + 3 h Tuesday
        Assert.False(Office.IsBreached(opened, TimeSpan.FromHours(8), now));
        Assert.True(Office.IsBreached(opened, TimeSpan.FromHours(2), now));
    }

    private static readonly DateTimeOffset Now = new(2026, 10, 30, 0, 1, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(10, "just now")]
    [InlineData(5 * 60, "5 minutes ago")]
    [InlineData(60, "1 minute ago")]
    [InlineData(3 * 3600, "3 hours ago")]
    [InlineData(-2 * 3600, "in 2 hours")]
    [InlineData(23 * 3600, "yesterday")]
    [InlineData(3 * 86400, "3 days ago")]
    [InlineData(15 * 86400, "2 weeks ago")]
    [InlineData(-90 * 86400, "in 3 months")]
    [InlineData(800 * 86400, "2 years ago")]
    public void RelativeTime_Describes(int secondsAgo, string expected) =>
        Assert.Equal(expected, RelativeTime.Describe(Now.AddSeconds(-secondsAgo), Now));

    [Fact]
    public void RelativeTime_YesterdayIsACalendarDay()
    {
        Assert.Equal("yesterday", RelativeTime.Describe(new DateTimeOffset(2026, 10, 29, 23, 0, 0, TimeSpan.Zero).AddHours(-22), Now));
        Assert.Equal(["45s", "5m", "3h", "2d", "1y"], new[] { 45, 300, 10_800, 172_800, 40_000_000 }.Select(s => RelativeTime.Short(TimeSpan.FromSeconds(s))));
    }

    [Theory]
    [InlineData("2026-01-01", 2026, 1)]
    [InlineData("2027-01-01", 2026, 53)]   // a Friday: still in the last week of 2026
    [InlineData("2024-12-30", 2025, 1)]    // a Monday: already week 1 of 2025
    [InlineData("2026-10-30", 2026, 44)]
    public void IsoWeek_MatchesTheFramework(string date, int year, int week)
    {
        var d = DateOnly.Parse(date, CultureInfo.InvariantCulture);
        Assert.Equal((year, week), Iso8601.WeekOf(d));
        Assert.Equal((ISOWeek.GetYear(d.ToDateTime(TimeOnly.MinValue)), ISOWeek.GetWeekOfYear(d.ToDateTime(TimeOnly.MinValue))), Iso8601.WeekOf(d));
    }

    [Fact]
    public void IsoWeek_RoundTripsAndCountsWeeks()
    {
        for (var d = new DateOnly(2020, 1, 1); d < new DateOnly(2030, 1, 1); d = d.AddDays(1))
        {
            var (year, week) = Iso8601.WeekOf(d);
            Assert.InRange(d.DayNumber - Iso8601.MondayOfWeek(year, week).DayNumber, 0, 6);
        }
        Assert.Equal(53, Iso8601.WeeksInYear(2026));
        Assert.Equal(52, Iso8601.WeeksInYear(2025));
        Assert.Equal("2026-W44-5", Iso8601.FormatWeek(new DateOnly(2026, 10, 30)));
    }

    [Fact]
    public void IsoDuration_ParsesFormatsAndAdds()
    {
        var d = Iso8601.ParseDuration("P1Y2M3DT4H5M6S");

        Assert.Equal((1, 2, 3, new TimeSpan(4, 5, 6)), (d.Years, d.Months, d.Days, d.Time));
        Assert.Equal("P1Y2M3DT4H5M6S", d.ToString());
        Assert.Equal(TimeSpan.FromMinutes(90), Iso8601.ParseDuration("PT1.5H").Time);
        Assert.Equal(14, Iso8601.ParseDuration("P2W").Days);
        Assert.Equal("PT0S", Iso8601.ParseDuration("PT0S").ToString());

        Assert.Equal(new DateTime(2026, 2, 28), Iso8601.ParseDuration("P1M").AddTo(new DateTime(2026, 1, 31))); // month-end clamps
        Assert.Throws<FormatException>(() => Iso8601.ParseDuration("P"));
        Assert.Throws<FormatException>(() => Iso8601.ParseDuration("1D"));
        Assert.Throws<FormatException>(() => Iso8601.ParseDuration("P1DT"));
    }
}
