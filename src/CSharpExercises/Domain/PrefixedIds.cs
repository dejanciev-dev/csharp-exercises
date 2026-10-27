using System.Globalization;

namespace CSharpExercises.Domain;

/// <summary>
/// A strongly typed id: a <see cref="Guid"/> wrapped in its own type, so passing an order id where a customer id is
/// expected is a compile error instead of a bug found in production. The static abstract members let generic code
/// (<see cref="PrefixedId"/>) create, format and parse any id type without reflection.
/// </summary>
public interface IPrefixedId<TSelf> where TSelf : struct, IPrefixedId<TSelf>
{
    static abstract string Prefix { get; }
    static abstract TSelf From(Guid value);
    Guid Value { get; }
}

public readonly record struct CustomerId(Guid Value) : IPrefixedId<CustomerId>
{
    public static string Prefix => "cus";
    public static CustomerId From(Guid value) => new(value);
    public override string ToString() => PrefixedId.Format(this);
}

public readonly record struct OrderId(Guid Value) : IPrefixedId<OrderId>
{
    public static string Prefix => "ord";
    public static OrderId From(Guid value) => new(value);
    public override string ToString() => PrefixedId.Format(this);
}

/// <summary>
/// Ids written as <c>cus_0192f3a8...</c>, the style Stripe made popular: the prefix says what the id is for,
/// so an id pasted into the wrong place is rejected with a clear message instead of "not found".
/// </summary>
public static class PrefixedId
{
    /// <summary>
    /// A new id based on a version 7 GUID, which starts with the creation time in milliseconds. Ids created later sort
    /// later, so database indexes on them stay in insert order instead of fragmenting the way random GUIDs do.
    /// </summary>
    public static T New<T>(DateTimeOffset? createdAt = null) where T : struct, IPrefixedId<T> =>
        T.From(createdAt is { } at ? Guid.CreateVersion7(at) : Guid.CreateVersion7());

    public static string Format<T>(T id) where T : struct, IPrefixedId<T> => $"{T.Prefix}_{id.Value:N}";

    public static bool TryParse<T>(string? text, out T id) where T : struct, IPrefixedId<T>
    {
        id = default;
        var prefix = T.Prefix + "_";
        if (text is null || !text.StartsWith(prefix, StringComparison.Ordinal)) return false;
        if (!Guid.TryParseExact(text.AsSpan(prefix.Length), "N", out var value)) return false;

        id = T.From(value);
        return true;
    }

    public static T Parse<T>(string text) where T : struct, IPrefixedId<T>
    {
        if (TryParse<T>(text, out var id)) return id;

        var separator = text.IndexOf('_');
        throw new FormatException(separator > 0 && text[..separator] != T.Prefix
            ? $"'{text}' is a '{text[..separator]}' id, but a '{T.Prefix}' id was expected."
            : $"'{text}' is not a valid '{T.Prefix}' id.");
    }

    /// <summary>Reads the creation time back out of a version 7 id: the first 48 bits are Unix milliseconds.</summary>
    public static DateTimeOffset CreatedAt<T>(T id) where T : struct, IPrefixedId<T>
    {
        if (id.Value.Version != 7) throw new ArgumentException("Only version 7 ids carry a timestamp.", nameof(id));
        var hex = id.Value.ToString("N")[..12];
        return DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }
}
