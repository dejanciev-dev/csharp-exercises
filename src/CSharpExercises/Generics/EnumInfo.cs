using System.ComponentModel;
using System.Reflection;

namespace CSharpExercises.Generics;

/// <summary>
/// A static generic class as a per-type cache: the runtime creates a separate copy of the static fields for every
/// closed type (<c>EnumInfo&lt;Color&gt;</c>, <c>EnumInfo&lt;Size&gt;</c>, ...), and initialises each copy once,
/// thread-safely, the first time it is used. No dictionary keyed by <see cref="Type"/> and no locks needed.
/// </summary>
/// <remarks>The <c>struct, Enum</c> constraint rejects anything that is not an enum at compile time.</remarks>
public static class EnumInfo<TEnum> where TEnum : struct, Enum
{
    public static IReadOnlyList<TEnum> Values { get; } = Enum.GetValues<TEnum>();

    public static bool IsFlags { get; } = typeof(TEnum).IsDefined(typeof(FlagsAttribute), inherit: false);

    private static readonly Dictionary<string, TEnum> ByName =
        Enum.GetNames<TEnum>().ToDictionary(name => name, Enum.Parse<TEnum>, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<TEnum, string> Descriptions =
        typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static)
            .DistinctBy(field => field.GetValue(null)) // aliases (two names, one value) keep the first name
            .ToDictionary(
                field => (TEnum)field.GetValue(null)!,
                field => field.GetCustomAttribute<DescriptionAttribute>()?.Description ?? field.Name);

    /// <summary>
    /// Case-insensitive lookup by name only. Unlike <see cref="Enum.TryParse{TEnum}(string?, out TEnum)"/>, it does not
    /// accept numbers such as <c>"42"</c>, which would produce values the enum does not define.
    /// </summary>
    public static bool TryParse(string? name, out TEnum value)
    {
        value = default;
        return name is not null && ByName.TryGetValue(name.Trim(), out value);
    }

    /// <summary>The <see cref="DescriptionAttribute"/> text, or the name if there is none.</summary>
    public static string Describe(TEnum value) =>
        Descriptions.TryGetValue(value, out var description) ? description : value.ToString();
}

public static class GenericParsing
{
    /// <summary>
    /// Parses a comma-separated list into any type that implements <see cref="IParsable{TSelf}"/>:
    /// <c>int</c>, <c>decimal</c>, <c>DateOnly</c>, <c>Guid</c>, or your own types. The constraint uses static abstract
    /// interface members, so <c>T.Parse</c> can be called on the type parameter itself.
    /// </summary>
    public static List<T> ParseList<T>(string csv, IFormatProvider? provider = null) where T : IParsable<T> =>
        [.. csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
               .Select(item => T.Parse(item, provider))];

    /// <summary>Like <see cref="ParseList{T}"/>, but reports the items it could not parse instead of throwing at the first one.</summary>
    public static (List<T> Parsed, List<string> Invalid) TryParseList<T>(string csv, IFormatProvider? provider = null) where T : IParsable<T>
    {
        List<T> parsed = [];
        List<string> invalid = [];

        foreach (var item in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (T.TryParse(item, provider, out var value)) parsed.Add(value);
            else invalid.Add(item);
        }

        return (parsed, invalid);
    }
}
