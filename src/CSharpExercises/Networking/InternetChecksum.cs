using System.Buffers.Binary;

namespace CSharpExercises.Networking;

/// <summary>
/// The 16-bit ones'-complement checksum used by IPv4, TCP, UDP and ICMP headers (RFC 1071). Add the data as 16-bit
/// big-endian words, fold any carries back into the low 16 bits, and invert. A receiver summing the data together with
/// the checksum gets 0xFFFF; a router that changes one field can update the checksum incrementally (RFC 1624)
/// instead of recomputing it.
/// </summary>
public static class InternetChecksum
{
    public static ushort Compute(ReadOnlySpan<byte> data)
    {
        uint sum = 0;
        while (data.Length >= 2)
        {
            sum += BinaryPrimitives.ReadUInt16BigEndian(data);
            data = data[2..];
        }
        if (data.Length == 1) sum += (uint)data[0] << 8; // pad an odd length with a zero byte

        while (sum >> 16 != 0) sum = (sum & 0xFFFF) + (sum >> 16); // end-around carry
        return (ushort)~sum;
    }

    /// <summary>Valid when the checksum over the whole header, checksum field included, is zero.</summary>
    public static bool Verify(ReadOnlySpan<byte> dataIncludingChecksum) => Compute(dataIncludingChecksum) == 0;

    /// <summary>HC' = ~(~HC + ~m + m'): the new checksum after one 16-bit field changes from m to m'.</summary>
    public static ushort Update(ushort checksum, ushort oldValue, ushort newValue)
    {
        uint sum = (ushort)~checksum + (uint)(ushort)~oldValue + newValue;
        while (sum >> 16 != 0) sum = (sum & 0xFFFF) + (sum >> 16);
        return (ushort)~sum;
    }

    /// <summary>Builds a minimal 20-byte IPv4 header with a correct checksum, as a worked example.</summary>
    public static byte[] Ipv4Header(string source, string destination, byte ttl, byte protocol, ushort totalLength)
    {
        var header = new byte[20];
        header[0] = 0x45; // version 4, header length 5 × 4 bytes
        BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(2), totalLength);
        header[8] = ttl;
        header[9] = protocol;
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(12), Ipv4Network.ToUInt32(source));
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(16), Ipv4Network.ToUInt32(destination));
        BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(10), Compute(header));
        return header;
    }
}
