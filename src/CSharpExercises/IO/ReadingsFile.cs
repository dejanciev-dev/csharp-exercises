using System.Buffers.Binary;
using System.Text;

namespace CSharpExercises.IO;

public sealed record SensorReading(int SensorId, DateTimeOffset At, double Value, string Unit);

/// <summary>
/// A small binary file format, with the parts every real one needs:
/// <code>
/// "RDGS"            magic number: rejects files that are not ours before reading anything else
/// version  (byte)   lets the format change while old files stay readable
/// count    (int32)
/// readings          id int32, time int64 (Unix ms), value float64, unit (length-prefixed UTF-8)
/// crc32    (uint32) checksum of everything before it: detects truncation and corruption
/// </code>
/// <see cref="BinaryWriter"/> and <see cref="BinaryReader"/> always use little-endian, whatever the machine,
/// so a file written on one platform reads correctly on any other.
/// </summary>
public static class ReadingsFile
{
    private static readonly byte[] Magic = "RDGS"u8.ToArray();
    public const byte CurrentVersion = 2;

    public static void Write(Stream destination, IReadOnlyCollection<SensorReading> readings)
    {
        // Write into a buffer first so the checksum can be computed over exactly the bytes that were written.
        using var buffer = new MemoryStream();
        using (var writer = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(Magic);
            writer.Write(CurrentVersion);
            writer.Write(readings.Count);
            foreach (var reading in readings)
            {
                writer.Write(reading.SensorId);
                writer.Write(reading.At.ToUnixTimeMilliseconds());
                writer.Write(reading.Value);
                writer.Write(reading.Unit); // 7-bit encoded length, then the UTF-8 bytes
            }
        }

        var content = buffer.GetBuffer().AsSpan(0, (int)buffer.Length);
        destination.Write(content);
        Span<byte> checksum = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(checksum, Crc32.Compute(content));
        destination.Write(checksum);
    }

    /// <summary>
    /// Reads a file written by any version so far. Version 1 had no unit, so those readings get "?".
    /// Every problem is reported as <see cref="InvalidDataException"/> with a reason, never as a garbage result.
    /// </summary>
    public static List<SensorReading> Read(Stream source)
    {
        using var buffer = new MemoryStream();
        source.CopyTo(buffer);
        var bytes = buffer.GetBuffer().AsSpan(0, (int)buffer.Length);

        if (bytes.Length < Magic.Length + 1 + 4 + 4 || !bytes[..Magic.Length].SequenceEqual(Magic))
            throw new InvalidDataException("Not a readings file.");

        var content = bytes[..^4];
        if (Crc32.Compute(content) != BinaryPrimitives.ReadUInt32LittleEndian(bytes[^4..]))
            throw new InvalidDataException("Checksum mismatch: the file is corrupted or truncated.");

        using var reader = new BinaryReader(new MemoryStream(content.ToArray()), Encoding.UTF8);
        reader.ReadBytes(Magic.Length);

        var version = reader.ReadByte();
        if (version is 0 or > CurrentVersion)
            throw new InvalidDataException($"Unsupported version {version}; this reader understands 1 to {CurrentVersion}.");

        var count = reader.ReadInt32();
        if (count < 0) throw new InvalidDataException($"Invalid count {count}.");

        // Do not trust the count for pre-allocation: a corrupt file claiming 2 billion items must not allocate 2 billion slots.
        var readings = new List<SensorReading>(Math.Min(count, 1024));
        try
        {
            for (var i = 0; i < count; i++)
            {
                var id = reader.ReadInt32();
                var at = DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadInt64());
                var value = reader.ReadDouble();
                var unit = version >= 2 ? reader.ReadString() : "?";
                readings.Add(new SensorReading(id, at, value, unit));
            }
        }
        catch (EndOfStreamException)
        {
            throw new InvalidDataException($"The file says it has {count} readings but ends after {readings.Count}.");
        }

        if (reader.BaseStream.Position != reader.BaseStream.Length)
            throw new InvalidDataException("Unexpected data after the last reading.");

        return readings;
    }

    /// <summary>Writes the old version 1 layout (no unit), to test that old files can still be read.</summary>
    public static byte[] WriteVersion1(IReadOnlyCollection<SensorReading> readings)
    {
        using var buffer = new MemoryStream();
        using (var writer = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(Magic);
            writer.Write((byte)1);
            writer.Write(readings.Count);
            foreach (var reading in readings)
            {
                writer.Write(reading.SensorId);
                writer.Write(reading.At.ToUnixTimeMilliseconds());
                writer.Write(reading.Value);
            }
            writer.Write(Crc32.Compute(buffer.GetBuffer().AsSpan(0, (int)buffer.Length)));
        }
        return buffer.ToArray();
    }
}
