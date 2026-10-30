namespace CSharpExercises.Bits;

/// <summary>
/// Gray code: a binary ordering where neighbours differ in exactly one bit. Rotary encoders use it so that a reading
/// taken mid-transition is off by at most one step, never wildly wrong as 0111 → 1000 could be in plain binary.
/// </summary>
public static class GrayCode
{
    public static uint Encode(uint n) => n ^ (n >> 1);

    /// <summary>Each binary bit is the XOR of all Gray bits at and above it: fold the prefix XOR down.</summary>
    public static uint Decode(uint gray)
    {
        var n = gray;
        for (var shift = 1; shift < 32; shift <<= 1) n ^= n >> shift;
        return n;
    }

    /// <summary>The 2^bits codes in order, e.g. 00, 01, 11, 10 for two bits.</summary>
    public static IEnumerable<uint> Sequence(int bits)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bits);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(bits, 31);
        for (var i = 0u; i < 1u << bits; i++) yield return Encode(i);
    }

    public static string ToBinary(uint value, int bits) => Convert.ToString(value, 2).PadLeft(bits, '0');
}
