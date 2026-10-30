namespace CSharpExercises.Algorithms;

/// <summary>
/// Stock trading problems as a small state machine evaluated day by day: after each day you either hold a share or
/// don't, and each state keeps the best profit reachable. Variants change the transitions: unlimited trades, at most k
/// trades, a cooldown day after selling, or a fee per trade. All run in O(days) (times k).
/// </summary>
public static class StockTrading
{
    public static decimal Unlimited(IReadOnlyList<decimal> prices, decimal fee = 0)
    {
        if (prices.Count == 0) return 0;
        decimal cash = 0, hold = -prices[0];
        foreach (var p in prices.Skip(1))
            (cash, hold) = (Math.Max(cash, hold + p - fee), Math.Max(hold, cash - p));
        return cash;
    }

    public static decimal AtMostKTrades(IReadOnlyList<decimal> prices, int k)
    {
        if (prices.Count == 0 || k == 0) return 0;
        var cash = new decimal[k + 1];               // cash[t]: best with t completed trades, holding nothing
        var hold = Enumerable.Repeat(decimal.MinValue / 2, k + 1).ToArray(); // hold[t]: holding, in trade t
        foreach (var p in prices)
            for (var t = k; t >= 1; t--)
            {
                cash[t] = Math.Max(cash[t], hold[t] + p);
                hold[t] = Math.Max(hold[t], cash[t - 1] - p);
            }
        return cash[k];
    }

    /// <summary>After selling you must wait a day before buying again.</summary>
    public static decimal WithCooldown(IReadOnlyList<decimal> prices)
    {
        if (prices.Count == 0) return 0;
        decimal hold = -prices[0], sold = decimal.MinValue / 2, rest = 0;
        foreach (var p in prices.Skip(1))
            (hold, sold, rest) = (Math.Max(hold, rest - p), hold + p, Math.Max(rest, sold));
        return Math.Max(sold, rest);
    }

    /// <summary>Brute force over every buy/sell/wait sequence for small inputs, to check the state machines.</summary>
    public static decimal BruteForce(IReadOnlyList<decimal> prices, int maxTrades = int.MaxValue, bool cooldown = false, decimal fee = 0)
    {
        decimal Go(int day, bool holding, int trades, bool justSold)
        {
            if (day == prices.Count) return 0;
            var wait = Go(day + 1, holding, trades, false);
            if (holding) return Math.Max(wait, prices[day] - fee + Go(day + 1, false, trades, true));
            if (trades >= maxTrades || (cooldown && justSold)) return wait;
            return Math.Max(wait, -prices[day] + Go(day + 1, true, trades + 1, false));
        }
        return Go(0, false, 0, false);
    }
}
