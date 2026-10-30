namespace CSharpExercises.Commerce;

/// <summary>
/// Converting money between currencies. Rate feeds usually quote everything against one base currency, so EUR → JPY
/// goes through the base (triangulation) instead of needing a rate for every pair. Results round to each currency's
/// minor unit: 2 decimals for most, 0 for yen, 3 for Kuwaiti dinar. Splitting the difference between buy and sell
/// rates is how exchange offices make money, so the spread is a parameter.
/// </summary>
public sealed class CurrencyConverter(string baseCurrency, IReadOnlyDictionary<string, decimal> unitsPerBase)
{
    private static readonly Dictionary<string, int> MinorUnits = new() { ["JPY"] = 0, ["KRW"] = 0, ["KWD"] = 3, ["BHD"] = 3 };

    public string BaseCurrency { get; } = baseCurrency;

    public static int DecimalsOf(string currency) => MinorUnits.GetValueOrDefault(currency, 2);

    public decimal Rate(string from, string to) => UnitsPerBase(to) / UnitsPerBase(from);

    /// <param name="spreadPercent">The margin taken: the customer receives this much less than the mid-market rate.</param>
    public decimal Convert(decimal amount, string from, string to, decimal spreadPercent = 0)
    {
        var converted = amount * Rate(from, to) * (1 - spreadPercent / 100);
        return Math.Round(converted, DecimalsOf(to), MidpointRounding.ToEven); // banker's rounding avoids bias over many conversions
    }

    /// <summary>What converting there and back costs: the spread is paid twice, while rounding alone can go either way.</summary>
    public decimal RoundTripLoss(decimal amount, string from, string via, decimal spreadPercent = 0) =>
        amount - Convert(Convert(amount, from, via, spreadPercent), via, from, spreadPercent);

    public string Format(decimal amount, string currency) =>
        $"{amount.ToString($"N{DecimalsOf(currency)}", System.Globalization.CultureInfo.InvariantCulture)} {currency}";

    private decimal UnitsPerBase(string currency) =>
        currency == BaseCurrency ? 1
        : unitsPerBase.TryGetValue(currency, out var rate) && rate > 0 ? rate
        : throw new KeyNotFoundException($"No rate for {currency}.");
}
