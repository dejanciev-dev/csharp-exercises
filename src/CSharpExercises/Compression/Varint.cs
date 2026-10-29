using System.Numerics;

namespace CSharpExercises.Compression;

/// <summary>
/// Variable-length integers (LEB128, as in Protocol Buffers and WebAssembly): 7 bits per byte, with the high bit saying
/// "more bytes follow". Small numbers take one byte instead of eight, which pays off when most values are small.
/// </summary>
public static class Varint
{
    /// <summary>A 64-bit value needs at most ceil(64 / 7) = 10 bytes.</summary>
    public const int MaxBytes = 10;

    /// <summary>Writes <paramref name="value"/> least significant group first and returns the number of bytes written.</summary>
    public static int Write(Span<byte> destination, ulong value)
    {
        var i = 0;
        while (value >= 0x80)
        {
            destination[i++] = (byte)(value | 0x80);
            value >>= 7;
        }
        destination[i++] = (byte)value;
        return i;
    }

    public static int SizeOf(ulong value) => value == 0 ? 1 : (64 - BitOperations.LeadingZeroCount(value) + 6) / 7;

    /// <summary>
    /// Reads one varint. Fails instead of throwing on truncated input, and on input that would overflow 64 bits
    /// (more than 10 bytes, or a 10th byte carrying more than the one bit that is left), since the bytes are untrusted.
    /// </summary>
    public static bool TryRead(ReadOnlySpan<byte> source, out ulong value, out int bytesRead)
    {
        value = 0;
        for (var i = 0; i < source.Length && i < MaxBytes; i++)
        {
            var b = source[i];
            if (i == MaxBytes - 1 && b > 1) break;

            value |= (ulong)(b & 0x7F) << (7 * i);
            if (b < 0x80)
            {
                bytesRead = i + 1;
                return true;
            }
        }

        value = 0;
        bytesRead = 0;
        return false;
    }

    /// <summary>
    /// Maps signed to unsigned so that small magnitudes stay small: 0, -1, 1, -2, 2 become 0, 1, 2, 3, 4.
    /// Without it, -1 as a ulong is 0xFFFF…FF and takes all 10 bytes.
    /// </summary>
    public static ulong ZigZagEncode(long value) => (ulong)((value << 1) ^ (value >> 63));

    public static long ZigZagDecode(ulong value) => (long)(value >> 1) ^ -(long)(value & 1);

    /// <summary>
    /// Stores a sequence as the differences between neighbours, e.g. sorted timestamps or ids, where the values are
    /// large but the gaps are small. Overflowing differences wrap around, and wrap back the same way when decoding.
    /// </summary>
    public static byte[] EncodeDeltas(IReadOnlyList<long> values)
    {
        Span<byte> buffer = stackalloc byte[MaxBytes];
        var output = new List<byte>(values.Count + MaxBytes);

        output.AddRange(buffer[..Write(buffer, (ulong)values.Count)]);
        var previous = 0L;
        foreach (var value in values)
        {
            output.AddRange(buffer[..Write(buffer, ZigZagEncode(unchecked(value - previous)))]);
            previous = value;
        }
        return [.. output];
    }

    public static long[] DecodeDeltas(ReadOnlySpan<byte> data)
    {
        if (!TryRead(data, out var count, out var read)) throw new InvalidDataException("Missing or malformed count.");
        data = data[read..];

        // Every value takes at least one byte, so a count larger than the remaining bytes is a lie: don't allocate for it.
        if (count > (ulong)data.Length) throw new InvalidDataException($"Count {count} exceeds the {data.Length} bytes that follow.");

        var values = new long[count];
        var previous = 0L;
        for (var i = 0; i < values.Length; i++)
        {
            if (!TryRead(data, out var delta, out read)) throw new InvalidDataException($"Value {i} is truncated or malformed.");
            data = data[read..];
            previous = unchecked(previous + ZigZagDecode(delta));
            values[i] = previous;
        }

        if (!data.IsEmpty) throw new InvalidDataException($"{data.Length} unexpected bytes after the last value.");
        return values;
    }
}
