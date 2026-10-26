namespace CSharpExercises.Algorithms;

/// <summary>
/// Dynamic programming: solve each small subproblem once, store the answer in a table, and build bigger answers from it.
/// </summary>
public static class DynamicProgramming
{
    /// <summary>
    /// Fewest coins that add up to <paramref name="amount"/>, or -1 if it can't be made.
    /// best[a] = 1 + min(best[a - coin]). Unlike a greedy approach, this is correct for coins like {1, 3, 4} and amount 6.
    /// </summary>
    public static int MinCoins(int[] coins, int amount)
    {
        ValidateCoins(coins, amount);

        var best = new int[amount + 1];
        Array.Fill(best, int.MaxValue);
        best[0] = 0;

        for (var a = 1; a <= amount; a++)
        {
            foreach (var coin in coins)
            {
                if (coin <= a && best[a - coin] != int.MaxValue)
                {
                    best[a] = Math.Min(best[a], best[a - coin] + 1);
                }
            }
        }

        return best[amount] == int.MaxValue ? -1 : best[amount];
    }

    /// <summary>
    /// Number of different coin combinations that add up to <paramref name="amount"/>.
    /// Looping over coins on the outside counts 1+2 and 2+1 as the same combination.
    /// Throws <see cref="OverflowException"/> when the count doesn't fit in a long, instead of silently wrapping around.
    /// </summary>
    public static long CountWays(int[] coins, int amount)
    {
        ValidateCoins(coins, amount);

        var ways = new long[amount + 1];
        ways[0] = 1;

        foreach (var coin in coins)
        {
            for (var a = coin; a <= amount; a++)
            {
                ways[a] = checked(ways[a] + ways[a - coin]);
            }
        }

        return ways[amount];
    }

    /// <summary>
    /// Longest sequence of characters that appears in both strings in the same order, not necessarily next to each other.
    /// </summary>
    public static string LongestCommonSubsequence(string a, string b)
    {
        // lengths[i, j] = LCS length of the first i chars of a and the first j chars of b.
        var lengths = new int[a.Length + 1, b.Length + 1];
        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                lengths[i, j] = a[i - 1] == b[j - 1]
                    ? lengths[i - 1, j - 1] + 1
                    : Math.Max(lengths[i - 1, j], lengths[i, j - 1]);
            }
        }

        // Walk back from the bottom-right corner to rebuild one subsequence.
        var result = new char[lengths[a.Length, b.Length]];
        int x = a.Length, y = b.Length, k = result.Length;
        while (x > 0 && y > 0)
        {
            if (a[x - 1] == b[y - 1])
            {
                result[--k] = a[x - 1];
                x--;
                y--;
            }
            else if (lengths[x - 1, y] >= lengths[x, y - 1])
            {
                x--;
            }
            else
            {
                y--;
            }
        }

        return new string(result);
    }

    /// <summary>
    /// Levenshtein distance: the fewest single-character inserts, deletes or substitutions that turn
    /// <paramref name="source"/> into <paramref name="target"/>. Keeps only two rows of the table.
    /// </summary>
    public static int EditDistance(string source, string target)
    {
        var previous = Enumerable.Range(0, target.Length + 1).ToArray();
        var current = new int[target.Length + 1];

        for (var i = 1; i <= source.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= target.Length; j++)
            {
                var substitute = previous[j - 1] + (source[i - 1] == target[j - 1] ? 0 : 1);
                current[j] = Math.Min(substitute, Math.Min(previous[j] + 1, current[j - 1] + 1));
            }

            (previous, current) = (current, previous);
        }

        return previous[target.Length];
    }

    private static void ValidateCoins(int[] coins, int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (coins.Any(c => c <= 0)) throw new ArgumentException("Coin values must be positive.", nameof(coins));
    }
}
