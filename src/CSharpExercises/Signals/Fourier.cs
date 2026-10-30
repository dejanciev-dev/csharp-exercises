using System.Numerics;

namespace CSharpExercises.Signals;

/// <summary>
/// The discrete Fourier transform rewrites a signal as a sum of sine waves, revealing which frequencies it contains.
/// Computed directly it costs O(n²); the Cooley–Tukey FFT splits it into the transforms of the even and odd samples
/// and combines them with "twiddle factors", O(n log n) for power-of-two lengths. 1 million samples: seconds → milliseconds.
/// </summary>
public static class Fourier
{
    public static Complex[] Dft(IReadOnlyList<Complex> input)
    {
        var n = input.Count;
        var output = new Complex[n];
        for (var k = 0; k < n; k++)
            for (var t = 0; t < n; t++)
                output[k] += input[t] * Complex.FromPolarCoordinates(1, -2 * Math.PI * k * t / n);
        return output;
    }

    public static Complex[] Fft(IReadOnlyList<Complex> input)
    {
        var n = input.Count;
        if (n == 1) return [input[0]];
        if (!BitOperations.IsPow2(n)) throw new ArgumentException("The length must be a power of two.", nameof(input));

        var even = Fft(Enumerable.Range(0, n / 2).Select(i => input[2 * i]).ToArray());
        var odd = Fft(Enumerable.Range(0, n / 2).Select(i => input[2 * i + 1]).ToArray());

        var output = new Complex[n];
        for (var k = 0; k < n / 2; k++)
        {
            var twiddle = Complex.FromPolarCoordinates(1, -2 * Math.PI * k / n) * odd[k];
            output[k] = even[k] + twiddle;
            output[k + n / 2] = even[k] - twiddle; // the second half reuses the same products
        }
        return output;
    }

    /// <summary>The inverse transform: conjugate, transform, conjugate, divide by n.</summary>
    public static Complex[] InverseFft(IReadOnlyList<Complex> spectrum) =>
        Fft(spectrum.Select(Complex.Conjugate).ToArray()).Select(c => Complex.Conjugate(c) / spectrum.Count).ToArray();

    /// <summary>The strongest frequencies in a real signal, in Hz, ignoring the mirrored upper half of the spectrum.</summary>
    public static IReadOnlyList<(double Frequency, double Magnitude)> DominantFrequencies(double[] samples, double sampleRate, int count)
    {
        var spectrum = Fft(samples.Select(s => new Complex(s, 0)).ToArray());
        return Enumerable.Range(1, spectrum.Length / 2 - 1)
            .Select(k => (k * sampleRate / spectrum.Length, spectrum[k].Magnitude * 2 / spectrum.Length))
            .OrderByDescending(p => p.Item2)
            .Take(count)
            .ToList();
    }

    /// <summary>Polynomial (or big-number) multiplication by convolution: transform, multiply pointwise, transform back.</summary>
    public static long[] MultiplyPolynomials(IReadOnlyList<long> a, IReadOnlyList<long> b)
    {
        var size = (int)BitOperations.RoundUpToPowerOf2((uint)(a.Count + b.Count - 1));
        Complex[] Pad(IReadOnlyList<long> p) => [.. p.Select(x => new Complex(x, 0)), .. Enumerable.Repeat(Complex.Zero, size - p.Count)];

        var fa = Fft(Pad(a));
        var fb = Fft(Pad(b));
        var product = InverseFft(fa.Zip(fb, (x, y) => x * y).ToArray());
        return product.Take(a.Count + b.Count - 1).Select(c => (long)Math.Round(c.Real)).ToArray();
    }
}
