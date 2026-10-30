using System.Numerics;
using CSharpExercises.Bits;

namespace CSharpExercises.Tests;

public class BitsTests
{
    [Theory]
    [InlineData(0UL, false)]
    [InlineData(1UL, true)]
    [InlineData(64UL, true)]
    [InlineData(96UL, false)]
    [InlineData(1UL << 63, true)]
    public void IsPowerOfTwo(ulong x, bool expected) => Assert.Equal(expected, BitTricks.IsPowerOfTwo(x));

    [Fact]
    public void Tricks_AgreeWithBitOperations()
    {
        var random = new Random(1);
        for (var i = 0; i < 1000; i++)
        {
            var x = (ulong)random.NextInt64() ^ ((ulong)random.Next() << 40);
            Assert.Equal(BitOperations.PopCount(x), BitTricks.PopCount(x));
            if (x != 0) Assert.Equal(1UL << BitOperations.TrailingZeroCount(x), BitTricks.LowestSetBit(x));
            if (x <= 1UL << 62) Assert.Equal(BitOperations.RoundUpToPowerOf2(x == 0 ? 1 : x), BitTricks.NextPowerOfTwo(x));
        }
    }

    [Fact]
    public void Tricks_ReverseXorAndGosper()
    {
        Assert.Equal(0x80000000u, BitTricks.ReverseBits(1));
        Assert.Equal(0x0F000000u, BitTricks.ReverseBits(0xF0));
        Assert.Equal(7, BitTricks.FindUnpaired([4, 7, 2, 4, 2]));

        int a = 3, b = 9;
        BitTricks.XorSwap(ref a, ref b);
        Assert.Equal((9, 3), (a, b));

        Assert.Equal(0b1011UL, BitTricks.NextSamePopCount(0b0111));
        Assert.Equal(0b1101UL, BitTricks.NextSamePopCount(0b1011));
    }

    [Fact]
    public void Tricks_EnumeratesSubsetsOfAMask()
    {
        var subsets = BitTricks.SubsetsOf(0b1010).ToArray();
        Assert.Equal([0b1010UL, 0b1000, 0b0010, 0], subsets);
    }

    [Fact]
    public void BitSet_SetOperations()
    {
        var evens = new BitSet(100);
        var threes = new BitSet(100);
        for (var i = 0; i < 100; i += 2) evens[i] = true;
        for (var i = 0; i < 100; i += 3) threes[i] = true;

        evens.IntersectWith(threes);

        Assert.Equal(Enumerable.Range(0, 17).Select(i => i * 6), evens);
        Assert.Equal(17, evens.Count);
        Assert.Throws<ArgumentOutOfRangeException>(() => evens[100]);
    }

    [Fact]
    public void BitSet_Sieve()
    {
        Assert.Equal([2, 3, 5, 7, 11, 13, 17, 19, 23, 29], BitSet.PrimesBelow(30));
        Assert.Equal(78498, BitSet.PrimesBelow(1_000_000).Count);
    }

    [Fact]
    public void GrayCode_NeighboursDifferInOneBit()
    {
        var codes = GrayCode.Sequence(4).ToArray();

        Assert.Equal(["000", "001", "011", "010", "110", "111", "101", "100"], GrayCode.Sequence(3).Select(c => GrayCode.ToBinary(c, 3)));
        for (var i = 1; i < codes.Length; i++) Assert.Equal(1, BitOperations.PopCount(codes[i] ^ codes[i - 1]));
        for (var i = 0u; i < 1000; i++) Assert.Equal(i, GrayCode.Decode(GrayCode.Encode(i)));
    }

    [Fact]
    public void Morton_InterleavesAndRoundTrips()
    {
        Assert.Equal(0b1111UL, MortonCode.Encode(0b11, 0b11));
        Assert.Equal(0b0101UL, MortonCode.Encode(0b11, 0));
        Assert.Equal(0b1010UL, MortonCode.Encode(0, 0b11));
        Assert.Equal((123456u, uint.MaxValue), MortonCode.Decode(MortonCode.Encode(123456, uint.MaxValue)));
    }

    [Fact]
    public void Tsp_FindsTheShortestTour()
    {
        int[,] d =
        {
            { 0, 10, 15, 20 },
            { 10, 0, 35, 25 },
            { 15, 35, 0, 30 },
            { 20, 25, 30, 0 },
        };

        var (cost, tour) = TravellingSalesman.Solve(d);

        Assert.Equal(80, cost);
        Assert.Equal(0, tour[0]);
        Assert.Equal(0, tour[^1]);
        Assert.Equal(cost, tour.Zip(tour.Skip(1)).Sum(p => d[p.First, p.Second]));
    }
}
