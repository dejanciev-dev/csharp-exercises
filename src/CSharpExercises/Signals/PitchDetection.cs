namespace CSharpExercises.Signals;

/// <summary>
/// Finding the pitch of a note with autocorrelation: compare the signal with itself shifted by a lag; at a lag of one
/// period it lines up again and the correlation peaks. Unlike picking the biggest FFT bin, this finds the fundamental
/// even when an overtone is louder. Then the pitch is named on the equal-tempered scale (A4 = 440 Hz).
/// </summary>
public static class PitchDetection
{
    private static readonly string[] NoteNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];

    public static double? DetectFrequency(double[] samples, double sampleRate, double minHz = 50, double maxHz = 2000)
    {
        var minLag = (int)(sampleRate / maxHz);
        var maxLag = Math.Min((int)(sampleRate / minHz), samples.Length / 2);
        var energy = samples.Sum(s => s * s);
        if (energy < 1e-9) return null; // silence

        var correlations = new double[maxLag + 2];
        for (var lag = minLag; lag <= maxLag + 1 && lag < samples.Length; lag++)
        {
            var sum = 0.0;
            for (var i = 0; i + lag < samples.Length; i++) sum += samples[i] * samples[i + lag];
            correlations[lag] = sum / (samples.Length - lag); // normalise for the shrinking overlap
        }

        // The first local maximum that is nearly as strong as the best one is the fundamental period.
        var best = correlations[minLag..(maxLag + 1)].Max();
        for (var lag = minLag + 1; lag <= maxLag; lag++)
        {
            if (correlations[lag] < 0.9 * best || correlations[lag] < correlations[lag - 1] || correlations[lag] < correlations[lag + 1]) continue;

            // Parabolic interpolation around the peak gives sub-sample precision.
            var (a, b, c) = (correlations[lag - 1], correlations[lag], correlations[lag + 1]);
            var shift = (a - c) / (2 * (a - 2 * b + c));
            return sampleRate / (lag + (double.IsFinite(shift) ? shift : 0));
        }
        return null;
    }

    /// <summary>Nearest note and how many cents (hundredths of a semitone) sharp or flat: a tuner display.</summary>
    public static (string Note, int Cents) NearestNote(double frequency)
    {
        var semitonesFromA4 = 12 * Math.Log2(frequency / 440);
        var nearest = (int)Math.Round(semitonesFromA4);
        var midi = 69 + nearest;
        var name = $"{NoteNames[(midi % 12 + 12) % 12]}{midi / 12 - 1}";
        return (name, (int)Math.Round((semitonesFromA4 - nearest) * 100));
    }
}
