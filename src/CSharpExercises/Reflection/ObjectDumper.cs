using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace CSharpExercises.Reflection;

/// <summary>
/// Printing any object graph for debugging, like LINQPad's Dump: public properties via reflection, indented, collections
/// listed, simple values inline. Object graphs can contain cycles (parent ↔ child), so visited objects are tracked by
/// reference and printed as a back-reference instead of recursing forever; a depth limit keeps huge graphs readable.
/// </summary>
public static class ObjectDumper
{
    public static string Dump(object? value, int maxDepth = 5)
    {
        var sb = new StringBuilder();
        Write(value, sb, 0, maxDepth, new HashSet<object>(ReferenceEqualityComparer.Instance));
        return sb.ToString().TrimEnd('\n');
    }

    private static void Write(object? value, StringBuilder sb, int depth, int maxDepth, HashSet<object> path)
    {
        if (IsSimple(value))
        {
            sb.Append(Format(value)).Append('\n');
            return;
        }

        var type = value!.GetType();
        if (!path.Add(value))
        {
            sb.Append($"<cycle: {type.Name}>\n");
            return;
        }
        if (depth >= maxDepth)
        {
            sb.Append($"{type.Name} {{…}}\n");
            path.Remove(value);
            return;
        }

        var indent = new string(' ', 2 * (depth + 1));
        if (value is IEnumerable items)
        {
            var list = items.Cast<object?>().ToList();
            sb.Append($"{TypeName(type)} ({list.Count} items)\n");
            for (var i = 0; i < list.Count; i++)
            {
                sb.Append($"{indent}[{i}]: ");
                Write(list[i], sb, depth + 1, maxDepth, path);
            }
        }
        else
        {
            sb.Append(type.Name).Append('\n');
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetIndexParameters().Length == 0))
            {
                sb.Append($"{indent}{property.Name}: ");
                object? propertyValue;
                try { propertyValue = property.GetValue(value); }
                catch (TargetInvocationException e) { propertyValue = $"<threw {e.InnerException?.GetType().Name}>"; }
                Write(propertyValue, sb, depth + 1, maxDepth, path);
            }
        }
        path.Remove(value); // shared (non-cyclic) references may appear again elsewhere
    }

    private static bool IsSimple(object? value) =>
        value is null or string or Enum or DateTime or DateTimeOffset or TimeSpan or Guid or decimal || value.GetType().IsPrimitive;

    private static string Format(object? value) => value switch
    {
        null => "null",
        string s => $"\"{s}\"",
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };

    private static string TypeName(Type type) =>
        type.IsArray ? $"{type.GetElementType()!.Name}[]"
        : type.IsGenericType ? $"{type.Name[..type.Name.IndexOf('`')]}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>"
        : type.Name;
}
