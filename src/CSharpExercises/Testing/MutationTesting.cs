using System.Linq.Expressions;

namespace CSharpExercises.Testing;

public sealed record Mutant(string Description, bool Killed);

/// <summary>
/// Mutation testing measures how good tests are, not how much code they run: make small deliberate bugs ("mutants"),
/// such as turning &lt; into &lt;= or + into -, and check that some test fails for each. A surviving mutant shows a
/// behaviour no test pins down. Here the code is an expression tree, so mutants are made by rewriting the tree with an
/// <see cref="ExpressionVisitor"/> one operator at a time. Some mutants are "equivalent" (they behave identically), so a
/// score below 100% needs a human look. Tools like Stryker.NET do this to compiled code.
/// </summary>
public static class MutationTesting
{
    public static IReadOnlyList<Mutant> Run<T>(Expression<Func<T, T, T>> function, IEnumerable<Func<Func<T, T, T>, bool>> tests)
    {
        var testList = tests.ToList();
        if (!testList.All(t => t(function.Compile()))) throw new InvalidOperationException("The tests must pass on the original code first.");

        var mutants = new List<Mutant>();
        var count = new OperatorCounter();
        count.Visit(function);

        for (var i = 0; i < count.Total; i++)
        {
            var mutator = new SingleMutation(i);
            var mutated = (Expression<Func<T, T, T>>)mutator.Visit(function);
            if (mutator.Description is null) continue;
            var compiled = mutated.Compile();
            var killed = testList.Any(t =>
            {
                try { return !t(compiled); }
                catch { return true; } // a mutant that crashes the test is also caught
            });
            mutants.Add(new Mutant(mutator.Description, killed));
        }
        return mutants;
    }

    public static double Score(IReadOnlyList<Mutant> mutants) => mutants.Count == 0 ? 1 : (double)mutants.Count(m => m.Killed) / mutants.Count;

    private static readonly Dictionary<ExpressionType, ExpressionType> Swaps = new()
    {
        [ExpressionType.Add] = ExpressionType.Subtract, [ExpressionType.Subtract] = ExpressionType.Add,
        [ExpressionType.Multiply] = ExpressionType.Divide, [ExpressionType.Divide] = ExpressionType.Multiply,
        [ExpressionType.LessThan] = ExpressionType.LessThanOrEqual, [ExpressionType.LessThanOrEqual] = ExpressionType.LessThan,
        [ExpressionType.GreaterThan] = ExpressionType.GreaterThanOrEqual, [ExpressionType.GreaterThanOrEqual] = ExpressionType.GreaterThan,
    };

    private sealed class OperatorCounter : ExpressionVisitor
    {
        public int Total { get; private set; }
        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (Swaps.ContainsKey(node.NodeType)) Total++;
            return base.VisitBinary(node);
        }
    }

    /// <summary>Rewrites only the n-th swappable operator, leaving the rest of the tree as it was.</summary>
    private sealed class SingleMutation(int target) : ExpressionVisitor
    {
        private int _seen;
        public string? Description { get; private set; }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (Swaps.TryGetValue(node.NodeType, out var replacement) && _seen++ == target)
            {
                Description = $"{node.NodeType} → {replacement} in ({node})";
                return Expression.MakeBinary(replacement, Visit(node.Left), Visit(node.Right));
            }
            return base.VisitBinary(node);
        }
    }
}
