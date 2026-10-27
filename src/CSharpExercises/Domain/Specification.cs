using System.Linq.Expressions;

namespace CSharpExercises.Domain;

/// <summary>
/// A business rule as a reusable, composable object: <c>Vip &amp; InCountry("MK") &amp; !Inactive</c>.
/// Rules are kept as expression trees rather than delegates, so the same rule can run in memory
/// (<see cref="IsSatisfiedBy"/>) or be translated by a LINQ provider such as Entity Framework into a SQL WHERE clause.
/// </summary>
public sealed class Specification<T>(Expression<Func<T, bool>> criteria)
{
    private Func<T, bool>? _compiled;

    public Expression<Func<T, bool>> Criteria => criteria;

    public bool IsSatisfiedBy(T candidate) => (_compiled ??= criteria.Compile())(candidate);

    public Specification<T> And(Specification<T> other) => Combine(other, Expression.AndAlso);

    public Specification<T> Or(Specification<T> other) => Combine(other, Expression.OrElse);

    public Specification<T> Not() => new(Expression.Lambda<Func<T, bool>>(Expression.Not(criteria.Body), criteria.Parameters));

    public static Specification<T> operator &(Specification<T> left, Specification<T> right) => left.And(right);
    public static Specification<T> operator |(Specification<T> left, Specification<T> right) => left.Or(right);
    public static Specification<T> operator !(Specification<T> spec) => spec.Not();

    /// <summary>
    /// Each lambda has its own parameter (<c>c =&gt; ...</c> and <c>x =&gt; ...</c>). Gluing the bodies together as they are
    /// leaves the second body referring to a parameter the new lambda does not declare. So the other body is rewritten
    /// to use this lambda's parameter first.
    /// </summary>
    private Specification<T> Combine(Specification<T> other, Func<Expression, Expression, BinaryExpression> merge)
    {
        var parameter = criteria.Parameters[0];
        var otherBody = new ParameterReplacer(other.Criteria.Parameters[0], parameter).Visit(other.Criteria.Body);
        return new(Expression.Lambda<Func<T, bool>>(merge(criteria.Body, otherBody), parameter));
    }

    private sealed class ParameterReplacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : node;
    }
}

public sealed record Customer(string Name, string Country, decimal LifetimeSpend, DateOnly? LastOrder);

/// <summary>Named rules: the vocabulary of the business, defined once and combined where needed.</summary>
public static class CustomerRules
{
    public static Specification<Customer> Vip { get; } = new(c => c.LifetimeSpend >= 10_000m);

    public static Specification<Customer> InCountry(string country) => new(c => c.Country == country);

    public static Specification<Customer> OrderedSince(DateOnly date) => new(c => c.LastOrder != null && c.LastOrder >= date);
}
