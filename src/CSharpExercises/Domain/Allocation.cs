namespace CSharpExercises.Domain;

/// <summary>
/// Splitting money without losing or inventing cents. 100.00 / 3 rounded is 33.33 three times, which adds up to 99.99:
/// a cent has vanished. The largest-remainder method rounds every share down, then hands the leftover cents one by one
/// to the shares that lost the most in rounding, so the parts always add up to the whole.
/// </summary>
public static class Allocation
{
    /// <summary>Equal shares, e.g. splitting a bill: 100.00 in 3 is [33.34, 33.33, 33.33].</summary>
    public static decimal[] Split(decimal amount, int parts, int decimals = 2)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(parts);
        return ByRatios(amount, Enumerable.Repeat(1m, parts), decimals);
    }

    /// <summary>
    /// Shares in proportion to <paramref name="ratios"/>, e.g. a discount spread over order lines by their prices.
    /// Ties for a leftover unit go to the earlier share, so the result is deterministic.
    /// </summary>
    public static decimal[] ByRatios(decimal amount, IEnumerable<decimal> ratios, int decimals = 2)
    {
        var weights = ratios.ToArray();
        if (weights.Length == 0) throw new ArgumentException("At least one ratio is needed.", nameof(ratios));
        if (weights.Any(w => w < 0)) throw new ArgumentException("Ratios cannot be negative.", nameof(ratios));

        var totalWeight = weights.Sum();
        if (totalWeight == 0) throw new ArgumentException("Ratios cannot all be zero.", nameof(ratios));

        // Work in whole units of the smallest denomination (cents for 2 decimals), so everything is an integer.
        var unit = 1m / (decimal)Math.Pow(10, decimals);
        var totalUnits = amount / unit;
        if (totalUnits != decimal.Truncate(totalUnits))
            throw new ArgumentException($"{amount} has more than {decimals} decimal places.", nameof(amount));

        // Allocate the magnitude and put the sign back at the end, so negative amounts (refunds) round the same way.
        var sign = Math.Sign(amount);
        var units = Math.Abs(totalUnits);

        var exact = weights.Select(w => units * w / totalWeight).ToArray();
        var shares = exact.Select(decimal.Floor).ToArray();
        var leftover = (int)(units - shares.Sum());

        var byRemainder = Enumerable.Range(0, weights.Length)
            .OrderByDescending(i => exact[i] - shares[i])
            .ThenBy(i => i)
            .Take(leftover);
        foreach (var i in byRemainder) shares[i]++;

        return [.. shares.Select(s => sign * s * unit)];
    }
}
