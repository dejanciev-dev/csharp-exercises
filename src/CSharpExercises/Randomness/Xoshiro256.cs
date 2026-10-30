using System.Numerics;

namespace CSharpExercises.Randomness;

/// <summary>
/// xoshiro256**, a small, fast generator with good statistical quality (the algorithm behind
/// <see cref="Random"/>'s default implementation since .NET 6). Seeded generators make simulations and tests
/// reproducible. Not for secrets: use <see cref="System.Security.Cryptography.RandomNumberGenerator"/> for those.
/// </summary>
public sealed class Xoshiro256
{
    private ulong _s0, _s1, _s2, _s3;

    /// <summary>Expands one seed into 256 bits of state with SplitMix64, so similar seeds still give unrelated streams.</summary>
    public Xoshiro256(ulong seed)
    {
        _s0 = SplitMix(ref seed);
        _s1 = SplitMix(ref seed);
        _s2 = SplitMix(ref seed);
        _s3 = SplitMix(ref seed);
    }

    public ulong NextUInt64()
    {
        var result = BitOperations.RotateLeft(_s1 * 5, 7) * 9;
        var t = _s1 << 17;

        _s2 ^= _s0;
        _s3 ^= _s1;
        _s1 ^= _s2;
        _s0 ^= _s3;
        _s2 ^= t;
        _s3 = BitOperations.RotateLeft(_s3, 45);

        return result;
    }

    /// <summary>A double in [0, 1) from the top 53 bits, the precision of a double's mantissa.</summary>
    public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1UL << 53));

    /// <summary>
    /// A uniform integer in [0, bound). "x % bound" is biased when bound doesn't divide 2^64; Lemire's method
    /// multiplies instead and rejects the few values that would cause bias.
    /// </summary>
    public ulong NextBelow(ulong bound)
    {
        ArgumentOutOfRangeException.ThrowIfZero(bound);
        var threshold = (0 - bound) % bound; // 2^64 mod bound
        while (true)
        {
            var product = (UInt128)NextUInt64() * bound;
            if ((ulong)product >= threshold) return (ulong)(product >> 64);
        }
    }

    public int Next(int minInclusive, int maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(minInclusive, maxExclusive);
        return (int)((long)minInclusive + (long)NextBelow((ulong)((long)maxExclusive - minInclusive)));
    }

    private static ulong SplitMix(ref ulong state)
    {
        var z = state += 0x9E3779B97F4A7C15;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EB;
        return z ^ (z >> 31);
    }
}
