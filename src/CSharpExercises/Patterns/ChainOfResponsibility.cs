namespace CSharpExercises.Patterns;

public sealed record Expense(string Description, decimal Amount, string Category = "general");

public sealed record ApprovalDecision(bool Approved, string By, string Reason);

/// <summary>
/// Chain of responsibility: an expense passes along a chain of approvers until one can decide. The sender doesn't
/// know who will handle it, and the chain can be reordered or extended (a fraud check, a category rule) without
/// touching the others. ASP.NET Core middleware is the same idea.
/// </summary>
public abstract class Approver(string name)
{
    private Approver? _next;

    public string Name { get; } = name;

    /// <summary>Links the next approver and returns it, so chains read left to right: a.Then(b).Then(c).</summary>
    public Approver Then(Approver next)
    {
        _next = next;
        return next;
    }

    public ApprovalDecision Handle(Expense expense) =>
        TryDecide(expense) ?? _next?.Handle(expense) ?? new ApprovalDecision(false, "nobody", "No approver could decide.");

    /// <summary>Returns a decision, or null to pass the expense along.</summary>
    protected abstract ApprovalDecision? TryDecide(Expense expense);
}

public sealed class SpendingLimitApprover(string name, decimal limit) : Approver(name)
{
    protected override ApprovalDecision? TryDecide(Expense expense) =>
        expense.Amount <= limit ? new ApprovalDecision(true, Name, $"Within the {limit:0} limit.") : null;
}

/// <summary>Can reject outright, which also ends the chain.</summary>
public sealed class ForbiddenCategoryApprover(params string[] forbidden) : Approver("policy")
{
    protected override ApprovalDecision? TryDecide(Expense expense) =>
        forbidden.Contains(expense.Category, StringComparer.OrdinalIgnoreCase)
            ? new ApprovalDecision(false, Name, $"'{expense.Category}' expenses are not allowed.")
            : null;
}

public static class ApprovalChain
{
    public static Approver CreateDefault()
    {
        var first = new ForbiddenCategoryApprover("gambling");
        first.Then(new SpendingLimitApprover("team lead", 500))
             .Then(new SpendingLimitApprover("manager", 5_000))
             .Then(new SpendingLimitApprover("director", 50_000));
        return first;
    }
}
