using System.Linq.Expressions;
using System.Reflection;

namespace CSharpExercises.Reflection;

/// <summary>
/// An AutoMapper-style mapper: copy properties with the same name (and compatible types) from a source type to a
/// destination type. Reflection is used once to build an expression tree, which is compiled to a delegate and cached, so
/// each later mapping runs at hand-written speed. Custom rules override or add members, and a validation step lists
/// destination properties nobody fills, which catches renamed fields at start-up instead of as silent nulls.
/// </summary>
public sealed class ObjectMapper<TSource, TDestination> where TDestination : new()
{
    private readonly Dictionary<string, LambdaExpression> _custom = new();
    private readonly HashSet<string> _ignored = [];
    private Func<TSource, TDestination>? _compiled;

    public ObjectMapper<TSource, TDestination> ForMember<TValue>(Expression<Func<TDestination, TValue>> destination, Expression<Func<TSource, TValue>> source)
    {
        _custom[MemberName(destination)] = source;
        _compiled = null;
        return this;
    }

    public ObjectMapper<TSource, TDestination> Ignore<TValue>(Expression<Func<TDestination, TValue>> destination)
    {
        _ignored.Add(MemberName(destination));
        _compiled = null;
        return this;
    }

    public TDestination Map(TSource source) => (_compiled ??= Compile())(source);

    /// <summary>Destination properties that no source property or rule maps to.</summary>
    public IReadOnlyList<string> UnmappedMembers() =>
        Writable().Where(p => !_custom.ContainsKey(p.Name) && !_ignored.Contains(p.Name) && SourceFor(p) is null).Select(p => p.Name).ToList();

    private Func<TSource, TDestination> Compile()
    {
        var source = Expression.Parameter(typeof(TSource), "source");
        var bindings = new List<MemberBinding>();

        foreach (var property in Writable())
        {
            if (_ignored.Contains(property.Name)) continue;
            Expression? value = null;
            if (_custom.TryGetValue(property.Name, out var rule))
                value = new ParameterReplacer(rule.Parameters[0], source).Visit(rule.Body); // inline the rule's body
            else if (SourceFor(property) is { } match)
                value = Expression.Property(source, match);
            if (value is null) continue;

            if (value.Type != property.PropertyType) value = Expression.Convert(value, property.PropertyType); // e.g. int → long
            bindings.Add(Expression.Bind(property, value));
        }

        // source => new TDestination { A = source.A, B = <custom rule>, … }
        var body = Expression.MemberInit(Expression.New(typeof(TDestination)), bindings);
        return Expression.Lambda<Func<TSource, TDestination>>(body, source).Compile();
    }

    private static IEnumerable<PropertyInfo> Writable() =>
        typeof(TDestination).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite);

    private static PropertyInfo? SourceFor(PropertyInfo destination)
    {
        var source = typeof(TSource).GetProperty(destination.Name, BindingFlags.Public | BindingFlags.Instance);
        if (source is null || !source.CanRead) return null;
        var (from, to) = (source.PropertyType, destination.PropertyType);
        var convertible = to.IsAssignableFrom(from) || (from.IsPrimitive && to.IsPrimitive && Type.GetTypeCode(to) > Type.GetTypeCode(from));
        return convertible ? source : null;
    }

    private static string MemberName(LambdaExpression expression) =>
        expression.Body is MemberExpression m ? m.Member.Name : throw new ArgumentException("Expected a property access like x => x.Name.");

    private sealed class ParameterReplacer(ParameterExpression from, Expression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : base.VisitParameter(node);
    }
}
