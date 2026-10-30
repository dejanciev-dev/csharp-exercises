namespace CSharpExercises.Commerce;

public sealed record InvoiceLine(string Description, int Quantity, decimal UnitPrice, decimal VatRatePercent);

public sealed record InvoiceTotals(decimal Net, IReadOnlyDictionary<decimal, decimal> VatByRate, decimal Gross)
{
    public decimal Vat => VatByRate.Values.Sum();
}

/// <summary>
/// VAT on invoices, where cents matter and the rounding rule is a legal choice: rounding the tax per line and adding up
/// can differ by a cent or more from rounding the tax on the total per rate. Prices may also be quoted including VAT
/// (consumer prices in most of Europe), and extracting the tax from a gross price is not "subtract 20%".
/// </summary>
public static class VatCalculator
{
    public static InvoiceTotals PerLine(IEnumerable<InvoiceLine> lines)
    {
        var list = lines.ToList();
        var vat = list.GroupBy(l => l.VatRatePercent).ToDictionary(g => g.Key, g => g.Sum(l => Round(l.Quantity * l.UnitPrice * l.VatRatePercent / 100)));
        var net = list.Sum(l => Round(l.Quantity * l.UnitPrice));
        return new InvoiceTotals(net, vat, net + vat.Values.Sum());
    }

    public static InvoiceTotals PerRate(IEnumerable<InvoiceLine> lines)
    {
        var list = lines.ToList();
        var vat = list.GroupBy(l => l.VatRatePercent).ToDictionary(g => g.Key, g => Round(g.Sum(l => l.Quantity * l.UnitPrice) * g.Key / 100));
        var net = Round(list.Sum(l => l.Quantity * l.UnitPrice));
        return new InvoiceTotals(net, vat, net + vat.Values.Sum());
    }

    /// <summary>Net and VAT contained in a gross price: gross × rate / (100 + rate), not gross × rate / 100.</summary>
    public static (decimal Net, decimal Vat) FromGross(decimal gross, decimal ratePercent)
    {
        var vat = Round(gross * ratePercent / (100 + ratePercent));
        return (gross - vat, vat);
    }

    public static decimal ToGross(decimal net, decimal ratePercent) => Round(net * (1 + ratePercent / 100));

    /// <summary>Commercial rounding: halves go away from zero (banker's rounding is .NET's default for Math.Round).</summary>
    public static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
