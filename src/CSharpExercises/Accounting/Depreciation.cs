namespace CSharpExercises.Accounting;

public sealed record DepreciationYear(int Year, decimal Expense, decimal Accumulated, decimal BookValue);

/// <summary>
/// Spreading an asset's cost over its useful life. Straight-line charges the same each year. Declining balance charges a
/// fixed rate of the remaining book value, front-loading the expense, and switches to straight-line when that becomes
/// larger so the asset still reaches its salvage value on time. Sum-of-the-years'-digits is in between. Every schedule
/// ends exactly at the salvage value, with rounding absorbed in the final year.
/// </summary>
public static class Depreciation
{
    public static IReadOnlyList<DepreciationYear> StraightLine(decimal cost, decimal salvage, int years) =>
        Build(cost, salvage, years, (_, _, _) => (cost - salvage) / years);

    public static IReadOnlyList<DepreciationYear> DecliningBalance(decimal cost, decimal salvage, int years, decimal factor = 2)
    {
        var rate = factor / years;
        return Build(cost, salvage, years, (year, bookValue, remainingYears) =>
        {
            var declining = bookValue * rate;
            var straight = (bookValue - salvage) / remainingYears; // switch when straight-line gives more
            return Math.Max(declining, straight);
        });
    }

    public static IReadOnlyList<DepreciationYear> SumOfYearsDigits(decimal cost, decimal salvage, int years)
    {
        var sum = years * (years + 1) / 2m;
        return Build(cost, salvage, years, (year, _, _) => (cost - salvage) * (years - year + 1) / sum);
    }

    private static List<DepreciationYear> Build(decimal cost, decimal salvage, int years, Func<int, decimal, int, decimal> expenseFor)
    {
        if (salvage > cost || years < 1) throw new ArgumentException("Need salvage ≤ cost and at least one year.");
        var schedule = new List<DepreciationYear>();
        var accumulated = 0m;
        for (var year = 1; year <= years; year++)
        {
            var bookValue = cost - accumulated;
            var expense = year == years
                ? bookValue - salvage                                          // the last year lands exactly on salvage
                : Math.Min(Math.Round(expenseFor(year, bookValue, years - year + 1), 2, MidpointRounding.AwayFromZero), bookValue - salvage);
            accumulated += expense;
            schedule.Add(new DepreciationYear(year, expense, accumulated, cost - accumulated));
        }
        return schedule;
    }
}
