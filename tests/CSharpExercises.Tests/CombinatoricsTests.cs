using System.Numerics;
using CSharpExercises.Combinatorics;

namespace CSharpExercises.Tests;

public class CombinatoricsTests
{
    [Fact]
    public void Binomial_ExactPascalAndModular()
    {
        Assert.Equal(10, Binomial.Choose(5, 2));
        Assert.Equal(0, Binomial.Choose(5, 6));
        Assert.Equal(BigInteger.Parse("100891344545564193334812497256"), Binomial.Choose(100, 50));

        var pascal = Binomial.PascalTriangle(6);
        Assert.Equal([1L, 5, 10, 10, 5, 1], pascal[5]);

        Assert.Equal((long)(Binomial.Choose(1000, 300) % 13), Binomial.ChooseModPrime(1000, 300, 13));
        Assert.Equal(6, Binomial.LatticePaths(2, 2));
    }

    [Fact]
    public void Permutations_StepsInLexicographicOrder()
    {
        int[] p = [1, 2, 3];
        var seen = new List<string> { string.Concat(p) };
        while (Permutations.NextPermutation<int>(p)) seen.Add(string.Concat(p));

        Assert.Equal(["123", "132", "213", "231", "312", "321"], seen);
        Assert.Equal("123", string.Concat(p)); // wrapped back to the first
        Assert.Equal(3, Permutations.All("aab".ToCharArray()).Count()); // duplicates skipped
    }

    [Fact]
    public void Permutations_RankAndUnrank()
    {
        Assert.Equal(new[] { 2, 7, 8, 3, 9, 1, 5, 4, 6, 0 }, Permutations.Unrank(10, 999_999)); // Project Euler 24
        Assert.Equal(999_999, Permutations.Rank([2, 7, 8, 3, 9, 1, 5, 4, 6, 0]));
        for (var r = 0; r < 24; r++) Assert.Equal(r, Permutations.Rank(Permutations.Unrank(4, r)));
    }

    [Fact]
    public void Permutations_Derangements()
    {
        Assert.Equal([1, 0, 1, 2, 9, 44, 265], Enumerable.Range(0, 7).Select(n => (int)Permutations.Derangements(n)));
        // The share of derangements tends to 1/e.
        Assert.Equal(1 / Math.E, (double)(Permutations.Derangements(20) * 1_000_000 / Permutations.Factorial(20)) / 1_000_000, 6);
    }

    [Fact]
    public void Combinations_ChooseAndProduct()
    {
        Assert.Equal(["ab", "ac", "ad", "bc", "bd", "cd"], Combinations.Choose("abcd".ToCharArray(), 2).Select(c => new string(c)));
        Assert.Single(Combinations.Choose("abc".ToCharArray(), 0));
        Assert.Equal(252, Combinations.Choose(Enumerable.Range(0, 10).ToArray(), 5).Count());

        IReadOnlyList<IReadOnlyList<string>> lists = [["S", "M"], ["red", "blue", "green"]];
        Assert.Equal(["S red", "S blue", "S green", "M red", "M blue", "M green"], Combinations.CartesianProduct(lists).Select(x => string.Join(' ', x)));
    }

    [Fact]
    public void Combinations_ListsWaysToMakeChange()
    {
        var ways = Combinations.WaysToMakeChange(10, [1, 2, 5]).Select(w => string.Join('+', w)).ToArray();

        Assert.Equal(10, ways.Length);
        Assert.Contains("5+5", ways);
        Assert.Contains("2+2+2+2+2", ways);
    }

    [Fact]
    public void Partitions_CountAndList()
    {
        Assert.Equal(["4", "3+1", "2+2", "2+1+1", "1+1+1+1"], Partitions.All(4).Select(p => string.Join('+', p)));
        Assert.Equal(42, Partitions.Count(10));
        Assert.Equal(190_569_292, Partitions.Count(100));
    }

    [Fact]
    public void Partitions_StirlingAndBell()
    {
        Assert.Equal(25, Partitions.Stirling2(5, 3));
        Assert.Equal(0, Partitions.Stirling2(3, 0));
        Assert.Equal(1, Partitions.Stirling2(0, 0));
        Assert.Equal([1, 1, 2, 5, 15, 52, 203], Enumerable.Range(0, 7).Select(n => (int)Partitions.Bell(n)));
    }

    [Fact]
    public void Catalan_CountsBracketsAndTrees()
    {
        Assert.Equal([1, 1, 2, 5, 14, 42, 132], Enumerable.Range(0, 7).Select(n => (int)Catalan.Number(n)));
        Assert.Equal(["((()))", "(()())", "(())()", "()(())", "()()()"], Catalan.BalancedBrackets(3));
        Assert.Equal(14, Catalan.TreeShapes(4).Count);
        Assert.Equal(14, Catalan.TreeShapes(4).Distinct().Count());
    }

    [Fact]
    public void InclusionExclusion_Counts()
    {
        Assert.Equal(467, InclusionExclusion.CountDivisible(1000, [3, 5])); // Project Euler 1, counted instead of summed
        Assert.Equal(Enumerable.Range(1, 1000).Count(i => i % 4 == 0 || i % 6 == 0 || i % 9 == 0), InclusionExclusion.CountDivisible(1000, [4, 6, 9]));
        Assert.Equal(150, InclusionExclusion.Surjections(5, 3));
        Assert.Equal(Partitions.Stirling2(5, 3) * 6, InclusionExclusion.Surjections(5, 3)); // groups × labels
        Assert.Equal(40, InclusionExclusion.Totient(100));
        Assert.Equal(96, InclusionExclusion.Totient(97));
    }
}
