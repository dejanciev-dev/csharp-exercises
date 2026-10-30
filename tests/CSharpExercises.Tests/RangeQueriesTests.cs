using CSharpExercises.RangeQueries;

namespace CSharpExercises.Tests;

public class RangeQueriesTests
{
    private static readonly int[] Values = [5, -2, 7, 1, 0, 3, -4, 8];

    [Fact]
    public void PrefixSums_AnswerRangesInConstantTime()
    {
        var sums = new PrefixSums(Values);

        Assert.Equal(18, sums.Sum(0, 8));
        Assert.Equal(6, sums.Sum(1, 4));
        Assert.Equal(0, sums.Sum(3, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => sums.Sum(0, 9));
    }

    [Fact]
    public void PrefixSums_CountSubarraysWithNegatives()
    {
        Assert.Equal(2, PrefixSums.CountSubarraysWithSum([1, 1, 1], 2));
        Assert.Equal(3, PrefixSums.CountSubarraysWithSum([1, -1, 0], 0)); // [1,-1], [1,-1,0] and [0]
    }

    [Fact]
    public void PrefixSums2D_SumsRectangles()
    {
        int[,] grid = { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 } };
        var sums = new PrefixSums2D(grid);

        Assert.Equal(45, sums.Sum(0, 0, 3, 3));
        Assert.Equal(28, sums.Sum(1, 1, 3, 3));
        Assert.Equal(5, sums.Sum(1, 1, 2, 2));
    }

    [Fact]
    public void Fenwick_SupportsUpdatesAndQueries()
    {
        var tree = new FenwickTree(Values.Select(v => (long)v).ToArray());

        Assert.Equal(18, tree.Sum(0, 8));
        Assert.Equal(6, tree.Sum(1, 4));

        tree.Add(2, 10);
        Assert.Equal(16, tree.Sum(1, 4));
        Assert.Equal(28, tree.PrefixSum(8));
    }

    [Fact]
    public void Fenwick_CountsInversions()
    {
        Assert.Equal(0, FenwickTree.CountInversions([1, 2, 3]));
        Assert.Equal(3, FenwickTree.CountInversions([3, 2, 1]));
        Assert.Equal(3, FenwickTree.CountInversions([2, 4, 1, 3, 5]));
    }

    [Fact]
    public void SegmentTree_MatchesBruteForceAfterUpdates()
    {
        var random = new Random(7);
        var data = Enumerable.Range(0, 50).Select(_ => random.Next(-100, 100)).ToArray();
        var min = SegmentTree.ForMin(data);
        var sum = SegmentTree.ForSum(data.Select(v => (long)v).ToArray());

        for (var step = 0; step < 500; step++)
        {
            var index = random.Next(data.Length);
            data[index] = random.Next(-100, 100);
            min.Set(index, data[index]);
            sum.Set(index, data[index]);

            var from = random.Next(data.Length);
            var to = random.Next(from + 1, data.Length + 1);
            Assert.Equal(data[from..to].Min(), min.Query(from, to));
            Assert.Equal(data[from..to].Sum(), sum.Query(from, to));
        }
    }

    [Fact]
    public void SegmentTree_KeepsOrderForNonCommutativeOperations()
    {
        var words = new SegmentTree<string>(["a", "b", "c", "d", "e"], string.Concat, "");

        Assert.Equal("bcd", words.Query(1, 4));
        Assert.Equal("abcde", words.Query(0, 5));
    }

    [Fact]
    public void SparseTable_RangeMinimum()
    {
        var table = new SparseTable(Values);

        for (var from = 0; from < Values.Length; from++)
            for (var to = from + 1; to <= Values.Length; to++)
                Assert.Equal(Values[from..to].Min(), table.Min(from, to));
    }

    [Fact]
    public void DifferenceArray_AppliesRangeUpdates()
    {
        var diff = new DifferenceArray(6);
        diff.AddRange(0, 3, 5);
        diff.AddRange(2, 6, 1);
        diff.AddRange(4, 5, -2);

        Assert.Equal([5L, 5, 6, 1, -1, 1], diff.Build());
    }

    [Fact]
    public void DifferenceArray_PeakOverlap()
    {
        Assert.Equal(2, DifferenceArray.PeakOverlap([(0, 30), (5, 10), (15, 20)]));
        Assert.Equal(1, DifferenceArray.PeakOverlap([(1, 5), (5, 9)])); // checkout and check-in at 5 don't clash
        Assert.Equal(0, DifferenceArray.PeakOverlap([]));
    }

    [Fact]
    public void SlidingWindow_Maximums()
    {
        Assert.Equal([3, 3, 5, 5, 6, 7], SlidingWindow.Maximums([1, 3, -1, -3, 5, 3, 6, 7], 3));
        Assert.Empty(SlidingWindow.Maximums([1, 2], 3));
    }

    [Theory]
    [InlineData("abcabcbb", "abc")]
    [InlineData("bbbbb", "b")]
    [InlineData("pwwkew", "wke")]
    [InlineData("", "")]
    public void SlidingWindow_LongestWithoutRepeats(string text, string expected) =>
        Assert.Equal(expected, SlidingWindow.LongestWithoutRepeats(text));

    [Fact]
    public void SlidingWindow_ShortestWithSum()
    {
        Assert.Equal(2, SlidingWindow.ShortestWithSumAtLeast([2, 3, 1, 2, 4, 3], 7));
        Assert.Equal(0, SlidingWindow.ShortestWithSumAtLeast([1, 1], 5));
    }
}
