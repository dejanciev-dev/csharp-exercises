namespace CSharpExercises.Oop;

/// <summary>
/// Encapsulation: the balance only changes through validated operations, and every change is recorded.
/// </summary>
public class BankAccount(string owner)
{
    private readonly List<Transaction> _transactions = [];

    public string Owner { get; } = owner;
    public decimal Balance { get; private set; }
    public IReadOnlyList<Transaction> Transactions => _transactions;

    public void Deposit(decimal amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        Balance += amount;
        _transactions.Add(new Transaction(amount, DateTime.UtcNow, "Deposit"));
    }

    public void Withdraw(decimal amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        if (amount > Balance) throw new InvalidOperationException("Insufficient funds.");

        Balance -= amount;
        _transactions.Add(new Transaction(-amount, DateTime.UtcNow, "Withdrawal"));
    }

    public void TransferTo(BankAccount target, decimal amount)
    {
        Withdraw(amount);
        target.Deposit(amount);
    }
}

public record Transaction(decimal Amount, DateTime Date, string Note);
