using CSharpExercises.Commerce;
using Microsoft.Extensions.Time.Testing;

namespace CSharpExercises.Tests;

public class CommerceTests
{
    [Fact]
    public void Vat_RoundingPerLineAndPerRateCanDiffer()
    {
        InvoiceLine[] lines = [new("pen", 1, 0.15m, 19), new("pad", 1, 0.15m, 19), new("ink", 1, 0.15m, 19)];

        var perLine = VatCalculator.PerLine(lines);
        var perRate = VatCalculator.PerRate(lines);

        Assert.Equal(0.09m, perLine.Vat); // 0.0285 rounds to 0.03, three times
        Assert.Equal(0.09m, perRate.Vat); // 0.0855 rounds to 0.09
        InvoiceLine[] many = [.. Enumerable.Repeat(new InvoiceLine("bolt", 1, 0.05m, 19), 10)];
        Assert.Equal(0.10m, VatCalculator.PerLine(many).Vat); // 0.0095 → 0.01, ten times
        Assert.Equal(0.10m, VatCalculator.PerRate(many).Vat); // 0.095 → 0.10
        InvoiceLine[] odd = [.. Enumerable.Repeat(new InvoiceLine("clip", 1, 0.02m, 19), 10)];
        Assert.Equal(0.00m, VatCalculator.PerLine(odd).Vat);  // 0.0038 → 0.00 each time
        Assert.Equal(0.04m, VatCalculator.PerRate(odd).Vat);  // 0.038 → 0.04 on the total
    }

    [Fact]
    public void Vat_MixedRatesAndGrossPrices()
    {
        var totals = VatCalculator.PerRate([new("book", 2, 10m, 7), new("lamp", 1, 50m, 19)]);

        Assert.Equal(70m, totals.Net);
        Assert.Equal(1.40m, totals.VatByRate[7]);
        Assert.Equal(9.50m, totals.VatByRate[19]);
        Assert.Equal(80.90m, totals.Gross);

        Assert.Equal((100m, 20m), VatCalculator.FromGross(120m, 20)); // not 120 - 24
        Assert.Equal(119m, VatCalculator.ToGross(100m, 19));
        Assert.Equal(0.13m, VatCalculator.Round(0.125m)); // away from zero, not to even
    }

    private static readonly TieredPricing ApiPricing = new([new(1_000, 0.10m), new(10_000, 0.08m), new(null, 0.05m)]);

    [Fact]
    public void Tiers_VolumeVersusGraduated()
    {
        Assert.Equal(100m, ApiPricing.Volume(1_000));
        Assert.Equal(400m, ApiPricing.Volume(5_000));       // every unit at 0.08
        Assert.Equal(420m, ApiPricing.Graduated(5_000));    // 1000 × 0.10 + 4000 × 0.08
        Assert.Equal(0m, ApiPricing.Graduated(0));
        Assert.Equal(100m + 720m + 250m, ApiPricing.Graduated(15_000));
    }

    [Fact]
    public void Tiers_VolumePricingHasCliffs()
    {
        Assert.Equal([1_000, 10_000], ApiPricing.VolumeCliffs(20_000));
        Assert.True(ApiPricing.Volume(1_001) < ApiPricing.Volume(1_000)); // 80.08 < 100
        Assert.Throws<ArgumentException>(() => new TieredPricing([new(10, 1m)]));
    }

    [Fact]
    public void Proration_UpgradeMidMonth()
    {
        // February 2026 has 28 days; upgrading on the 15th leaves 14 of them.
        var lines = Proration.ChangePlan(28m, 56m, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 15), "Basic", "Pro");

        Assert.Equal(-14m, lines[0].Amount);
        Assert.Equal(28m, lines[1].Amount);
        Assert.Equal(14m, Proration.NetCharge(lines));
        Assert.Contains("14 of 28 days", lines[1].Description);

        var downgrade = Proration.ChangePlan(30m, 10m, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 21), "Pro", "Basic");
        Assert.Equal(-6.67m, Proration.NetCharge(downgrade)); // a credit
    }

    [Fact]
    public void Proration_FirstPartialPeriod()
    {
        Assert.Equal(16.13m, Proration.FirstPartialPeriod(50m, new DateOnly(2026, 1, 22), billingDay: 1)); // 10 of 31 days
        Assert.Equal(50m, Proration.FirstPartialPeriod(50m, new DateOnly(2026, 1, 1), billingDay: 1));
    }

    [Fact]
    public void Reservations_HoldStockAndExpire()
    {
        var time = new FakeTimeProvider();
        var stock = new InventoryReservations(time, TimeSpan.FromMinutes(15));
        stock.Receive("SKU-1", 5);

        var alice = stock.TryReserve("SKU-1", 3)!;
        Assert.Equal(2, stock.Available("SKU-1"));
        Assert.Null(stock.TryReserve("SKU-1", 3)); // only 2 left to promise

        time.Advance(TimeSpan.FromMinutes(16)); // Alice wandered off
        Assert.Equal(5, stock.Available("SKU-1"));
        Assert.False(stock.Confirm(alice.Id));

        var bob = stock.TryReserve("SKU-1", 4)!;
        Assert.True(stock.Confirm(bob.Id));
        Assert.Equal((1, 1), (stock.OnHand("SKU-1"), stock.Available("SKU-1")));
    }

    [Fact]
    public void Reservations_CanBeReleased()
    {
        var stock = new InventoryReservations(new FakeTimeProvider(), TimeSpan.FromMinutes(15));
        stock.Receive("SKU-2", 1);
        var r = stock.TryReserve("SKU-2", 1)!;

        stock.Release(r.Id);

        Assert.Equal(1, stock.Available("SKU-2"));
    }

    private static readonly CurrencyConverter Fx = new("EUR", new Dictionary<string, decimal> { ["USD"] = 1.08m, ["JPY"] = 162.5m, ["GBP"] = 0.85m, ["KWD"] = 0.33m });

    [Fact]
    public void Currency_TriangulatesAndRoundsToMinorUnits()
    {
        Assert.Equal(108.00m, Fx.Convert(100m, "EUR", "USD"));
        Assert.Equal(15046m, Fx.Convert(100m, "USD", "JPY"));   // via EUR, no decimals for yen
        Assert.Equal(30.556m, Fx.Convert(100m, "USD", "KWD"));  // three decimals for dinar
        Assert.Equal(104.76m, Fx.Convert(100m, "EUR", "USD", spreadPercent: 3));
        Assert.Throws<KeyNotFoundException>(() => Fx.Convert(1m, "EUR", "XYZ"));
    }

    [Fact]
    public void Currency_RoundTripsLoseMoney()
    {
        Assert.Equal(0m, Fx.RoundTripLoss(0.99m, "USD", "JPY")); // a yen is under half a cent, so rounding recovers
        Assert.True(Fx.RoundTripLoss(1000m, "EUR", "USD", spreadPercent: 2) > 39m);
        Assert.Equal("1,234,567 JPY", Fx.Format(1_234_567m, "JPY"));
        Assert.Equal("12.50 EUR", Fx.Format(12.5m, "EUR"));
    }
}
