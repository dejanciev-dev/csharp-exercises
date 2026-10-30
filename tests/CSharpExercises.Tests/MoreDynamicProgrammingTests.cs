using System.Numerics;
using CSharpExercises.Algorithms;

namespace CSharpExercises.Tests;

public class MoreDynamicProgrammingTests
{
    [Theory]
    [InlineData(1, 100, 100)]
    [InlineData(2, 100, 14)]
    [InlineData(3, 100, 9)]
    [InlineData(2, 36, 8)]
    [InlineData(10, 1000, 10)] // enough eggs: binary search
    public void EggDrop_MinimumDrops(int eggs, int floors, int expected) => Assert.Equal(expected, EggDrop.MinimumDrops(eggs, floors));

    [Fact]
    public void EggDrop_FastMatchesSlowAndPlans()
    {
        for (var e = 1; e <= 4; e++)
            for (var f = 1; f <= 60; f++)
                Assert.Equal(EggDrop.MinimumDropsSlow(e, f), EggDrop.MinimumDrops(e, f));

        Assert.Equal([14, 27, 39, 50, 60, 69, 77, 84, 90, 95, 99], EggDrop.TwoEggPlan(100));
    }

    [Theory]
    [InlineData("character", "carac")]
    [InlineData("bbbab", "bbbb")]
    [InlineData("abc", "a")]
    [InlineData("", "")]
    public void Palindrome_LongestSubsequence(string s, string expected)
    {
        var result = PalindromeDp.LongestSubsequence(s);
        Assert.Equal(expected.Length, result.Length);
        Assert.Equal(result, new string(result.Reverse().ToArray()));
    }

    [Fact]
    public void Palindrome_InsertionsAndCuts()
    {
        Assert.Equal(2, PalindromeDp.MinInsertionsToPalindrome("abc")); // "abc" → "abcba"

        var pieces = PalindromeDp.MinCutPartition("abbabb"); // e.g. "abba" + "bb": one cut
        Assert.Equal(2, pieces.Count);
        Assert.Equal("abbabb", string.Concat(pieces));
        Assert.All(pieces, p => Assert.Equal(p, new string(p.Reverse().ToArray())));
        Assert.Equal(["aa", "b"], PalindromeDp.MinCutPartition("aab"));
        Assert.Equal(["racecar"], PalindromeDp.MinCutPartition("racecar"));
    }

    [Theory]
    [InlineData("aa", "a", false)]
    [InlineData("aa", "a*", true)]
    [InlineData("ab", ".*", true)]
    [InlineData("aab", "c*a*b", true)]
    [InlineData("mississippi", "mis*is*p*.", false)]
    [InlineData("mississippi", "mis*is*ip*.", true)]
    [InlineData("", "a*b*", true)]
    public void Pattern_RegexMatch(string text, string pattern, bool expected) =>
        Assert.Equal(expected, PatternMatchingDp.RegexMatch(text, pattern));

    [Theory]
    [InlineData("Smith", "Sm_th", true)]
    [InlineData("Johnson", "J%son", true)]
    [InlineData("Jonson", "%hn%", false)]
    [InlineData("anything", "%", true)]
    [InlineData("", "%", true)]
    [InlineData("MiXeD", "mixed", true)]
    public void Pattern_SqlLike(string text, string pattern, bool expected) => Assert.Equal(expected, PatternMatchingDp.Like(text, pattern));

    private static readonly decimal[] Prices = [1, 5, 8, 9, 10, 17, 17, 20];

    [Fact]
    public void RodCutting_FindsTheBestCuts()
    {
        var (revenue, pieces) = RodCutting.Best(Prices, 8);

        Assert.Equal(22, revenue);
        Assert.Equal([6, 2], pieces);
        for (var n = 1; n <= 8; n++) Assert.Equal(RodCutting.BestNaive(Prices, n), RodCutting.Best(Prices, n).Revenue);
    }

    [Fact]
    public void RodCutting_CutCostsFavourFewerPieces()
    {
        var (revenue, pieces) = RodCutting.Best(Prices, 4, costPerCut: 1);

        Assert.Equal(9, revenue);  // 2 + 2 earns 10 - 1 = 9, the same as no cut
        Assert.Single(RodCutting.Best(Prices, 4, costPerCut: 2).Pieces);
        Assert.Equal(4, pieces.Sum());
    }

    [Fact]
    public void Grid_CountsPathsAroundObstacles()
    {
        Assert.Equal(6, GridPaths.CountPaths(["...", "...", "..."]));
        Assert.Equal(2, GridPaths.CountPaths(["...", ".#.", "..."]));
        Assert.Equal(0, GridPaths.CountPaths([".#", "#."]));
        Assert.Equal(BigInteger.Parse("137846528820"), GridPaths.CountPaths(Enumerable.Repeat(new string('.', 21), 21).ToList())); // Project Euler 15
    }

    [Fact]
    public void Grid_CheapestPath()
    {
        int[,] cost = { { 1, 3, 1 }, { 1, 5, 1 }, { 4, 2, 1 } };

        var (total, moves) = GridPaths.CheapestPath(cost);

        Assert.Equal(7, total);
        Assert.Equal("RRDD", moves);
    }

    [Fact]
    public void Stocks_StateMachinesMatchBruteForce()
    {
        var random = new Random(1);
        for (var trial = 0; trial < 200; trial++)
        {
            var prices = Enumerable.Range(0, random.Next(1, 9)).Select(_ => (decimal)random.Next(1, 10)).ToArray();
            Assert.Equal(StockTrading.BruteForce(prices), StockTrading.Unlimited(prices));
            Assert.Equal(StockTrading.BruteForce(prices, fee: 2), StockTrading.Unlimited(prices, fee: 2));
            Assert.Equal(StockTrading.BruteForce(prices, maxTrades: 2), StockTrading.AtMostKTrades(prices, 2));
            Assert.Equal(StockTrading.BruteForce(prices, cooldown: true), StockTrading.WithCooldown(prices));
        }
    }

    [Fact]
    public void Stocks_KnownExamples()
    {
        decimal[] prices = [3, 3, 5, 0, 0, 3, 1, 4];
        Assert.Equal(6, StockTrading.AtMostKTrades(prices, 2));
        Assert.Equal(8, StockTrading.Unlimited(prices));
        Assert.Equal(3, StockTrading.WithCooldown([1, 2, 3, 0, 2]));
    }
}
