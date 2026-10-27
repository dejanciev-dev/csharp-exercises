using System.Buffers.Binary;

namespace CSharpExercises.IO;

[Flags]
public enum PacketFlags : byte { None = 0, Ack = 1, Compressed = 2, Final = 4 }

/// <summary>
/// A fixed 12-byte header for a made-up network protocol. Network protocols use big-endian ("network byte order"),
/// while x86 and ARM are little-endian, so every multi-byte field must be converted explicitly:
/// <see cref="BinaryPrimitives"/> does that, and reads and writes spans without allocating.
/// <code>
/// offset 0  version   (1 byte)
///        1  flags     (1 byte)
///        2  type      (uint16, big-endian)
///        4  sequence  (uint32, big-endian)
///        8  length    (uint32, big-endian) of the payload that follows
/// </code>
/// </summary>
public readonly record struct PacketHeader(byte Version, PacketFlags Flags, ushort Type, uint Sequence, uint PayloadLength)
{
    public const int Size = 12;
    public const uint MaxPayload = 16 * 1024 * 1024;

    public void WriteTo(Span<byte> destination)
    {
        if (destination.Length < Size) throw new ArgumentException($"Need {Size} bytes.", nameof(destination));

        destination[0] = Version;
        destination[1] = (byte)Flags;
        BinaryPrimitives.WriteUInt16BigEndian(destination[2..], Type);
        BinaryPrimitives.WriteUInt32BigEndian(destination[4..], Sequence);
        BinaryPrimitives.WriteUInt32BigEndian(destination[8..], PayloadLength);
    }

    /// <summary>
    /// Parses a header from bytes received off the network. Returns false (rather than throwing) for data that is
    /// incomplete or invalid, because both are normal there: a packet split across reads, or a misbehaving peer.
    /// </summary>
    public static bool TryRead(ReadOnlySpan<byte> source, out PacketHeader header)
    {
        header = default;
        if (source.Length < Size) return false;

        var flags = (PacketFlags)source[1];
        var length = BinaryPrimitives.ReadUInt32BigEndian(source[8..]);

        // Validate everything a peer controls: unknown flag bits, and a length that would make us allocate gigabytes.
        if (source[0] != 1 || (flags & ~(PacketFlags.Ack | PacketFlags.Compressed | PacketFlags.Final)) != 0 || length > MaxPayload)
            return false;

        header = new PacketHeader(source[0], flags, BinaryPrimitives.ReadUInt16BigEndian(source[2..]), BinaryPrimitives.ReadUInt32BigEndian(source[4..]), length);
        return true;
    }
}
