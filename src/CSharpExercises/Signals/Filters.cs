namespace CSharpExercises.Signals;

/// <summary>
/// Simple digital filters. The one-pole low-pass y[n] = y[n-1] + α(x[n] - y[n-1]) smooths a signal using one number of
/// state (an RC circuit in software); subtracting it from the input gives a high-pass that removes slow drift. A
/// median filter removes impulsive spikes that would drag an average along with them.
/// </summary>
public static class Filters
{
    /// <summary>α from the cutoff: frequencies well below it pass, those well above are attenuated.</summary>
    public static double Alpha(double cutoffHz, double sampleRate)
    {
        var rc = 1 / (2 * Math.PI * cutoffHz);
        var dt = 1 / sampleRate;
        return dt / (rc + dt);
    }

    public static double[] LowPass(IReadOnlyList<double> input, double cutoffHz, double sampleRate)
    {
        var alpha = Alpha(cutoffHz, sampleRate);
        var output = new double[input.Count];
        var y = input.Count > 0 ? input[0] : 0;
        for (var i = 0; i < input.Count; i++) output[i] = y += alpha * (input[i] - y);
        return output;
    }

    public static double[] HighPass(IReadOnlyList<double> input, double cutoffHz, double sampleRate)
    {
        var low = LowPass(input, cutoffHz, sampleRate);
        return input.Select((x, i) => x - low[i]).ToArray();
    }

    /// <summary>Each output is the median of the surrounding window, so isolated outliers vanish entirely.</summary>
    public static double[] Median(IReadOnlyList<double> input, int window)
    {
        if (window < 1 || window % 2 == 0) throw new ArgumentException("The window must be a positive odd number.", nameof(window));
        var half = window / 2;
        return Enumerable.Range(0, input.Count).Select(i =>
        {
            var neighbourhood = Enumerable.Range(i - half, window).Select(j => input[Math.Clamp(j, 0, input.Count - 1)]).Order().ToArray();
            return neighbourhood[half];
        }).ToArray();
    }

    /// <summary>Root-mean-square: the "power" of a signal, used to compare how much a filter let through.</summary>
    public static double Rms(IEnumerable<double> signal) => Math.Sqrt(signal.Average(x => x * x));
}
