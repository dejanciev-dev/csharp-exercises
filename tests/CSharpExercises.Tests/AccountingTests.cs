using CSharpExercises.Accounting;

namespace CSharpExercises.Tests;

public class AccountingTests
{
    private static Ledger NewLedger() => new Ledger()
        .AddAccount("1000", "Cash", AccountType.Asset)
        .AddAccount("1200", "Receivables", AccountType.Asset)
        .AddAccount("2000", "Loan", AccountType.Liability)
        .AddAccount("3000", "Owner's capital", AccountType.Equity)
        .AddAccount("4000", "Sales", AccountType.Income)
        .AddAccount("5000", "Rent", AccountType.Expense);

    private static readonly DateOnly Day = new(2026, 10, 1);

    [Fact]
    public void Ledger_KeepsTheBooksBalanced()
    {
        var ledger = NewLedger();
        ledger.Post(Day, "owner invests", Ledger.Debit("1000", 10_000), Ledger.Credit("3000", 10_000));
        ledger.Post(Day, "bank loan", Ledger.Debit("1000", 5_000), Ledger.Credit("2000", 5_000));
        ledger.Post(Day.AddDays(5), "invoice sent", Ledger.Debit("1200", 3_000), Ledger.Credit("4000", 3_000));
        ledger.Post(Day.AddDays(9), "rent paid", Ledger.Debit("5000", 1_200), Ledger.Credit("1000", 1_200));
        ledger.Post(Day.AddDays(20), "customer pays", Ledger.Debit("1000", 3_000), Ledger.Credit("1200", 3_000));

        Assert.Equal(16_800, ledger.Balance("1000"));
        Assert.Equal(0, ledger.Balance("1200"));
        Assert.Equal(1_800, ledger.NetIncome);
        Assert.True(ledger.Balances);
        Assert.Equal(13_800, ledger.Balance("1000", asOf: Day.AddDays(10)));

        var trial = ledger.TrialBalance();
        Assert.Equal(trial.Sum(t => t.Debit), trial.Sum(t => t.Credit));
    }

    [Fact]
    public void Ledger_RejectsBadEntries()
    {
        var ledger = NewLedger();
        Assert.Throws<InvalidOperationException>(() => ledger.Post(Day, "unbalanced", Ledger.Debit("1000", 100), Ledger.Credit("4000", 90)));
        Assert.Throws<InvalidOperationException>(() => ledger.Post(Day, "one side", Ledger.Debit("1000", 100)));
        Assert.Throws<KeyNotFoundException>(() => ledger.Post(Day, "typo", Ledger.Debit("1001", 100), Ledger.Credit("4000", 100)));
        Assert.Throws<InvalidOperationException>(() => ledger.Post(Day, "both", new Posting("1000", 5, 5), Ledger.Credit("4000", 0)));
        Assert.Empty(ledger.Journal);
    }

    [Fact]
    public void Depreciation_SchedulesEndAtSalvage()
    {
        var straight = Depreciation.StraightLine(10_000, 1_000, 5);
        var declining = Depreciation.DecliningBalance(10_000, 1_000, 5);
        var digits = Depreciation.SumOfYearsDigits(10_000, 1_000, 5);

        Assert.All(straight, y => Assert.Equal(1_800, y.Expense));
        Assert.Equal([4000m, 2400m, 1440m, 864m, 296m], declining.Select(y => y.Expense)); // the last year stops at salvage
        Assert.Equal([4000m, 2400m, 1440m, 1080m, 1080m], Depreciation.DecliningBalance(10_000, 0, 5).Select(y => y.Expense)); // switches to straight-line
        Assert.Equal([3000m, 2400m, 1800m, 1200m, 600m], digits.Select(y => y.Expense));
        Assert.All(new[] { straight, declining, digits }, s => Assert.Equal(1_000, s[^1].BookValue));
    }

    [Fact]
    public void Depreciation_RoundingIsAbsorbedAtTheEnd()
    {
        var schedule = Depreciation.StraightLine(1_000, 0, 3);

        Assert.Equal([333.33m, 333.33m, 333.34m], schedule.Select(y => y.Expense));
        Assert.Throws<ArgumentException>(() => Depreciation.StraightLine(100, 200, 3));
    }

    [Theory]
    [InlineData(CostingMethod.Fifo, 1_600, 2_000)]            // sold 100 × 10 + 50 × 12
    [InlineData(CostingMethod.Lifo, 2_000, 1_600)]            // sold 100 × 14 + 50 × 12
    [InlineData(CostingMethod.WeightedAverage, 1_800, 1_800)] // 150 × the average 12
    public void Inventory_MethodsSplitTheSameTotalDifferently(CostingMethod method, decimal cogs, decimal closing)
    {
        var stock = new InventoryCosting(method);
        stock.Purchase(100, 10);
        stock.Purchase(100, 12);
        stock.Purchase(100, 14);

        stock.Sell(100);
        stock.Sell(50);

        Assert.Equal(cogs, stock.CostOfGoodsSold);
        Assert.Equal(closing, stock.StockValue);
        Assert.Equal(3_600, cogs + closing); // the total cost is always 3,600
        Assert.Throws<InvalidOperationException>(() => stock.Sell(1_000));
    }

    [Fact]
    public void Reconciliation_MatchesByReferenceThenByAmountAndDate()
    {
        BankLine[] bank =
        [
            new("b1", new(2026, 10, 3), -1_200, "RENT OCT"),
            new("b2", new(2026, 10, 7), 3_000, "INV-1001"),
            new("b3", new(2026, 10, 8), 450, ""),
            new("b4", new(2026, 10, 31), -15, "MONTHLY FEE"),
        ];
        BankLine[] books =
        [
            new("k1", new(2026, 10, 1), -1_200, "rent oct"),
            new("k2", new(2026, 10, 5), 3_000, "inv 1001"),
            new("k3", new(2026, 10, 6), 450, "cash sale"),
            new("k4", new(2026, 10, 30), -800, "supplier"),
        ];

        var result = Reconciliation.Match(bank, books);

        Assert.Equal([("b1", "k1"), ("b2", "k2"), ("b3", "k3")], result.Matched.Select(m => (m.Bank.Id, m.Book.Id)).Order());
        Assert.Equal(["b4"], result.UnmatchedBank.Select(l => l.Id));  // the fee isn't in the books yet
        Assert.Equal(["k4"], result.UnmatchedBook.Select(l => l.Id));  // the cheque hasn't cleared
        Assert.Equal(0, Reconciliation.Difference(bankBalance: 2_235, bookBalance: 1_450, result));
    }

    [Fact]
    public void Aging_BucketsReceivablesAndEstimatesLosses()
    {
        var asOf = new DateOnly(2026, 10, 31);
        Invoice[] invoices =
        [
            new("Acme", "1", asOf.AddDays(10), 1_000),
            new("Acme", "2", asOf.AddDays(-20), 500, Paid: 200),
            new("Acme", "3", asOf.AddDays(-100), 800),
            new("Globex", "4", asOf.AddDays(-45), 2_000),
            new("Globex", "5", asOf.AddDays(-5), 300, Paid: 300),
        ];

        var report = AgingReport.Build(invoices, asOf);

        Assert.Equal(1_000, report["Acme"]["Current"]);
        Assert.Equal(300, report["Acme"]["1-30"]);
        Assert.Equal(800, report["Acme"]["90+"]);
        Assert.Equal(2_000, report["Globex"]["31-60"]);
        Assert.Equal(0, report["Globex"]["1-30"]); // fully paid

        var rates = new Dictionary<string, decimal> { ["Current"] = 0.01m, ["1-30"] = 0.03m, ["31-60"] = 0.1m, ["61-90"] = 0.25m, ["90+"] = 0.5m };
        Assert.Equal(10 + 9 + 200 + 400, AgingReport.BadDebtAllowance(invoices, asOf, rates));
        Assert.Equal(45.6m, AgingReport.DaysSalesOutstanding(4_100, 27_000, 300));
    }
}
