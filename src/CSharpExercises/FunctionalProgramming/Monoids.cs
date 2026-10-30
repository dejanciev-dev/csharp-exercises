namespace CSharpExercises.FunctionalProgramming;

/// <summary>
/// A monoid is a type with an associative combine operation and an identity value: (int, +, 0), (string, concat, ""),
/// (max, -∞). That small contract is all a fold needs, and associativity is what makes it safe to split the work
/// across threads or machines and combine partial results in any grouping (but the same order).
/// </summary>
public interface IMonoid<T>
{
    static abstract T Identity { get; }
    static abstract T Combine(T a, T b);
}

public readonly record struct Sum(long Value) : IMonoid<Sum>
{
    public static Sum Identity => new(0);
    public static Sum Combine(Sum a, Sum b) => new(a.Value + b.Value);
}

public readonly record struct Max(int Value) : IMonoid<Max>
{
    public static Max Identity => new(int.MinValue);
    public static Max Combine(Max a, Max b) => new(Math.Max(a.Value, b.Value));
}

/// <summary>Several statistics in one pass, because a tuple of monoids is a monoid.</summary>
public readonly record struct Summary(long Count, double Total, double Min, double Max) : IMonoid<Summary>
{
    public static Summary Of(double value) => new(1, value, value, value);
    public static Summary Identity => new(0, 0, double.PositiveInfinity, double.NegativeInfinity);
    public static Summary Combine(Summary a, Summary b) =>
        new(a.Count + b.Count, a.Total + b.Total, Math.Min(a.Min, b.Min), Math.Max(a.Max, b.Max));
    public double Mean => Count == 0 ? double.NaN : Total / Count;
}

/// <summary>
/// Not obviously a monoid: the best subarray sum (Kadane) as a combinable summary. Each chunk records its total, best
/// prefix, best suffix and best inside; two chunks combine in O(1), so the problem parallelises.
/// </summary>
public readonly record struct BestRun(long Total, long Prefix, long Suffix, long Best) : IMonoid<BestRun>
{
    public static BestRun Of(long value) => new(value, value, value, value);

    // An empty run that is neutral: everything starts at "minus infinity" except the total.
    public static BestRun Identity => new(0, long.MinValue / 4, long.MinValue / 4, long.MinValue / 4);

    public static BestRun Combine(BestRun a, BestRun b) => new(
        a.Total + b.Total,
        Math.Max(a.Prefix, a.Total + b.Prefix),
        Math.Max(b.Suffix, b.Total + a.Suffix),
        Math.Max(Math.Max(a.Best, b.Best), a.Suffix + b.Prefix));
}

public static class Monoid
{
    public static T Concat<T>(IEnumerable<T> values) where T : IMonoid<T> =>
        values.Aggregate(T.Identity, T.Combine);

    /// <summary>Folds each chunk in parallel, then combines the chunk results in their original order.</summary>
    public static T ParallelConcat<T>(IReadOnlyList<T> values, int chunks) where T : IMonoid<T>
    {
        var size = (values.Count + chunks - 1) / Math.Max(chunks, 1);
        var partial = new T[chunks];
        Parallel.For(0, chunks, i =>
        {
            var acc = T.Identity;
            for (var j = i * size; j < Math.Min(values.Count, (i + 1) * size); j++) acc = T.Combine(acc, values[j]);
            partial[i] = acc;
        });
        return Concat(partial);
    }
}
