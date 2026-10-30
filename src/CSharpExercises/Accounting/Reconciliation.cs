namespace CSharpExercises.Accounting;

public sealed record BankLine(string Id, DateOnly Date, decimal Amount, string Reference);

public sealed record ReconciliationResult(
    IReadOnlyList<(BankLine Bank, BankLine Book)> Matched,
    IReadOnlyList<BankLine> UnmatchedBank,
    IReadOnlyList<BankLine> UnmatchedBook);

/// <summary>
/// Bank reconciliation: match the bank statement against the company's own records. Exact matches (same amount and
/// reference) go first, then looser ones (same amount within a few days, since the bank books later), each record used
/// once. Whatever remains is the work for a human: missing entries, bank fees, typos. Greedy in order of confidence.
/// </summary>
public static class Reconciliation
{
    public static ReconciliationResult Match(IEnumerable<BankLine> bank, IEnumerable<BankLine> books, int dayTolerance = 3)
    {
        var remainingBank = bank.ToList();
        var remainingBooks = books.ToList();
        var matched = new List<(BankLine, BankLine)>();

        void Pass(Func<BankLine, BankLine, bool> isMatch)
        {
            foreach (var b in remainingBank.ToList())
            {
                // Among candidates, take the closest date so near-duplicates pair up sensibly.
                var candidate = remainingBooks.Where(k => isMatch(b, k)).MinBy(k => Math.Abs(k.Date.DayNumber - b.Date.DayNumber));
                if (candidate is null) continue;
                matched.Add((b, candidate));
                remainingBank.Remove(b);
                remainingBooks.Remove(candidate);
            }
        }

        Pass((b, k) => b.Amount == k.Amount && Normalise(b.Reference) == Normalise(k.Reference) && Normalise(b.Reference).Length > 0);
        Pass((b, k) => b.Amount == k.Amount && Math.Abs(b.Date.DayNumber - k.Date.DayNumber) <= dayTolerance);

        return new ReconciliationResult(matched, remainingBank, remainingBooks);
    }

    /// <summary>Statement balance adjusted for items not yet on both sides should equal the book balance.</summary>
    public static decimal Difference(decimal bankBalance, decimal bookBalance, ReconciliationResult result) =>
        bankBalance - result.UnmatchedBank.Sum(l => l.Amount) - (bookBalance - result.UnmatchedBook.Sum(l => l.Amount));

    private static string Normalise(string reference) => new(reference.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
}
