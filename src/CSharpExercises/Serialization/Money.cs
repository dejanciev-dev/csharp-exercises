using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CSharpExercises.Serialization;

/// <summary>
/// An amount in a currency. In JSON it is the single string <c>"12.50 EUR"</c> instead of an object,
/// thanks to <see cref="MoneyJsonConverter"/>.
/// </summary>
[JsonConverter(typeof(MoneyJsonConverter))]
public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money Parse(string text)
    {
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts is not [var amount, var currency]
            || !decimal.TryParse(amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            || currency.Length != 3 || !currency.All(char.IsAsciiLetterUpper))
        {
            throw new FormatException($"'{text}' is not an amount followed by a currency code, like '12.50 EUR'.");
        }

        return new Money(value, currency);
    }

    public static Money operator +(Money left, Money right) =>
        left.Currency == right.Currency
            ? left with { Amount = left.Amount + right.Amount }
            : throw new InvalidOperationException($"Cannot add {left.Currency} and {right.Currency}.");

    public static Money operator *(Money money, int quantity) => money with { Amount = money.Amount * quantity };

    // decimal keeps its scale, so 12.50m prints as "12.50" and round-trips exactly.
    public override string ToString() => $"{Amount.ToString(CultureInfo.InvariantCulture)} {Currency}";
}

public sealed class MoneyJsonConverter : JsonConverter<Money>
{
    public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Expected money as a string like \"12.50 EUR\", got {reader.TokenType}.");

        try
        {
            return Money.Parse(reader.GetString()!);
        }
        catch (FormatException ex)
        {
            // For a JsonException the serializer fills in Path (e.g. $[0].lines[0].unitPrice); other exceptions escape as they are.
            throw new JsonException(ex.Message, ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
