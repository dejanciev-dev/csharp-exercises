namespace CSharpExercises.Algorithms;

/// <summary>
/// Rod cutting, the textbook introduction to dynamic programming: given prices for pieces of each length, how should a
/// rod be cut to earn the most? The naive recursion tries 2^(n-1) ways; remembering the best value for each shorter
/// length makes it O(n²). A cutting cost per cut shows how the optimum shifts toward fewer, larger pieces.
/// </summary>
public static class RodCutting
{
    /// <param name="prices">prices[i] is the price of a piece of length i + 1.</param>
    public static (decimal Revenue, IReadOnlyList<int> Pieces) Best(IReadOnlyList<decimal> prices, int length, decimal costPerCut = 0)
    {
        var best = new decimal[length + 1];
        var firstPiece = new int[length + 1];

        for (var n = 1; n <= length; n++)
        {
            best[n] = decimal.MinValue;
            for (var piece = 1; piece <= Math.Min(n, prices.Count); piece++)
            {
                var value = prices[piece - 1] + best[n - piece] - (piece < n ? costPerCut : 0);
                if (value > best[n]) (best[n], firstPiece[n]) = (value, piece);
            }
        }

        var pieces = new List<int>();
        for (var n = length; n > 0; n -= firstPiece[n]) pieces.Add(firstPiece[n]);
        return (best[length], pieces.OrderDescending().ToList());
    }

    /// <summary>The exponential recursion, to compare against.</summary>
    public static decimal BestNaive(IReadOnlyList<decimal> prices, int length)
    {
        if (length == 0) return 0;
        var best = decimal.MinValue;
        for (var piece = 1; piece <= Math.Min(length, prices.Count); piece++)
            best = Math.Max(best, prices[piece - 1] + BestNaive(prices, length - piece));
        return best;
    }
}
