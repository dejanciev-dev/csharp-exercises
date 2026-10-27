namespace CSharpExercises.IO;

/// <summary>
/// CRC-32 (the one used by zip, PNG and Ethernet), table-driven. A checksum catches accidental corruption such as a
/// truncated download or a flipped bit; it is not a security measure, since anyone can recompute it.
/// </summary>
public static class Crc32
{
    private static readonly uint[] Table = BuildTable();

    public static uint Compute(ReadOnlySpan<byte> data) => Append(0, data);

    /// <summary>Continues a checksum over more data, so a file can be checked in chunks without loading it whole.</summary>
    public static uint Append(uint crc, ReadOnlySpan<byte> data)
    {
        crc = ~crc;
        foreach (var b in data) crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return ~crc;
    }

    // One entry per byte value: the effect of shifting that byte through the polynomial bit by bit, precomputed.
    private static uint[] BuildTable()
    {
        const uint polynomial = 0xEDB88320; // 0x04C11DB7 with its bits reversed
        var table = new uint[256];
        for (var i = 0u; i < 256; i++)
        {
            var value = i;
            for (var bit = 0; bit < 8; bit++) value = (value & 1) != 0 ? (value >> 1) ^ polynomial : value >> 1;
            table[i] = value;
        }
        return table;
    }
}
