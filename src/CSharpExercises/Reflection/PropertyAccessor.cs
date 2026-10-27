using System.Linq.Expressions;
using System.Reflection;

namespace CSharpExercises.Reflection;

/// <summary>
/// Turns a <see cref="PropertyInfo"/> into a compiled delegate with expression trees.
/// <c>PropertyInfo.GetValue</c> pays for reflection on every call; the compiled getter pays once, when it is built,
/// and is then about as fast as reading the property directly.
/// </summary>
public static class PropertyAccessor
{
    /// <summary>Builds <c>instance =&gt; (object)((TDeclaring)instance).Property</c>.</summary>
    public static Func<object, object?> CreateGetter(PropertyInfo property)
    {
        if (property.GetMethod is not { } getter) throw new ArgumentException($"{property.Name} has no getter.", nameof(property));

        var instance = Expression.Parameter(typeof(object), "instance");
        var target = getter.IsStatic ? null : Expression.Convert(instance, property.DeclaringType!);
        var body = Expression.Convert(Expression.Property(target, property), typeof(object));

        return Expression.Lambda<Func<object, object?>>(body, instance).Compile();
    }

    /// <summary>Builds <c>(instance, value) =&gt; ((TDeclaring)instance).Property = (TProperty)value</c>.</summary>
    public static Action<object, object?> CreateSetter(PropertyInfo property)
    {
        if (property.SetMethod is not { } setter) throw new ArgumentException($"{property.Name} has no setter.", nameof(property));

        var instance = Expression.Parameter(typeof(object), "instance");
        var value = Expression.Parameter(typeof(object), "value");
        var target = setter.IsStatic ? null : Expression.Convert(instance, property.DeclaringType!);
        var body = Expression.Assign(Expression.Property(target, property), Expression.Convert(value, property.PropertyType));

        return Expression.Lambda<Action<object, object?>>(body, instance, value).Compile();
    }
}
