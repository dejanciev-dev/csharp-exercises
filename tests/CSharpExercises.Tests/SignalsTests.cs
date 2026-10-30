using System.Numerics;
using CSharpExercises.Signals;

namespace CSharpExercises.Tests;

public class SignalsTests
{
    [Fact]
    public void Fft_MatchesTheDirectTransformAndInverts()
    {
        var random = new Random(1);
        var signal = Enumerable.Range(0, 64).Select(_ => new Complex(random.NextDouble() - 0.5, 0)).ToArray();

        var fast = Fourier.Fft(signal);
        var slow = Fourier.Dft(signal);
        var back = Fourier.InverseFft(fast);

        for (var i = 0; i < 64; i++)
        {
            Assert.Equal(slow[i].Real, fast[i].Real, 9);
            Assert.Equal(slow[i].Imaginary, fast[i].Imaginary, 9);
            Assert.Equal(signal[i].Real, back[i].Real, 9);
        }
        Assert.Throws<ArgumentException>(() => Fourier.Fft(new Complex[6]));
    }

    [Fact]
    public void Fft_FindsTheFrequenciesInAChord()
    {
        const int Rate = 8192;
        var samples = Enumerable.Range(0, 8192).Select(i => Math.Sin(2 * Math.PI * 440 * i / Rate) + 0.5 * Math.Sin(2 * Math.PI * 1000 * i / Rate)).ToArray();

        var top = Fourier.DominantFrequencies(samples, Rate, 2);

        Assert.Equal([440.0, 1000.0], top.Select(t => t.Frequency));
        Assert.Equal(1, top[0].Magnitude, 6);
        Assert.Equal(0.5, top[1].Magnitude, 6);
    }

    [Fact]
    public void Fft_MultipliesPolynomials()
    {
        // (1 + 2x + 3x²)(4 + 5x) = 4 + 13x + 22x² + 15x³
        Assert.Equal([4L, 13, 22, 15], Fourier.MultiplyPolynomials([1, 2, 3], [4, 5]));

        // 123 × 456 = 56088 by treating the digits (least significant first) as coefficients.
        var digits = Fourier.MultiplyPolynomials([3, 2, 1], [6, 5, 4]);
        Assert.Equal(56088, digits.Select((d, i) => d * (long)Math.Pow(10, i)).Sum());
    }

    [Fact]
    public void Dtmf_DecodesADialledNumber()
    {
        const double Rate = 8000;
        var tones = "0123456789*#".Select(k => Dtmf.Generate(k, Rate, 0.05));

        Assert.Equal("0123456789*#", Dtmf.Decode(tones, Rate));
    }

    [Fact]
    public void Dtmf_IgnoresNoiseAndSilence()
    {
        var random = new Random(2);
        var noise = Enumerable.Range(0, 400).Select(_ => random.NextDouble() - 0.5).ToArray();

        Assert.Null(Dtmf.Detect(noise, 8000));
        Assert.Null(Dtmf.Detect(new double[400], 8000));

        var tone = WavFile.Sine(697, 8000, 0.05);
        Assert.True(Dtmf.Goertzel(tone, 697, 8000) > 100 * Dtmf.Goertzel(tone, 1209, 8000));
    }

    [Fact]
    public void Wav_RoundTripsAndHasTheRightHeader()
    {
        var samples = WavFile.Sine(440, 8000, 0.01);

        var file = WavFile.Write(samples, 8000);
        var (read, rate) = WavFile.Read(file);

        Assert.Equal(44 + samples.Length * 2, file.Length);
        Assert.Equal("RIFF", System.Text.Encoding.ASCII.GetString(file, 0, 4));
        Assert.Equal(8000, rate);
        Assert.Equal(samples.Length, read.Length);
        Assert.All(samples.Zip(read), p => Assert.InRange(Math.Abs(p.First - p.Second), 0, 1.0 / short.MaxValue)); // one quantisation step
    }

    [Fact]
    public void Wav_SkipsUnknownChunksAndRejectsNonsense()
    {
        var file = WavFile.Write([0.25, -0.25], 44100);
        // Insert a 3-byte LIST chunk (padded to 4) between fmt and data.
        byte[] list = [(byte)'L', (byte)'I', (byte)'S', (byte)'T', 3, 0, 0, 0, 1, 2, 3, 0];
        byte[] withList = [.. file[..36], .. list, .. file[36..]];

        Assert.Equal(2, WavFile.Read(withList).Samples.Length);
        Assert.Throws<InvalidDataException>(() => WavFile.Read("not a wav file at all"u8));
    }

    [Fact]
    public void Filters_LowPassRemovesHighFrequencies()
    {
        const double Rate = 8000;
        var slow = WavFile.Sine(50, (int)Rate, 0.5);
        var fast = WavFile.Sine(3000, (int)Rate, 0.5);

        Assert.True(Filters.Rms(Filters.LowPass(slow, 200, Rate).Skip(400)) > 0.9 * Filters.Rms(slow));
        Assert.True(Filters.Rms(Filters.LowPass(fast, 200, Rate).Skip(400)) < 0.1 * Filters.Rms(fast));
        Assert.True(Filters.Rms(Filters.HighPass(slow, 2000, Rate).Skip(400)) < 0.1 * Filters.Rms(slow));
    }

    [Fact]
    public void Filters_MedianRemovesSpikes()
    {
        double[] readings = [10, 10, 11, 250, 10, 9, 10, -100, 10];

        Assert.Equal([10.0, 10, 11, 11, 10, 10, 9, 10, 10], Filters.Median(readings, 3));
        Assert.Throws<ArgumentException>(() => Filters.Median(readings, 4));
    }

    [Theory]
    [InlineData(440, "A4", 0)]
    [InlineData(261.63, "C4", 0)]
    [InlineData(445, "A4", 20)]
    [InlineData(82.41, "E2", 0)]
    public void Pitch_DetectsNotes(double frequency, string note, int cents)
    {
        const int Rate = 16000;
        // A fundamental plus a louder second harmonic: the biggest FFT bin would say one octave too high.
        var samples = Enumerable.Range(0, 4096)
            .Select(i => 0.4 * Math.Sin(2 * Math.PI * frequency * i / Rate) + 0.6 * Math.Sin(2 * Math.PI * 2 * frequency * i / Rate))
            .ToArray();

        var detected = PitchDetection.DetectFrequency(samples, Rate)!.Value;

        Assert.InRange(detected, frequency * 0.995, frequency * 1.005);
        Assert.Equal((note, cents), PitchDetection.NearestNote(frequency));
    }

    [Fact]
    public void Pitch_SilenceHasNoPitch() => Assert.Null(PitchDetection.DetectFrequency(new double[2048], 16000));
}
