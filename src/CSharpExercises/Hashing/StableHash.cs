using System.Text;

namespace CSharpExercises.Hashing;

/// <summary>
/// A hash that gives the same number on every run and every machine. <c>string.GetHashCode()</c> is randomised per
/// process (to stop hash-flooding attacks), so it must never be stored, sent over the network, or used to pick a
/// server or a bit position that another process has to agree on.
/// </summary>
public static class StableHash
{
    private const ulong OffsetBasis = 14695981039346656037;
    private const ulong Prime = 1099511628211;

    /// <summary>64-bit FNV-1a: XOR in each byte, then multiply by a prime. Simple and fast, not cryptographic.</summary>
    public static ulong Fnv1a64(ReadOnlySpan<byte> data)
    {
        var hash = OffsetBasis;
        foreach (var b in data)
        {
            hash ^= b;
            hash *= Prime;
        }
        return hash;
    }

    /// <summary>Hashes the UTF-8 bytes, so the result does not depend on the platform's string encoding.</summary>
    public static ulong Fnv1a64(string text) => Fnv1a64(Encoding.UTF8.GetBytes(text));

    /// <summary>
    /// The SplitMix64 finalizer: scrambles the bits so that similar inputs ("node-1", "node-2") end up far apart.
    /// FNV alone leaves related keys clustered in the high bits.
    /// </summary>
    public static ulong Mix(ulong x)
    {
        x ^= x >> 30;
        x *= 0xBF58476D1CE4E5B9;
        x ^= x >> 27;
        x *= 0x94D049BB133111EB;
        x ^= x >> 31;
        return x;
    }

    public static ulong Of(string text) => Mix(Fnv1a64(text));
}
