using CSharpExercises.Algorithms;

namespace CSharpExercises.Tests;

public class DynamicProgrammingTests
{
    [Theory]
    [InlineData(new[] { 1, 2, 5 }, 11, 3)]
    [InlineData(new[] { 1, 3, 4 }, 6, 2)] // greedy (4 + 1 + 1) would give 3
    [InlineData(new[] { 2 }, 3, -1)]
    [InlineData(new[] { 7 }, 0, 0)]
    public void MinCoins(int[] coins, int amount, int expected) =>
        Assert.Equal(expected, DynamicProgramming.MinCoins(coins, amount));

    [Theory]
    [InlineData(new[] { 1, 2, 5 }, 5, 4)]
    [InlineData(new[] { 2 }, 3, 0)]
    [InlineData(new[] { 1, 2, 5, 10, 20, 50, 100, 200 }, 200, 73682)]
    public void CountWays(int[] coins, int amount, long expected) =>
        Assert.Equal(expected, DynamicProgramming.CountWays(coins, amount));

    [Fact]
    public void Coins_RejectInvalidInput()
    {
        Assert.Throws<ArgumentException>(() => DynamicProgramming.MinCoins([0, 1], 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => DynamicProgramming.CountWays([1], -1));
    }

    [Theory]
    [InlineData("AGGTAB", "GXTXAYB", "GTAB")]
    [InlineData("abc", "abc", "abc")]
    [InlineData("abc", "def", "")]
    [InlineData("", "abc", "")]
    public void LongestCommonSubsequence(string a, string b, string expected) =>
        Assert.Equal(expected, DynamicProgramming.LongestCommonSubsequence(a, b));

    [Fact]
    public void LongestCommonSubsequence_IsInBothStrings_WhenSeveralExist()
    {
        const string a = "ABCBDAB", b = "BDCABA";

        var lcs = DynamicProgramming.LongestCommonSubsequence(a, b);

        Assert.Equal(4, lcs.Length);
        Assert.True(IsSubsequence(lcs, a));
        Assert.True(IsSubsequence(lcs, b));
    }

    [Theory]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("flaw", "lawn", 2)]
    [InlineData("", "abc", 3)]
    [InlineData("same", "same", 0)]
    public void EditDistance(string source, string target, int expected) =>
        Assert.Equal(expected, DynamicProgramming.EditDistance(source, target));

    private static bool IsSubsequence(string candidate, string text)
    {
        var i = 0;
        foreach (var c in text)
        {
            if (i < candidate.Length && candidate[i] == c) i++;
        }

        return i == candidate.Length;
    }
}
