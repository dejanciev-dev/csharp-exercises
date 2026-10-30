namespace CSharpExercises.Bits;

/// <summary>
/// Morton (Z-order) codes interleave the bits of x and y into one number. Points close in 2D tend to be close in
/// the 1D order, so a sorted list or B-tree of Morton codes works as a cheap spatial index.
/// </summary>
public static class MortonCode
{
    public static ulong Encode(uint x, uint y) => Spread(x) | (Spread(y) << 1);

    public static (uint X, uint Y) Decode(ulong code) => (Compact(code), Compact(code >> 1));

    /// <summary>Moves bit i of the input to bit 2i, by spreading ever smaller groups apart.</summary>
    private static ulong Spread(uint value)
    {
        ulong x = value;
        x = (x | (x << 16)) & 0x0000FFFF0000FFFF;
        x = (x | (x << 8)) & 0x00FF00FF00FF00FF;
        x = (x | (x << 4)) & 0x0F0F0F0F0F0F0F0F;
        x = (x | (x << 2)) & 0x3333333333333333;
        x = (x | (x << 1)) & 0x5555555555555555;
        return x;
    }

    /// <summary>The reverse of <see cref="Spread"/>: keep the even bits and squeeze them back together.</summary>
    private static uint Compact(ulong x)
    {
        x &= 0x5555555555555555;
        x = (x | (x >> 1)) & 0x3333333333333333;
        x = (x | (x >> 2)) & 0x0F0F0F0F0F0F0F0F;
        x = (x | (x >> 4)) & 0x00FF00FF00FF00FF;
        x = (x | (x >> 8)) & 0x0000FFFF0000FFFF;
        x = (x | (x >> 16)) & 0x00000000FFFFFFFF;
        return (uint)x;
    }
}
