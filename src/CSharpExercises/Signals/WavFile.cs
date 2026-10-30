using System.Buffers.Binary;
using System.Text;

namespace CSharpExercises.Signals;

/// <summary>
/// The WAV format: a RIFF container with a "fmt " chunk (channels, sample rate, bits per sample) and a "data" chunk of
/// little-endian PCM samples. Writing one by hand is a good exercise in binary layouts; reading one means walking the
/// chunks and skipping the ones you don't know (many files carry LIST metadata chunks).
/// </summary>
public static class WavFile
{
    /// <summary>Mono, 16-bit PCM. Samples are clipped to [-1, 1] before scaling to short.</summary>
    public static byte[] Write(IReadOnlyList<double> samples, int sampleRate)
    {
        const short Channels = 1, BitsPerSample = 16;
        var dataSize = samples.Count * 2;
        var bytes = new byte[44 + dataSize];
        var span = bytes.AsSpan();

        Encoding.ASCII.GetBytes("RIFF").CopyTo(span);
        BinaryPrimitives.WriteInt32LittleEndian(span[4..], 36 + dataSize);
        Encoding.ASCII.GetBytes("WAVEfmt ").CopyTo(span[8..]);
        BinaryPrimitives.WriteInt32LittleEndian(span[16..], 16);                 // fmt chunk size
        BinaryPrimitives.WriteInt16LittleEndian(span[20..], 1);                  // PCM
        BinaryPrimitives.WriteInt16LittleEndian(span[22..], Channels);
        BinaryPrimitives.WriteInt32LittleEndian(span[24..], sampleRate);
        BinaryPrimitives.WriteInt32LittleEndian(span[28..], sampleRate * Channels * BitsPerSample / 8); // byte rate
        BinaryPrimitives.WriteInt16LittleEndian(span[32..], Channels * BitsPerSample / 8);              // block align
        BinaryPrimitives.WriteInt16LittleEndian(span[34..], BitsPerSample);
        Encoding.ASCII.GetBytes("data").CopyTo(span[36..]);
        BinaryPrimitives.WriteInt32LittleEndian(span[40..], dataSize);

        for (var i = 0; i < samples.Count; i++)
            BinaryPrimitives.WriteInt16LittleEndian(span[(44 + 2 * i)..], (short)Math.Round(Math.Clamp(samples[i], -1, 1) * short.MaxValue));
        return bytes;
    }

    public static (double[] Samples, int SampleRate) Read(ReadOnlySpan<byte> file)
    {
        if (file.Length < 12 || !file[..4].SequenceEqual("RIFF"u8) || !file[8..12].SequenceEqual("WAVE"u8))
            throw new InvalidDataException("Not a RIFF/WAVE file.");

        int? sampleRate = null, bits = null, channels = null;
        var position = 12;
        while (position + 8 <= file.Length)
        {
            var id = file.Slice(position, 4);
            var size = BinaryPrimitives.ReadInt32LittleEndian(file[(position + 4)..]);
            var body = file.Slice(position + 8, Math.Min(size, file.Length - position - 8));

            if (id.SequenceEqual("fmt "u8))
            {
                if (BinaryPrimitives.ReadInt16LittleEndian(body) != 1) throw new InvalidDataException("Only PCM is supported.");
                channels = BinaryPrimitives.ReadInt16LittleEndian(body[2..]);
                sampleRate = BinaryPrimitives.ReadInt32LittleEndian(body[4..]);
                bits = BinaryPrimitives.ReadInt16LittleEndian(body[14..]);
            }
            else if (id.SequenceEqual("data"u8))
            {
                if (sampleRate is null || bits != 16 || channels != 1) throw new InvalidDataException("Expected a mono 16-bit fmt chunk before data.");
                var samples = new double[body.Length / 2];
                for (var i = 0; i < samples.Length; i++) samples[i] = BinaryPrimitives.ReadInt16LittleEndian(body[(2 * i)..]) / (double)short.MaxValue;
                return (samples, sampleRate.Value);
            }
            position += 8 + size + (size & 1); // chunks are padded to an even length
        }
        throw new InvalidDataException("No data chunk.");
    }

    public static double[] Sine(double frequency, int sampleRate, double seconds, double amplitude = 0.5) =>
        Enumerable.Range(0, (int)(sampleRate * seconds)).Select(i => amplitude * Math.Sin(2 * Math.PI * frequency * i / sampleRate)).ToArray();
}
