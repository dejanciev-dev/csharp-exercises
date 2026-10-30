namespace CSharpExercises.Accounting;

public sealed record Invoice(string Customer, string Number, DateOnly DueDate, decimal Amount, decimal Paid = 0)
{
    public decimal Outstanding => Amount - Paid;
}

/// <summary>
/// An accounts-receivable aging report: how much each customer owes, bucketed by how long it's overdue (current, 1–30,
/// 31–60, 61–90, 90+ days). Older debts are less likely to be paid, so the report drives collection calls and the bad
/// debt allowance, which applies a higher loss percentage to older buckets.
/// </summary>
public static class AgingReport
{
    public static readonly string[] Buckets = ["Current", "1-30", "31-60", "61-90", "90+"];

    public static string BucketOf(DateOnly dueDate, DateOnly asOf) => (asOf.DayNumber - dueDate.DayNumber) switch
    {
        <= 0 => "Current",
        <= 30 => "1-30",
        <= 60 => "31-60",
        <= 90 => "61-90",
        _ => "90+",
    };

    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, decimal>> Build(IEnumerable<Invoice> invoices, DateOnly asOf) =>
        invoices
            .Where(i => i.Outstanding > 0)
            .GroupBy(i => i.Customer)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyDictionary<string, decimal>)Buckets.ToDictionary(b => b, b => g.Where(i => BucketOf(i.DueDate, asOf) == b).Sum(i => i.Outstanding)));

    /// <summary>Expected losses: each bucket's total times its historical loss rate.</summary>
    public static decimal BadDebtAllowance(IEnumerable<Invoice> invoices, DateOnly asOf, IReadOnlyDictionary<string, decimal> lossRates) =>
        Math.Round(invoices.Where(i => i.Outstanding > 0).Sum(i => i.Outstanding * lossRates[BucketOf(i.DueDate, asOf)]), 2);

    /// <summary>Days sales outstanding: how many days of sales are tied up in receivables.</summary>
    public static decimal DaysSalesOutstanding(decimal receivables, decimal creditSales, int days) =>
        creditSales == 0 ? 0 : Math.Round(receivables / creditSales * days, 1);
}
