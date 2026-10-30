namespace CSharpExercises.Commerce;

/// <summary>A price tier: units up to and including <paramref name="UpTo"/> (null = no limit) cost <paramref name="UnitPrice"/>.</summary>
public sealed record Tier(int? UpTo, decimal UnitPrice, decimal FlatFee = 0);

/// <summary>
/// The two common ways usage-based billing (APIs, SaaS seats, utilities) applies tiers, which give different bills for
/// the same tiers and quantity. Volume: the tier the total falls in prices every unit. Graduated: each unit is priced by
/// the tier it falls in, like income tax brackets. Volume pricing has cliffs where buying more costs less.
/// </summary>
public sealed class TieredPricing
{
    private readonly IReadOnlyList<Tier> _tiers;

    public TieredPricing(IEnumerable<Tier> tiers)
    {
        _tiers = [.. tiers];
        if (_tiers.Count == 0 || _tiers[^1].UpTo is not null) throw new ArgumentException("The last tier must be unbounded.", nameof(tiers));
        for (var i = 1; i < _tiers.Count - 1; i++)
            if (_tiers[i].UpTo <= _tiers[i - 1].UpTo) throw new ArgumentException("Tier limits must increase.", nameof(tiers));
    }

    public decimal Volume(int quantity)
    {
        if (quantity <= 0) return 0;
        var tier = _tiers.First(t => t.UpTo is null || quantity <= t.UpTo);
        return quantity * tier.UnitPrice + tier.FlatFee;
    }

    public decimal Graduated(int quantity)
    {
        decimal total = 0;
        var previousLimit = 0;
        foreach (var tier in _tiers)
        {
            if (quantity <= previousLimit) break;
            var unitsInTier = Math.Min(quantity, tier.UpTo ?? int.MaxValue) - previousLimit;
            total += unitsInTier * tier.UnitPrice + tier.FlatFee;
            previousLimit = tier.UpTo ?? int.MaxValue;
        }
        return total;
    }

    /// <summary>Quantities where adding one unit lowers the volume price: worth knowing before publishing a price list.</summary>
    public IEnumerable<int> VolumeCliffs(int upTo) =>
        Enumerable.Range(1, upTo - 1).Where(q => Volume(q + 1) < Volume(q));
}
