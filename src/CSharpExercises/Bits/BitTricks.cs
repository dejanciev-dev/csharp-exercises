using System.Numerics;

namespace CSharpExercises.Bits;

/// <summary>
/// Classic bit twiddling, each written by hand and checked against <see cref="BitOperations"/>, which compiles to a
/// single CPU instruction where one exists.
/// </summary>
public static class BitTricks
{
    /// <summary>A power of two has exactly one bit set, and x - 1 flips that bit and every bit below it.</summary>
    public static bool IsPowerOfTwo(ulong x) => x != 0 && (x & (x - 1)) == 0;

    /// <summary>Kernighan's method: x &amp; (x - 1) clears the lowest set bit, so loop once per set bit.</summary>
    public static int PopCount(ulong x)
    {
        var count = 0;
        for (; x != 0; x &= x - 1) count++;
        return count;
    }

    /// <summary>In two's complement -x = ~x + 1, so x &amp; -x keeps only the lowest set bit.</summary>
    public static ulong LowestSetBit(ulong x) => x & (~x + 1);

    /// <summary>Smear the highest bit into every lower position, then add one.</summary>
    public static ulong NextPowerOfTwo(ulong x)
    {
        if (x <= 1) return 1;
        x--;
        x |= x >> 1;
        x |= x >> 2;
        x |= x >> 4;
        x |= x >> 8;
        x |= x >> 16;
        x |= x >> 32;
        return x + 1;
    }

    /// <summary>Swap ever larger blocks: neighbouring bits, then pairs, nibbles, bytes, and so on.</summary>
    public static uint ReverseBits(uint x)
    {
        x = ((x >> 1) & 0x55555555) | ((x & 0x55555555) << 1);
        x = ((x >> 2) & 0x33333333) | ((x & 0x33333333) << 2);
        x = ((x >> 4) & 0x0F0F0F0F) | ((x & 0x0F0F0F0F) << 4);
        x = ((x >> 8) & 0x00FF00FF) | ((x & 0x00FF00FF) << 8);
        return (x >> 16) | (x << 16);
    }

    /// <summary>The XOR of a list where every value appears twice except one: pairs cancel out.</summary>
    public static int FindUnpaired(IEnumerable<int> values) => values.Aggregate(0, (acc, v) => acc ^ v);

    /// <summary>Swapping without a temporary, as a curiosity: it's slower than a tuple swap on modern CPUs.</summary>
    public static void XorSwap(ref int a, ref int b)
    {
        if (a == b) return;
        a ^= b;
        b ^= a;
        a ^= b;
    }

    /// <summary>Gosper's hack: the next larger number with the same number of set bits, for walking k-subsets.</summary>
    public static ulong NextSamePopCount(ulong x)
    {
        var lowest = x & (~x + 1);
        var ripple = x + lowest;
        return ripple | (((x ^ ripple) >> 2) / lowest);
    }

    /// <summary>Every subset of a bit mask, including the empty one, by counting down within the mask.</summary>
    public static IEnumerable<ulong> SubsetsOf(ulong mask)
    {
        var subset = mask;
        while (true)
        {
            yield return subset;
            if (subset == 0) yield break;
            subset = (subset - 1) & mask;
        }
    }
}
