using System.Buffers.Binary;
using System.Text;
using CSharpExercises.IO;

namespace CSharpExercises.Tests;

public class BinaryIoTests
{
    private static readonly SensorReading[] Readings =
    [
        new(1, new DateTimeOffset(2026, 10, 27, 8, 0, 0, TimeSpan.Zero), 21.5, "°C"),
        new(2, new DateTimeOffset(2026, 10, 27, 8, 0, 1, 250, TimeSpan.Zero), 1013.25, "hPa"),
        new(1, new DateTimeOffset(2026, 10, 27, 8, 5, 0, TimeSpan.Zero), -0.0001, "°C"),
    ];

    private static byte[] Written()
    {
        using var stream = new MemoryStream();
        ReadingsFile.Write(stream, Readings);
        return stream.ToArray();
    }

    /// <summary>Replaces the checksum after editing the content, to get past the CRC check and test the checks behind it.</summary>
    private static byte[] Reseal(byte[] file)
    {
        var content = file[..^4];
        var sealedFile = new byte[file.Length];
        content.CopyTo(sealedFile, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(sealedFile.AsSpan(^4), Crc32.Compute(content));
        return sealedFile;
    }

    private static List<SensorReading> Read(byte[] file) => ReadingsFile.Read(new MemoryStream(file));

    [Theory]
    [InlineData("", 0x00000000u)]
    [InlineData("123456789", 0xCBF43926u)] // the standard CRC-32 check value
    [InlineData("The quick brown fox jumps over the lazy dog", 0x414FA339u)]
    public void Crc32_MatchesKnownValues(string text, uint expected)
    {
        Assert.Equal(expected, Crc32.Compute(Encoding.ASCII.GetBytes(text)));
    }

    [Fact]
    public void Crc32_CanBeComputedInChunks()
    {
        var data = Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");

        var chunked = Crc32.Append(Crc32.Append(Crc32.Compute(data[..10]), data[10..25]), data[25..]);

        Assert.Equal(Crc32.Compute(data), chunked);
    }

    [Fact]
    public void ReadingsFile_RoundTrips()
    {
        Assert.Equal(Readings, Read(Written()));
    }

    [Fact]
    public void ReadingsFile_StartsWithMagicAndVersion()
    {
        var file = Written();

        Assert.Equal("RDGS"u8.ToArray(), file[..4]);
        Assert.Equal(ReadingsFile.CurrentVersion, file[4]);
        Assert.Equal(3, BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(5)));
    }

    [Fact]
    public void ReadingsFile_ReadsTheOldVersion()
    {
        var old = Read(ReadingsFile.WriteVersion1(Readings));

        Assert.Equal(Readings.Select(r => r with { Unit = "?" }), old);
    }

    [Fact]
    public void ReadingsFile_DetectsAFlippedBit()
    {
        var file = Written();
        file[20] ^= 0b0000_0100;

        var ex = Assert.Throws<InvalidDataException>(() => Read(file));
        Assert.Contains("Checksum", ex.Message);
    }

    [Fact]
    public void ReadingsFile_DetectsTruncation()
    {
        Assert.Throws<InvalidDataException>(() => Read(Written()[..^10]));
    }

    [Fact]
    public void ReadingsFile_RejectsOtherFiles()
    {
        var ex = Assert.Throws<InvalidDataException>(() => Read("%PDF-1.7 not ours"u8.ToArray()));
        Assert.Equal("Not a readings file.", ex.Message);
    }

    [Fact]
    public void ReadingsFile_RejectsVersionsFromTheFuture()
    {
        var file = Written();
        file[4] = 9;

        var ex = Assert.Throws<InvalidDataException>(() => Read(Reseal(file)));
        Assert.Contains("Unsupported version 9", ex.Message);
    }

    [Fact]
    public void ReadingsFile_ACountThatLiesIsReportedNotTrusted()
    {
        var file = Written();
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(5), int.MaxValue);

        var ex = Assert.Throws<InvalidDataException>(() => Read(Reseal(file)));
        Assert.Contains("ends after 3", ex.Message);
    }

    [Fact]
    public void PacketHeader_UsesNetworkByteOrder()
    {
        var header = new PacketHeader(1, PacketFlags.Ack | PacketFlags.Final, Type: 0x0102, Sequence: 0x0A0B0C0D, PayloadLength: 5);
        var bytes = new byte[PacketHeader.Size];

        header.WriteTo(bytes);

        Assert.Equal([1, 5, 0x01, 0x02, 0x0A, 0x0B, 0x0C, 0x0D, 0, 0, 0, 5], bytes);
        Assert.True(PacketHeader.TryRead(bytes, out var parsed));
        Assert.Equal(header, parsed);
    }

    [Theory]
    [InlineData(new byte[] { 1, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0 }, "too short")]
    [InlineData(new byte[] { 2, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0 }, "unknown version")]
    [InlineData(new byte[] { 1, 0x80, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0 }, "unknown flag")]
    [InlineData(new byte[] { 1, 0, 0, 1, 0, 0, 0, 1, 0xFF, 0xFF, 0xFF, 0xFF }, "4 GB payload")]
    public void PacketHeader_RejectsBadInput(byte[] bytes, string reason)
    {
        Assert.False(PacketHeader.TryRead(bytes, out _), reason);
    }

    [Fact]
    public async Task CountingStream_CountsBothDirections()
    {
        var source = new CountingStream(new MemoryStream(new byte[10_000]));
        var destination = new CountingStream(new MemoryStream());

        await source.CopyToAsync(destination);

        Assert.Equal(10_000, source.BytesRead);
        Assert.Equal(10_000, destination.BytesWritten);
    }

    [Fact]
    public async Task CompressedSize_RepetitiveTextShrinksRandomBytesDoNot()
    {
        var text = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("2026-10-27 INFO request handled in 12 ms\n", 25_000)));
        var noise = new byte[text.Length];
        new Random(1).NextBytes(noise);

        var textSize = await StreamTools.CompressedSizeAsync(new MemoryStream(text));
        var noiseSize = await StreamTools.CompressedSizeAsync(new MemoryStream(noise));

        Assert.True(textSize < text.Length / 100, $"{textSize} bytes"); // over 99% smaller
        Assert.True(noiseSize > noise.Length * 0.99, $"{noiseSize} bytes"); // random data does not compress
    }

    [Fact]
    public async Task SumColumn_StreamsLineByLine()
    {
        var csv = new StringBuilder("id,name,amount\n");
        for (var i = 1; i <= 100_000; i++) csv.Append(i).Append(",item").Append(i).Append(',').Append("0.5\n");

        var (rows, sum) = await StreamTools.SumColumnAsync(new MemoryStream(Encoding.UTF8.GetBytes(csv.ToString())), column: 2);

        Assert.Equal(100_000, rows);
        Assert.Equal(50_000, sum);
    }

    [Fact]
    public async Task SumColumn_ReportsShortRows()
    {
        var csv = "a,b\n1,2\n3\n"u8.ToArray();

        var ex = await Assert.ThrowsAsync<InvalidDataException>(() => StreamTools.SumColumnAsync(new MemoryStream(csv), column: 1));
        Assert.Contains("Row 3", ex.Message);
    }
}
