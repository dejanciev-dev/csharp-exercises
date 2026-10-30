namespace CSharpExercises.Statistics;

/// <summary>
/// Smoothing a noisy series. A simple moving average weighs the last n values equally and forgets older ones
/// abruptly; an exponential moving average keeps O(1) state and lets old values fade gradually. The time-based
/// version copes with irregular samples, which is how Unix load averages and many metrics libraries work.
/// </summary>
public static class MovingAverages
{
    public static IEnumerable<double> Simple(IEnumerable<double> values, int window)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(window, 1);
        var buffer = new Queue<double>(window);
        var sum = 0.0;
        foreach (var value in values)
        {
            buffer.Enqueue(value);
            sum += value;
            if (buffer.Count > window) sum -= buffer.Dequeue();
            if (buffer.Count == window) yield return sum / window;
        }
    }

    /// <param name="alpha">Weight of the newest value, between 0 and 1. alpha = 2 / (n + 1) roughly matches an n-value SMA.</param>
    public static IEnumerable<double> Exponential(IEnumerable<double> values, double alpha)
    {
        if (alpha is <= 0 or > 1) throw new ArgumentOutOfRangeException(nameof(alpha));
        double? average = null;
        foreach (var value in values)
        {
            average = average is { } a ? a + alpha * (value - a) : value;
            yield return average.Value;
        }
    }
}

/// <summary>An EMA over irregular timestamps: the decay depends on how long ago the previous sample was.</summary>
public sealed class DecayingAverage(TimeSpan halfLife)
{
    private DateTimeOffset? _last;

    public double Value { get; private set; }

    public void Add(double value, DateTimeOffset at)
    {
        if (_last is not { } last)
        {
            Value = value;
        }
        else
        {
            if (at < last) throw new ArgumentException("Samples must arrive in time order.", nameof(at));
            var weightOfOld = Math.Pow(0.5, (at - last) / halfLife);
            Value = weightOfOld * Value + (1 - weightOfOld) * value;
        }
        _last = at;
    }
}
