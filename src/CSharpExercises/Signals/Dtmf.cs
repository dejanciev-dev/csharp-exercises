namespace CSharpExercises.Signals;

/// <summary>
/// Touch-tone (DTMF) dialling: each key is the sum of one low and one high frequency. The Goertzel algorithm measures
/// the power at a single frequency in O(n) with two state variables, much cheaper than a full FFT when only eight
/// frequencies matter, which is why phone systems use it to decode key presses.
/// </summary>
public static class Dtmf
{
    public static readonly double[] LowTones = [697, 770, 852, 941];
    public static readonly double[] HighTones = [1209, 1336, 1477, 1633];
    private static readonly string[] Keys = ["123A", "456B", "789C", "*0#D"];

    public static double[] Generate(char key, double sampleRate, double seconds)
    {
        var row = Array.FindIndex(Keys, r => r.Contains(key));
        if (row < 0) throw new ArgumentException($"'{key}' is not a DTMF key.", nameof(key));
        var col = Keys[row].IndexOf(key);
        return Enumerable.Range(0, (int)(sampleRate * seconds))
            .Select(i => 0.5 * Math.Sin(2 * Math.PI * LowTones[row] * i / sampleRate) + 0.5 * Math.Sin(2 * Math.PI * HighTones[col] * i / sampleRate))
            .ToArray();
    }

    /// <summary>Power of one frequency: a second-order recurrence s = x + 2cos(ω)·s₁ - s₂ over the samples.</summary>
    public static double Goertzel(ReadOnlySpan<double> samples, double frequency, double sampleRate)
    {
        var coefficient = 2 * Math.Cos(2 * Math.PI * frequency / sampleRate);
        double s1 = 0, s2 = 0;
        foreach (var x in samples)
        {
            var s = x + coefficient * s1 - s2;
            (s2, s1) = (s1, s);
        }
        return s1 * s1 + s2 * s2 - coefficient * s1 * s2;
    }

    /// <summary>The strongest low and high tone pick the key; null if either isn't clearly dominant (noise, speech).</summary>
    public static char? Detect(double[] samples, double sampleRate)
    {
        var low = LowTones.Select(f => Goertzel(samples, f, sampleRate)).ToArray();
        var high = HighTones.Select(f => Goertzel(samples, f, sampleRate)).ToArray();

        static int? Winner(double[] powers)
        {
            var order = powers.Select((p, i) => (p, i)).OrderByDescending(x => x.p).ToArray();
            return order[0].p > 4 * order[1].p ? order[0].i : null;
        }

        return Winner(low) is { } row && Winner(high) is { } col ? Keys[row][col] : null;
    }

    public static string Decode(IEnumerable<double[]> tones, double sampleRate) =>
        string.Concat(tones.Select(t => Detect(t, sampleRate)).OfType<char>());
}
