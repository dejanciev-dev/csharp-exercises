using CSharpExercises.Algorithms;

namespace CSharpExercises.Tests;

public class MoreAlgorithmsTests
{
    [Fact]
    public void Knapsack_BeatsGreedy()
    {
        KnapsackItem[] items = [new("laptop", 10, 60), new("camera", 20, 100), new("tripod", 30, 120)];

        var (value, chosen) = Knapsack.Solve(items, 50);

        Assert.Equal(220, value);
        Assert.Equal(["camera", "tripod"], chosen.Select(i => i.Name));
        Assert.Equal(160, Knapsack.GreedyByRatio(items, 50)); // grabs the laptop's ratio and wastes 20 kg
        Assert.Equal(220, Knapsack.MaxValue(items, 50));
    }

    [Fact]
    public void Knapsack_SubsetSum()
    {
        Assert.True(Knapsack.SubsetSum([3, 34, 4, 12, 5, 2], 9));
        Assert.False(Knapsack.SubsetSum([3, 34, 4, 12, 5, 2], 30));
    }

    [Fact]
    public void Lis_FindsAnActualSubsequence()
    {
        int[] values = [10, 9, 2, 5, 3, 7, 101, 18];

        var lis = LongestIncreasingSubsequence.Find(values);

        Assert.Equal([2, 3, 7, 18], lis);
        Assert.Equal(4, LongestIncreasingSubsequence.LengthQuadratic(values));
        Assert.Empty(LongestIncreasingSubsequence.Find([]));
    }

    [Fact]
    public void Lis_AgreesWithQuadraticVersion()
    {
        var random = new Random(5);
        for (var i = 0; i < 100; i++)
        {
            var values = Enumerable.Range(0, random.Next(0, 40)).Select(_ => random.Next(20)).ToArray();
            var lis = LongestIncreasingSubsequence.Find(values);

            Assert.Equal(LongestIncreasingSubsequence.LengthQuadratic(values), lis.Count);
            Assert.True(lis.Zip(lis.Skip(1)).All(p => p.First < p.Second));
        }
    }

    [Fact]
    public void Lis_NestsEnvelopes()
    {
        Assert.Equal(3, LongestIncreasingSubsequence.MaxNestedEnvelopes([(5, 4), (6, 4), (6, 7), (2, 3)]));
        Assert.Equal(1, LongestIncreasingSubsequence.MaxNestedEnvelopes([(1, 1), (1, 2), (1, 3)]));
    }

    [Fact]
    public void Kadane_FindsTheBestRun()
    {
        Assert.Equal((6L, 3, 4), MaximumSubarray.Find([-2, 1, -3, 4, -1, 2, 1, -5, 4]));
        Assert.Equal((-1L, 2, 1), MaximumSubarray.Find([-3, -2, -1, -4]));
    }

    [Fact]
    public void Kadane_CircularAndTrading()
    {
        Assert.Equal(10, MaximumSubarray.CircularMax([5, -3, 5])); // 5 + 5 around the end
        Assert.Equal(-2, MaximumSubarray.CircularMax([-3, -2, -3]));
        Assert.Equal(5m, MaximumSubarray.BestTrade([7, 1, 5, 3, 6, 4]));
        Assert.Equal(0m, MaximumSubarray.BestTrade([7, 6, 4, 3, 1]));
    }

    [Fact]
    public void WordBreak_SegmentsText()
    {
        var breaker = new WordBreak(["cat", "cats", "and", "sand", "dog", "i", "love", "new", "york", "newyork"]);

        Assert.True(breaker.CanSegment("catsanddog"));
        Assert.False(breaker.CanSegment("catsandog"));
        Assert.Equal(["cat sand dog", "cats and dog"], breaker.AllSegmentations("catsanddog").Order());
        Assert.Equal("i love newyork", breaker.FewestWords("ilovenewyork"));
        Assert.Null(breaker.FewestWords("ilovebrooklyn"));
    }

    [Fact]
    public void MatrixChain_FindsTheCheapestOrder()
    {
        // A: 10×30, B: 30×5, C: 5×60
        var (cost, order) = MatrixChain.Optimal([10, 30, 5, 60]);

        Assert.Equal(4500, cost);
        Assert.Equal("((AB)C)", order);
    }

    [Fact]
    public void MatrixChain_OrderCanHalveTheWork()
    {
        int[] dimensions = [40, 20, 30, 10, 30];

        Assert.Equal((26000L, "((A(BC))D)"), MatrixChain.Optimal(dimensions));
        Assert.Equal(48000, MatrixChain.LeftToRight(dimensions));
    }

    [Fact]
    public void Intervals_GreedyByEndTime()
    {
        Meeting[] meetings = [new("a", 1, 4), new("b", 3, 5), new("c", 0, 6), new("d", 5, 7), new("e", 8, 9), new("f", 5, 9)];

        Assert.Equal(["a", "d", "e"], IntervalScheduling.MaxNonOverlapping(meetings).Select(m => m.Name));
    }

    [Fact]
    public void Intervals_AssignRooms()
    {
        Meeting[] meetings = [new("standup", 9, 10), new("design", 9, 11), new("lunch", 10, 11), new("review", 11, 12)];

        var rooms = IntervalScheduling.AssignRooms(meetings);

        Assert.Equal(2, rooms.Values.Distinct().Count());
        Assert.NotEqual(rooms["standup"], rooms["design"]);
        Assert.Equal(rooms["standup"], rooms["lunch"]); // standup ends exactly when lunch starts
    }

    [Fact]
    public void Intervals_WeightedNeedsDynamicProgramming()
    {
        Meeting[] meetings = [new("short1", 0, 2, 3), new("long", 0, 10, 10), new("short2", 2, 4, 3), new("short3", 4, 6, 3)];

        var (weight, chosen) = IntervalScheduling.MaxWeight(meetings);

        Assert.Equal(10, weight);
        Assert.Equal(["long"], chosen.Select(m => m.Name));
        Assert.Equal(3, IntervalScheduling.MaxNonOverlapping(meetings).Count); // most meetings ≠ most value
    }
}
