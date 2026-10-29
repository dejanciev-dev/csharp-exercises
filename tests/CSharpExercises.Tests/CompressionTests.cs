using System.Text;
using CSharpExercises.Compression;

namespace CSharpExercises.Tests;

public class CompressionTests
{
    [Theory]
    [InlineData(0UL, new byte[] { 0x00 })]
    [InlineData(1UL, new byte[] { 0x01 })]
    [InlineData(127UL, new byte[] { 0x7F })]
    [InlineData(128UL, new byte[] { 0x80, 0x01 })]
    [InlineData(300UL, new byte[] { 0xAC, 0x02 })]
    public void Varint_UsesSevenBitsPerByte(ulong value, byte[] expected)
    {
        var buffer = new byte[Varint.MaxBytes];
        var written = Varint.Write(buffer, value);

        Assert.Equal(expected, buffer[..written]);
        Assert.Equal(written, Varint.SizeOf(value));
        Assert.True(Varint.TryRead(buffer, out var read, out var bytesRead));
        Assert.Equal((value, written), (read, bytesRead));
    }

    [Fact]
    public void Varint_LargestValueTakesTenBytes()
    {
        var buffer = new byte[Varint.MaxBytes];

        Assert.Equal(10, Varint.Write(buffer, ulong.MaxValue));
        Assert.True(Varint.TryRead(buffer, out var value, out _));
        Assert.Equal(ulong.MaxValue, value);
    }

    [Theory]
    [InlineData(new byte[0])]
    [InlineData(new byte[] { 0x80 })]                                                       // truncated
    [InlineData(new byte[] { 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x01 })] // 11 bytes
    [InlineData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x02 })]       // 65 bits
    public void Varint_RejectsTruncatedOrOverflowingInput(byte[] bytes)
    {
        Assert.False(Varint.TryRead(bytes, out _, out _));
    }

    [Theory]
    [InlineData(0L, 0UL)]
    [InlineData(-1L, 1UL)]
    [InlineData(1L, 2UL)]
    [InlineData(-2L, 3UL)]
    [InlineData(long.MaxValue, ulong.MaxValue - 1)]
    [InlineData(long.MinValue, ulong.MaxValue)]
    public void ZigZag_KeepsSmallMagnitudesSmall(long value, ulong encoded)
    {
        Assert.Equal(encoded, Varint.ZigZagEncode(value));
        Assert.Equal(value, Varint.ZigZagDecode(encoded));
    }

    [Fact]
    public void Deltas_StoreSortedTimestampsCompactly()
    {
        var start = new DateTimeOffset(2026, 10, 29, 9, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var timestamps = Enumerable.Range(0, 1000).Select(i => start + i * 250L).ToArray();

        var encoded = Varint.EncodeDeltas(timestamps);

        Assert.Equal(timestamps, Varint.DecodeDeltas(encoded));
        Assert.True(encoded.Length < 2100, $"{encoded.Length} bytes instead of {timestamps.Length * 8}");
    }

    [Fact]
    public void Deltas_RoundTripExtremesByWrappingAround()
    {
        long[] values = [long.MinValue, long.MaxValue, 0, -5, long.MaxValue, long.MinValue];

        Assert.Equal(values, Varint.DecodeDeltas(Varint.EncodeDeltas(values)));
        Assert.Empty(Varint.DecodeDeltas(Varint.EncodeDeltas([])));
    }

    [Fact]
    public void Deltas_DistrustTheCount()
    {
        var buffer = new byte[Varint.MaxBytes];
        var lyingCount = buffer[..Varint.Write(buffer, 1_000_000_000)];

        Assert.Throws<InvalidDataException>(() => Varint.DecodeDeltas(lyingCount));
        Assert.Throws<InvalidDataException>(() => Varint.DecodeDeltas([.. Varint.EncodeDeltas([1, 2]), 0x00]));
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("aaaaaaaa")]
    [InlineData("abracadabra")]
    [InlineData("Ж ünïcödé 🎉 text")]
    public void Huffman_RoundTrips(string text)
    {
        var data = Encoding.UTF8.GetBytes(text);

        Assert.Equal(data, Huffman.Decompress(Huffman.Compress(data)));
    }

    [Fact]
    public void Huffman_RoundTripsEveryByteValue()
    {
        var random = new Random(42);
        var data = new byte[10_000];
        random.NextBytes(data);
        // make sure every value appears
        for (var i = 0; i < 256; i++) data[i] = (byte)i;

        Assert.Equal(data, Huffman.Decompress(Huffman.Compress(data)));
    }

    [Fact]
    public void Huffman_GivesFrequentBytesShorterCodes()
    {
        var codes = Huffman.Codes("aaaaaaaabbbbccd"u8);

        Assert.Equal(new Dictionary<byte, string>
        {
            [(byte)'a'] = "0",
            [(byte)'b'] = "10",
            [(byte)'c'] = "110",
            [(byte)'d'] = "111",
        }, codes);
    }

    [Fact]
    public void Huffman_CodesArePrefixFree()
    {
        var codes = Huffman.Codes(Encoding.UTF8.GetBytes("the quick brown fox jumps over the lazy dog")).Values.ToList();

        Assert.DoesNotContain(codes, a => codes.Any(b => a != b && b.StartsWith(a)));
    }

    [Fact]
    public void Huffman_ShrinksSkewedText()
    {
        var text = string.Concat(Enumerable.Repeat("to be or not to be, that is the question. ", 50));
        var data = Encoding.ASCII.GetBytes(text);

        var compressed = Huffman.Compress(data);

        Assert.True(compressed.Length < data.Length * 0.6, $"{compressed.Length} of {data.Length} bytes");
    }

    [Fact]
    public void Huffman_RejectsCorruptData()
    {
        var compressed = Huffman.Compress("abracadabra"u8);

        Assert.Throws<InvalidDataException>(() => Huffman.Decompress(compressed[..^1]));
        Assert.Throws<InvalidDataException>(() => Huffman.Decompress([0xFF, 0xFF, 0xFF, 0x0F, 0x01, 0x61, 0x01]));
        Assert.Throws<InvalidDataException>(() => Huffman.Decompress([0x03, 0x03, 0x61, 0x01, 0x62, 0x01, 0x63, 0x01, 0x00]));
    }

    [Fact]
    public void Base32_EncodesFiveBitsPerCharacter()
    {
        Assert.Equal("", Base32.Encode([]));
        Assert.Equal("ZW", Base32.Encode([0xFF]));
        Assert.Equal("0000000000", Base32.Encode(new byte[6]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(17)]
    public void Base32_RoundTrips(int length)
    {
        var data = new byte[length];
        new Random(length).NextBytes(data);

        Assert.True(Base32.TryDecode(Base32.Encode(data), out var decoded));
        Assert.Equal(data, decoded);
    }

    [Theory]
    [InlineData("zw")]
    [InlineData("Z-W")]
    public void Base32_ForgivesHowHumansTypeCodes(string text)
    {
        Assert.True(Base32.TryDecode(text, out var data));
        Assert.Equal([0xFF], data);
    }

    [Theory]
    [InlineData("ZX")]  // non-zero padding bits
    [InlineData("ZWA")] // a length no encoding produces
    [InlineData("ZU")]  // U is not in the alphabet
    public void Base32_RejectsNonCanonicalText(string text)
    {
        Assert.False(Base32.TryDecode(text, out _));
    }

    [Fact]
    public void Base32_NumbersCarryACheckSymbol()
    {
        Assert.Equal("16J", Base32.EncodeNumber(1234));
        Assert.Equal("16JD", Base32.EncodeNumber(1234, withCheck: true));

        Assert.True(Base32.TryDecodeNumber("i6j-d", withCheck: true, out var value));
        Assert.Equal(1234UL, value);
        Assert.False(Base32.TryDecodeNumber("16KD", withCheck: true, out _)); // one character mistyped
        Assert.False(Base32.TryDecodeNumber("1J6D", withCheck: true, out _)); // two characters swapped
    }

    [Fact]
    public void Base32_NumbersRoundTripAndRejectOverflow()
    {
        var max = Base32.EncodeNumber(ulong.MaxValue, withCheck: true);

        Assert.True(Base32.TryDecodeNumber(max, withCheck: true, out var value));
        Assert.Equal(ulong.MaxValue, value);
        Assert.False(Base32.TryDecodeNumber("Z" + Base32.EncodeNumber(ulong.MaxValue), withCheck: false, out _));
    }
}
