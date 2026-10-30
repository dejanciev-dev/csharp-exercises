namespace CSharpExercises.Accounting;

public enum AccountType { Asset, Liability, Equity, Income, Expense }

public sealed record Account(string Code, string Name, AccountType Type)
{
    /// <summary>Assets and expenses grow with debits; liabilities, equity and income grow with credits.</summary>
    public bool IsDebitNormal => Type is AccountType.Asset or AccountType.Expense;
}

public sealed record Posting(string Account, decimal Debit, decimal Credit);

public sealed record JournalEntry(DateOnly Date, string Description, IReadOnlyList<Posting> Postings);

/// <summary>
/// Double-entry bookkeeping: every transaction moves value between at least two accounts, and total debits must equal
/// total credits, so the books always balance (assets = liabilities + equity + income - expenses). An unbalanced entry
/// is rejected before it's posted. The trial balance and the balance sheet equation are the built-in checksums.
/// </summary>
public sealed class Ledger
{
    private readonly Dictionary<string, Account> _accounts = new();
    private readonly List<JournalEntry> _journal = [];

    public Ledger AddAccount(string code, string name, AccountType type)
    {
        _accounts.Add(code, new Account(code, name, type));
        return this;
    }

    public IReadOnlyList<JournalEntry> Journal => _journal;

    public void Post(DateOnly date, string description, params Posting[] postings)
    {
        if (postings.Length < 2) throw new InvalidOperationException("A journal entry needs at least two postings.");
        if (postings.Any(p => p.Debit < 0 || p.Credit < 0 || (p.Debit > 0) == (p.Credit > 0)))
            throw new InvalidOperationException("Each posting is either a positive debit or a positive credit.");
        if (postings.FirstOrDefault(p => !_accounts.ContainsKey(p.Account)) is { } unknown)
            throw new KeyNotFoundException($"Unknown account '{unknown.Account}'.");

        var (debits, credits) = (postings.Sum(p => p.Debit), postings.Sum(p => p.Credit));
        if (debits != credits) throw new InvalidOperationException($"Unbalanced entry: debits {debits} ≠ credits {credits}.");
        _journal.Add(new JournalEntry(date, description, postings));
    }

    public static Posting Debit(string account, decimal amount) => new(account, amount, 0);
    public static Posting Credit(string account, decimal amount) => new(account, 0, amount);

    /// <summary>Balance in the account's normal direction, so it's positive in the usual case.</summary>
    public decimal Balance(string code, DateOnly? asOf = null)
    {
        var account = _accounts[code];
        var postings = _journal.Where(e => asOf is null || e.Date <= asOf).SelectMany(e => e.Postings).Where(p => p.Account == code);
        var net = postings.Sum(p => p.Debit - p.Credit);
        return account.IsDebitNormal ? net : -net;
    }

    /// <summary>Every account's balance in debit and credit columns; the column totals must match.</summary>
    public IReadOnlyList<(string Account, decimal Debit, decimal Credit)> TrialBalance() =>
        _accounts.Values.OrderBy(a => a.Code).Select(a =>
        {
            var net = _journal.SelectMany(e => e.Postings).Where(p => p.Account == a.Code).Sum(p => p.Debit - p.Credit);
            return (a.Code, Math.Max(net, 0), Math.Max(-net, 0));
        }).Where(r => r.Item2 != 0 || r.Item3 != 0).ToList();

    public decimal Total(AccountType type) => _accounts.Values.Where(a => a.Type == type).Sum(a => Balance(a.Code));

    public decimal NetIncome => Total(AccountType.Income) - Total(AccountType.Expense);

    /// <summary>Assets = Liabilities + Equity + Net income (before closing the books).</summary>
    public bool Balances => Total(AccountType.Asset) == Total(AccountType.Liability) + Total(AccountType.Equity) + NetIncome;
}
