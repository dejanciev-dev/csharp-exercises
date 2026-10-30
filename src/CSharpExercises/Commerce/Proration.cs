namespace CSharpExercises.Commerce;

public sealed record ProrationLine(string Description, decimal Amount);

/// <summary>
/// Changing a subscription mid-cycle: credit the unused part of the old plan and charge for the rest of the period on
/// the new one, both measured in days of the actual billing period (months differ in length, so "a month" isn't 30
/// days). Rounding each line to cents and listing both lines makes the invoice explainable to the customer.
/// </summary>
public static class Proration
{
    public static IReadOnlyList<ProrationLine> ChangePlan(
        decimal oldMonthlyPrice, decimal newMonthlyPrice, DateOnly periodStart, DateOnly changeDate, string oldPlan, string newPlan)
    {
        var periodEnd = periodStart.AddMonths(1);
        if (changeDate < periodStart || changeDate >= periodEnd) throw new ArgumentOutOfRangeException(nameof(changeDate));

        var totalDays = periodEnd.DayNumber - periodStart.DayNumber;
        var remainingDays = periodEnd.DayNumber - changeDate.DayNumber;
        var share = (decimal)remainingDays / totalDays;

        return
        [
            new($"Unused time on {oldPlan} ({remainingDays} of {totalDays} days)", -VatCalculator.Round(oldMonthlyPrice * share)),
            new($"Remaining time on {newPlan} ({remainingDays} of {totalDays} days)", VatCalculator.Round(newMonthlyPrice * share)),
        ];
    }

    public static decimal NetCharge(IEnumerable<ProrationLine> lines) => lines.Sum(l => l.Amount);

    /// <summary>The first invoice of a subscription that should renew on a fixed day of the month (e.g. the 1st).</summary>
    public static decimal FirstPartialPeriod(decimal monthlyPrice, DateOnly signUp, int billingDay)
    {
        var anchor = new DateOnly(signUp.Year, signUp.Month, Math.Min(billingDay, DateTime.DaysInMonth(signUp.Year, signUp.Month)));
        if (anchor <= signUp) anchor = anchor.AddMonths(1);
        var periodStart = anchor.AddMonths(-1);
        var share = (decimal)(anchor.DayNumber - signUp.DayNumber) / (anchor.DayNumber - periodStart.DayNumber);
        return VatCalculator.Round(monthlyPrice * share);
    }
}
