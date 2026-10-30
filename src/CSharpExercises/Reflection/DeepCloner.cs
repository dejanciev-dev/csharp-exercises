using System.Reflection;
using System.Runtime.CompilerServices;

namespace CSharpExercises.Reflection;

/// <summary>
/// A general deep copy by reflection: walk every field (including private ones and compiler-generated backing fields),
/// copy value types and strings as they are, and clone reference types recursively. A map from original to clone keeps
/// shared references shared and makes cycles safe. Objects are created without running constructors, as serializers do.
/// Reflection on every field is slow, so this suits tooling and tests rather than hot paths.
/// </summary>
public static class DeepCloner
{
    public static T Clone<T>(T source) => (T)Clone(source, new Dictionary<object, object>(ReferenceEqualityComparer.Instance))!;

    private static object? Clone(object? source, Dictionary<object, object> copies)
    {
        if (source is null) return null;
        var type = source.GetType();
        if (type.IsPrimitive || type.IsEnum || source is string or decimal or DateTime or DateTimeOffset or TimeSpan or Guid or Type or Delegate)
            return source; // immutable, or (delegates, types) not meaningfully clonable
        if (copies.TryGetValue(source, out var existing)) return existing;

        if (source is Array array)
        {
            var copy = Array.CreateInstance(type.GetElementType()!, Enumerable.Range(0, array.Rank).Select(array.GetLength).ToArray());
            copies[source] = copy;
            var index = new int[array.Rank];
            foreach (var _ in Enumerable.Range(0, array.Length))
            {
                copy.SetValue(Clone(array.GetValue(index), copies), index);
                Increment(index, array);
            }
            return copy;
        }

        var clone = RuntimeHelpers.GetUninitializedObject(type);
        if (!type.IsValueType) copies[source] = clone; // register before recursing so cycles resolve to this clone
        for (var t = type; t is not null; t = t.BaseType)
            foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                field.SetValue(clone, Clone(field.GetValue(source), copies));
        return clone;
    }

    private static void Increment(int[] index, Array array)
    {
        for (var dim = array.Rank - 1; dim >= 0; dim--)
        {
            if (++index[dim] < array.GetLength(dim)) return;
            index[dim] = 0;
        }
    }
}
