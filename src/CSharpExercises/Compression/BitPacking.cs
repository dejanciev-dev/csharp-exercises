using System.Numerics;

namespace CSharpExercises.Compression;

/// <summary>
/// Bit packing stores small integers in exactly as many bits as the largest one needs: 1,000 values below 64 take 6 bits
/// each (750 bytes) instead of 4,000 bytes as ints. Column stores and search indexes pack blocks this way; "frame of
/// reference" subtracts the block minimum first so that large but close values (timestamps, ids) pack small too.
/// </summary>
public static class BitPacking
{
    public static int BitsNeeded(uint max) => max == 0 ? 1 : 32 - BitOperations.LeadingZeroCount(max);

    public static byte[] Pack(IReadOnlyList<uint> values, int bitsPerValue)
    {
        if (bitsPerValue is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(bitsPerValue));
        var output = new byte[(values.Count * bitsPerValue + 7) / 8];
        long bitPosition = 0;
        foreach (var value in values)
        {
            if (bitsPerValue < 32 && value >> bitsPerValue != 0) throw new ArgumentException($"{value} doesn't fit in {bitsPerValue} bits.", nameof(values));
            for (var bit = 0; bit < bitsPerValue; bit++, bitPosition++)
                if ((value >> bit & 1) == 1) output[bitPosition >> 3] |= (byte)(1 << (int)(bitPosition & 7));
        }
        return output;
    }

    public static uint[] Unpack(ReadOnlySpan<byte> packed, int count, int bitsPerValue)
    {
        if ((long)count * bitsPerValue > packed.Length * 8L) throw new ArgumentException("Not enough data for that many values.");
        var values = new uint[count];
        long bitPosition = 0;
        for (var i = 0; i < count; i++)
            for (var bit = 0; bit < bitsPerValue; bit++, bitPosition++)
                if ((packed[(int)(bitPosition >> 3)] >> (int)(bitPosition & 7) & 1) == 1) values[i] |= 1u << bit;
        return values;
    }

    /// <summary>Frame of reference: store the minimum once, then pack the (small) offsets from it.</summary>
    public static (long Reference, int Bits, byte[] Packed) PackWithReference(IReadOnlyList<long> values)
    {
        var reference = values.Count == 0 ? 0 : values.Min();
        var offsets = values.Select(v => checked((uint)(v - reference))).ToArray();
        var bits = BitsNeeded(offsets.DefaultIfEmpty(0u).Max());
        return (reference, bits, Pack(offsets, bits));
    }

    public static long[] UnpackWithReference(long reference, int bits, ReadOnlySpan<byte> packed, int count) =>
        Unpack(packed, count, bits).Select(o => reference + o).ToArray();
}
