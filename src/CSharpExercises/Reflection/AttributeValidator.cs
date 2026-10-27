using System.Collections.Concurrent;
using System.Reflection;

namespace CSharpExercises.Reflection;

/// <summary>
/// Base class for rules declared on properties. A simplified version of
/// <c>System.ComponentModel.DataAnnotations</c>, to show how attribute-driven frameworks work underneath.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public abstract class RuleAttribute : Attribute
{
    /// <summary>Returns an error message, or <c>null</c> if <paramref name="value"/> is valid.</summary>
    public abstract string? Check(string propertyName, object? value);
}

public sealed class RequiredAttribute : RuleAttribute
{
    public override string? Check(string propertyName, object? value) => value switch
    {
        null => $"{propertyName} is required.",
        string s when string.IsNullOrWhiteSpace(s) => $"{propertyName} is required.",
        _ => null,
    };
}

public sealed class RangeAttribute(double min, double max) : RuleAttribute
{
    public double Min => min;
    public double Max => max;

    // null is left to [Required], so optional numbers can still have a range.
    public override string? Check(string propertyName, object? value) => value switch
    {
        null => null,
        IConvertible number when number.GetTypeCode() is >= TypeCode.SByte and <= TypeCode.Decimal
                                 && Convert.ToDouble(number) is var d && (d < min || d > max) =>
            $"{propertyName} must be between {min} and {max}.",
        _ => null,
    };
}

public sealed class MaxLengthAttribute(int length) : RuleAttribute
{
    public int Length => length;

    public override string? Check(string propertyName, object? value) =>
        value is string s && s.Length > length ? $"{propertyName} must be at most {length} characters." : null;
}

/// <summary>
/// Validates any object from the attributes on its properties.
/// Reflection (finding properties and attributes, building getters) runs once per type and is cached.
/// </summary>
public static class AttributeValidator
{
    private sealed record PropertyRules(string Name, Func<object, object?> Get, RuleAttribute[] Rules);

    private static readonly ConcurrentDictionary<Type, PropertyRules[]> Cache = new();

    public static IReadOnlyList<string> Validate(object instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        List<string> errors = [];
        foreach (var property in Cache.GetOrAdd(instance.GetType(), Inspect))
        {
            var value = property.Get(instance);
            errors.AddRange(property.Rules.Select(rule => rule.Check(property.Name, value)).OfType<string>());
        }
        return errors;
    }

    private static PropertyRules[] Inspect(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .Select(p => (Property: p, Rules: p.GetCustomAttributes<RuleAttribute>(inherit: true).ToArray()))
            .Where(x => x.Rules.Length > 0)
            .Select(x => new PropertyRules(x.Property.Name, PropertyAccessor.CreateGetter(x.Property), x.Rules))
            .ToArray();
}
