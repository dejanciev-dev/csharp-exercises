namespace CSharpExercises.Statistics;

/// <summary>
/// Mean and variance of a stream in one pass with Welford's algorithm. The textbook shortcut
/// E[x²] - E[x]² subtracts two huge, nearly equal numbers and loses all precision when values are large and close
/// together (timestamps, sensor readings around 1e9); Welford updates the mean and the squared deviations directly.
/// </summary>
public sealed class RunningStats
{
    private double _mean;
    private double _m2; // sum of squared differences from the current mean

    public long Count { get; private set; }
    public double Mean => Count > 0 ? _mean : double.NaN;
    public double Min { get; private set; } = double.PositiveInfinity;
    public double Max { get; private set; } = double.NegativeInfinity;

    /// <summary>Population variance: divide by n.</summary>
    public double Variance => Count > 0 ? _m2 / Count : double.NaN;

    /// <summary>Sample variance: divide by n - 1 (Bessel's correction) when the data is a sample of something bigger.</summary>
    public double SampleVariance => Count > 1 ? _m2 / (Count - 1) : double.NaN;

    public double StandardDeviation => Math.Sqrt(Variance);

    public void Add(double value)
    {
        Count++;
        var delta = value - _mean;
        _mean += delta / Count;
        _m2 += delta * (value - _mean);
        Min = Math.Min(Min, value);
        Max = Math.Max(Max, value);
    }

    /// <summary>
    /// Combines statistics computed separately, e.g. per thread or per server, without seeing the values again
    /// (Chan's parallel algorithm).
    /// </summary>
    public static RunningStats Merge(RunningStats a, RunningStats b)
    {
        if (a.Count == 0) return b.Copy();
        if (b.Count == 0) return a.Copy();

        var count = a.Count + b.Count;
        var delta = b._mean - a._mean;
        return new RunningStats
        {
            Count = count,
            _mean = a._mean + delta * b.Count / count,
            _m2 = a._m2 + b._m2 + delta * delta * a.Count * b.Count / count,
            Min = Math.Min(a.Min, b.Min),
            Max = Math.Max(a.Max, b.Max),
        };
    }

    /// <summary>The naive formula, to show the cancellation problem in a test.</summary>
    public static double NaiveVariance(IEnumerable<double> values)
    {
        double sum = 0, sumOfSquares = 0;
        long n = 0;
        foreach (var v in values)
        {
            n++;
            sum += v;
            sumOfSquares += v * v;
        }
        return sumOfSquares / n - (sum / n) * (sum / n);
    }

    private RunningStats Copy() => new() { Count = Count, _mean = _mean, _m2 = _m2, Min = Min, Max = Max };
}
