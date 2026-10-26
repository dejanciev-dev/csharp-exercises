using BenchmarkDotNet.Attributes;
using CSharpExercises.ModernCSharp;

namespace CSharpExercises.Benchmarks;

/// <summary>
/// Measures what <see cref="SpanExercises.SumCsv"/> saves over the usual string.Split approach,
/// which allocates an array plus one string per field.
/// </summary>
[MemoryDiagnoser]
public class CsvParsingBenchmarks
{
    private string _line = "";

    [Params(10, 1_000)]
    public int Fields { get; set; }

    [GlobalSetup]
    public void Setup() =>
        _line = string.Join(", ", Enumerable.Range(0, Fields).Select(i => i * 37 % 1000));

    [Benchmark(Baseline = true)]
    public int StringSplit()
    {
        var total = 0;
        foreach (var field in _line.Split(','))
        {
            var trimmed = field.Trim();
            if (trimmed.Length > 0) total += int.Parse(trimmed);
        }

        return total;
    }

    [Benchmark]
    public int SpanSplit() => SpanExercises.SumCsv(_line);
}
