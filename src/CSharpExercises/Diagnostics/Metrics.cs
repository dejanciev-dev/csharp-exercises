using System.Globalization;
using System.Text;

namespace CSharpExercises.Diagnostics;

/// <summary>A monotonically increasing count: requests served, errors seen.</summary>
public sealed class CounterMetric(string name, string help)
{
    private long _value;
    public string Name { get; } = name;
    public string Help { get; } = help;
    public long Value => Interlocked.Read(ref _value);
    public void Increment(long by = 1)
    {
        if (by < 0) throw new ArgumentOutOfRangeException(nameof(by), "Counters only go up; use a gauge.");
        Interlocked.Add(ref _value, by);
    }
}

/// <summary>A value that goes up and down: queue length, memory in use.</summary>
public sealed class GaugeMetric(string name, string help)
{
    private long _bits;
    public string Name { get; } = name;
    public string Help { get; } = help;
    public double Value => BitConverter.Int64BitsToDouble(Interlocked.Read(ref _bits));
    public void Set(double value) => Interlocked.Exchange(ref _bits, BitConverter.DoubleToInt64Bits(value));
}

/// <summary>
/// A histogram with fixed bucket boundaries, as Prometheus uses: each observation increments one bucket counter, so
/// recording is O(log buckets) with constant memory, and histograms from many servers can be added together. Percentiles
/// are estimated by linear interpolation inside the bucket that contains the rank, so bucket choice decides accuracy.
/// </summary>
public sealed class HistogramMetric(string name, string help, IReadOnlyList<double> upperBounds)
{
    private readonly double[] _bounds = [.. upperBounds.Order()];
    private readonly long[] _counts = new long[upperBounds.Count + 1]; // the last bucket is +Inf
    private double _sum;
    private readonly Lock _lock = new();

    public string Name { get; } = name;
    public string Help { get; } = help;
    public long Count { get { lock (_lock) return _counts.Sum(); } }
    public double Sum { get { lock (_lock) return _sum; } }

    public void Observe(double value)
    {
        var index = Array.BinarySearch(_bounds, value);
        if (index < 0) index = ~index; // first bound ≥ value
        lock (_lock)
        {
            _counts[index]++;
            _sum += value;
        }
    }

    public double Percentile(double p)
    {
        lock (_lock)
        {
            var total = _counts.Sum();
            if (total == 0) return double.NaN;
            var rank = p / 100 * total;
            long cumulative = 0;
            for (var i = 0; i < _counts.Length; i++)
            {
                if (cumulative + _counts[i] >= rank && _counts[i] > 0)
                {
                    if (i == _bounds.Length) return _bounds[^1]; // in +Inf: the best we can say is "above the last bound"
                    var lower = i == 0 ? 0 : _bounds[i - 1];
                    return lower + (_bounds[i] - lower) * (rank - cumulative) / _counts[i];
                }
                cumulative += _counts[i];
            }
            return _bounds[^1];
        }
    }

    /// <summary>The Prometheus text format: cumulative bucket counts with le="…" labels.</summary>
    public string Expose()
    {
        var sb = new StringBuilder($"# HELP {Name} {Help}\n# TYPE {Name} histogram\n");
        lock (_lock)
        {
            long cumulative = 0;
            for (var i = 0; i < _counts.Length; i++)
            {
                cumulative += _counts[i];
                var le = i < _bounds.Length ? _bounds[i].ToString(CultureInfo.InvariantCulture) : "+Inf";
                sb.Append($"{Name}_bucket{{le=\"{le}\"}} {cumulative}\n");
            }
            sb.Append($"{Name}_sum {_sum.ToString(CultureInfo.InvariantCulture)}\n{Name}_count {cumulative}\n");
        }
        return sb.ToString();
    }
}
