using BenchmarkDotNet.Attributes;
using CSharpExercises.Algorithms;

namespace CSharpExercises.Benchmarks;

public enum InputKind
{
    Random,
    Sorted,
    Reversed
}

/// <summary>
/// Compares the hand-written sorts with the built-in ones across input sizes and orderings.
/// Every method returns a new sorted array, so the input stays the same between runs.
/// </summary>
[MemoryDiagnoser]
public class SortingBenchmarks
{
    private int[] _data = [];

    [Params(100, 1_000, 10_000)]
    public int Size { get; set; }

    [Params(InputKind.Random, InputKind.Sorted, InputKind.Reversed)]
    public InputKind Kind { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        _data = Kind switch
        {
            InputKind.Random => Enumerable.Range(0, Size).Select(_ => random.Next()).ToArray(),
            InputKind.Sorted => Enumerable.Range(0, Size).ToArray(),
            InputKind.Reversed => Enumerable.Range(0, Size).Reverse().ToArray(),
            _ => throw new ArgumentOutOfRangeException(nameof(Kind))
        };
    }

    [Benchmark(Baseline = true)]
    public int[] ArraySort()
    {
        var copy = (int[])_data.Clone();
        Array.Sort(copy);
        return copy;
    }

    [Benchmark]
    public int[] LinqOrder() => _data.Order().ToArray();

    [Benchmark]
    public int[] BubbleSort() => Sorting.BubbleSort(_data);

    [Benchmark]
    public int[] MergeSort() => Sorting.MergeSort(_data);

    [Benchmark]
    public int[] QuickSort() => Sorting.QuickSort(_data);
}
