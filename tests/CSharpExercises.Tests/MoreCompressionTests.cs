using System.Text;
using CSharpExercises.Compression;

namespace CSharpExercises.Tests;

public class MoreCompressionTests
{
    private static readonly byte[] Repetitive = Encoding.ASCII.GetBytes(string.Concat(Enumerable.Repeat("the rain in spain falls mainly on the plain. ", 40)));

    [Fact]
    public void Lz77_RoundTripsAndCompressesRepeats()
    {
        var tokens = Lz77.Compress(Repetitive);

        Assert.Equal(Repetitive, Lz77.Decompress(tokens));
        Assert.True(tokens.Count < Repetitive.Length / 10);
    }

    [Fact]
    public void Lz77_OverlappingCopiesAndEdgeCases()
    {
        var tokens = Lz77.Compress("abcabcabcabcX"u8);
        Assert.Contains(tokens, t => t.Distance == 3 && t.Length > 3); // a reference longer than its distance

        var random = new Random(1);
        foreach (var length in new[] { 0, 1, 2, 3, 1000 })
        {
            var bytes = new byte[length];
            random.NextBytes(bytes);
            Assert.Equal(bytes, Lz77.Decompress(Lz77.Compress(bytes)));
        }
        Assert.Throws<InvalidDataException>(() => Lz77.Decompress([new Lz77Token(5, 2, 0)]));
    }

    [Fact]
    public void Lzw_RoundTripsIncludingTheTrickyCase()
    {
        Assert.Equal(Repetitive, Lzw.Decompress(Lzw.Compress(Repetitive)));
        Assert.True(Lzw.Compress(Repetitive).Count < Repetitive.Length / 5);

        var tricky = "abababababab"u8.ToArray(); // makes the decoder see a code it's defining at that moment
        Assert.Equal(tricky, Lzw.Decompress(Lzw.Compress(tricky)));
        Assert.Equal([84, 79, 66, 69, 79, 82, 78, 79, 84, 256, 258, 260, 265, 259, 261, 263], Lzw.Compress("TOBEORNOTTOBEORTOBEORNOT"u8));
    }

    [Fact]
    public void Lzw_SmallDictionaryStillRoundTrips()
    {
        Assert.Equal(Repetitive, Lzw.Decompress(Lzw.Compress(Repetitive, maxCodes: 300), maxCodes: 300));
        Assert.Throws<InvalidDataException>(() => Lzw.Decompress([65, 999]));
    }

    [Fact]
    public void Bwt_GroupsCharactersAndInverts()
    {
        var (last, index) = BurrowsWheeler.Transform("banana");

        Assert.Equal(("nnbaaa", 3), (last, index));
        Assert.Equal("banana", BurrowsWheeler.Inverse(last, index));

        var text = Encoding.ASCII.GetString(Repetitive);
        var transformed = BurrowsWheeler.Transform(text);
        Assert.Equal(text, BurrowsWheeler.Inverse(transformed.Last, transformed.Index));
        Assert.True(BurrowsWheeler.Runs(transformed.Last) < BurrowsWheeler.Runs(text) / 3);
    }

    [Fact]
    public void MoveToFront_LowersEntropyAfterBwt()
    {
        var text = Encoding.ASCII.GetString(Repetitive);
        var bwt = Encoding.ASCII.GetBytes(BurrowsWheeler.Transform(text).Last);

        var mtf = MoveToFront.Encode(bwt);

        Assert.Equal(bwt, MoveToFront.Decode(mtf));
        Assert.True(MoveToFront.Entropy(mtf) < MoveToFront.Entropy(Repetitive));
        Assert.True(mtf.Count(b => b == 0) > mtf.Length / 2); // runs became zeros
        Assert.Equal([98, 98, 0, 1], MoveToFront.Encode("baab"u8)); // 'a' is at index 98 once 'b' has moved in front of it
    }

    [Fact]
    public void BitPacking_UsesOnlyTheBitsNeeded()
    {
        var values = Enumerable.Range(0, 1000).Select(i => (uint)(i % 64)).ToArray();

        var packed = BitPacking.Pack(values, BitPacking.BitsNeeded(63));

        Assert.Equal(6, BitPacking.BitsNeeded(63));
        Assert.Equal(750, packed.Length);
        Assert.Equal(values, BitPacking.Unpack(packed, values.Length, 6));
        Assert.Throws<ArgumentException>(() => BitPacking.Pack([64], 6));
        Assert.Equal([uint.MaxValue, 0u], BitPacking.Unpack(BitPacking.Pack([uint.MaxValue, 0], 32), 2, 32));
    }

    [Fact]
    public void BitPacking_FrameOfReferenceForTimestamps()
    {
        long[] timestamps = [1_761_800_000_000, 1_761_800_000_250, 1_761_800_001_000, 1_761_800_000_999];

        var (reference, bits, packed) = BitPacking.PackWithReference(timestamps);

        Assert.Equal(10, bits);           // offsets up to 1000 need 10 bits instead of 64
        Assert.Equal(5, packed.Length);
        Assert.Equal(timestamps, BitPacking.UnpackWithReference(reference, bits, packed, timestamps.Length));
    }
}
