namespace CSharpExercises.Finance;

public sealed record Installment(int Number, decimal Payment, decimal Interest, decimal Principal, decimal Balance);

/// <summary>
/// A loan repayment schedule. The fixed monthly payment comes from the annuity formula P·r / (1 - (1 + r)^-n). Each
/// month the interest is charged on what's left, and the rest of the payment reduces the balance, so early payments
/// are mostly interest. Amounts are rounded to cents every month, and the last payment absorbs the rounding so the
/// balance ends at exactly zero.
/// </summary>
public static class Amortization
{
    public static decimal MonthlyPayment(decimal principal, decimal annualRatePercent, int months)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(months);
        var r = (double)annualRatePercent / 100 / 12;
        if (r == 0) return Math.Round(principal / months, 2, MidpointRounding.AwayFromZero);
        var payment = (double)principal * r / (1 - Math.Pow(1 + r, -months));
        return Math.Round((decimal)payment, 2, MidpointRounding.AwayFromZero);
    }

    public static IReadOnlyList<Installment> Schedule(decimal principal, decimal annualRatePercent, int months, decimal extraPerMonth = 0)
    {
        var payment = MonthlyPayment(principal, annualRatePercent, months);
        var monthlyRate = annualRatePercent / 100 / 12;
        var balance = principal;
        var schedule = new List<Installment>();

        for (var n = 1; balance > 0; n++)
        {
            var interest = Math.Round(balance * monthlyRate, 2, MidpointRounding.AwayFromZero);
            var due = payment + extraPerMonth;
            var principalPart = Math.Min(due - interest, balance);
            if (n == months && extraPerMonth == 0) principalPart = balance; // final payment settles any rounding drift
            balance -= principalPart;
            schedule.Add(new Installment(n, interest + principalPart, interest, principalPart, balance));
        }
        return schedule;
    }

    /// <summary>How much paying extra each month saves, in interest and in months.</summary>
    public static (decimal InterestSaved, int MonthsSaved) ExtraPaymentEffect(decimal principal, decimal annualRatePercent, int months, decimal extra)
    {
        var normal = Schedule(principal, annualRatePercent, months);
        var faster = Schedule(principal, annualRatePercent, months, extra);
        return (normal.Sum(i => i.Interest) - faster.Sum(i => i.Interest), normal.Count - faster.Count);
    }
}
