namespace CSharpExercises.Patterns;

public interface IAuditLog
{
    void Record(string action);
}

public sealed class MemoryAuditLog : IAuditLog
{
    public List<string> Entries { get; } = [];
    public void Record(string action) => Entries.Add(action);
}

/// <summary>
/// Null object: instead of passing null and checking "if (log != null)" everywhere, pass an object that implements the
/// interface and does nothing. Code stays free of null checks and can't forget one. A singleton instance is enough
/// because it has no state. The same idea gives "guest" users, empty discount strategies and no-op loggers.
/// </summary>
public sealed class NullAuditLog : IAuditLog
{
    public static readonly NullAuditLog Instance = new();
    private NullAuditLog() { }
    public void Record(string action) { }
}

public sealed record Shopper(string Name, decimal Credit);

/// <summary>A null shopper with safe defaults, so "no shopper found" doesn't need special cases downstream.</summary>
public static class Shoppers
{
    public static readonly Shopper Guest = new("Guest", 0);

    public static Shopper FindOrGuest(IReadOnlyDictionary<string, Shopper> customers, string? id) =>
        id is not null && customers.TryGetValue(id, out var customer) ? customer : Guest;
}

public sealed class TransferService(IAuditLog? audit = null)
{
    private readonly IAuditLog _audit = audit ?? NullAuditLog.Instance; // no null checks below this line

    public decimal Transfer(decimal balance, decimal amount)
    {
        if (amount > balance) throw new InvalidOperationException("Insufficient funds.");
        _audit.Record($"transfer {amount}");
        return balance - amount;
    }
}
