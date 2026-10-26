using CSharpExercises.Algorithms;

namespace CSharpExercises.Tests;

public class AlgorithmsTests
{
    public static TheoryData<int[]> SortInputs => new()
    {
        Array.Empty<int>(),
        new[] { 1 },
        new[] { 5, 2, 9, 1, 5, 6 },
        new[] { 3, 2, 1 },
        new[] { -4, 10, 0, -4, 7, 3, 3 }
    };

    [Theory]
    [MemberData(nameof(SortInputs))]
    public void AllSorts_MatchBuiltInSort(int[] input)
    {
        var expected = input.Order().ToArray();
        var original = (int[])input.Clone();

        Assert.Equal(expected, Sorting.BubbleSort(input));
        Assert.Equal(expected, Sorting.MergeSort(input));
        Assert.Equal(expected, Sorting.QuickSort(input));
        Assert.Equal(original, input); // input is left untouched
    }

    public static TheoryData<string, int[]> LargeInputs => new()
    {
        { "sorted", Enumerable.Range(0, 100_000).ToArray() },
        { "reversed", Enumerable.Range(0, 100_000).Reverse().ToArray() },
        { "all equal", Enumerable.Repeat(7, 100_000).ToArray() },
        { "random", Enumerable.Range(0, 100_000).Select(i => (i * 7919) % 100_003).ToArray() }
    };

    [Theory]
    [MemberData(nameof(LargeInputs))]
    public void QuickSort_HandlesLargeWorstCaseInputs(string kind, int[] input)
    {
        Assert.NotEmpty(kind);
        Assert.Equal(input.Order(), Sorting.QuickSort(input));
    }

    [Fact]
    public void Sorts_WorkWithStrings() =>
        Assert.Equal(["apple", "banana", "cherry"], Sorting.MergeSort(["cherry", "apple", "banana"]));

    [Theory]
    [InlineData(7, 3)]
    [InlineData(1, 0)]
    [InlineData(13, 6)]
    [InlineData(4, -1)]
    public void BinarySearch(int target, int expectedIndex) =>
        Assert.Equal(expectedIndex, Searching.BinarySearch([1, 3, 5, 7, 9, 11, 13], target));

    [Fact]
    public void TwoSum_FindsPair() =>
        Assert.Equal((0, 1), Searching.TwoSum([2, 7, 11, 15], 9));

    [Fact]
    public void TwoSum_ReturnsNullWhenNoPair() =>
        Assert.Null(Searching.TwoSum([1, 2, 3], 100));
}
