namespace CSharpExercises.Hashing;

/// <summary>
/// A probabilistic set: answers "definitely not present" or "probably present", in a fraction of the memory a real set
/// would need. Used to skip expensive lookups, e.g. checking a disk or a database only for keys that might be there.
/// Items cannot be removed, since clearing a bit could also remove other items that share it.
/// </summary>
public sealed class BloomFilter
{
    private readonly ulong[] _words;

    /// <summary>
    /// Sizes the filter for <paramref name="expectedItems"/> with the given false-positive rate, using the standard formulas
    /// m = -n·ln(p) / ln(2)² bits and k = (m / n)·ln(2) hash functions.
    /// </summary>
    public BloomFilter(int expectedItems, double falsePositiveRate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedItems);
        if (falsePositiveRate is <= 0 or >= 1) throw new ArgumentOutOfRangeException(nameof(falsePositiveRate), "Must be between 0 and 1.");

        BitCount = (int)Math.Ceiling(-expectedItems * Math.Log(falsePositiveRate) / (Math.Log(2) * Math.Log(2)));
        HashCount = Math.Max(1, (int)Math.Round((double)BitCount / expectedItems * Math.Log(2)));
        _words = new ulong[(BitCount + 63) / 64];
    }

    public int BitCount { get; }
    public int HashCount { get; }
    public int SizeInBytes => _words.Length * sizeof(ulong);

    public void Add(string item)
    {
        foreach (var bit in BitPositions(item)) _words[bit >> 6] |= 1UL << (bit & 63);
    }

    /// <summary>False means the item was never added. True means it probably was.</summary>
    public bool MightContain(string item)
    {
        foreach (var bit in BitPositions(item))
        {
            if ((_words[bit >> 6] & (1UL << (bit & 63))) == 0) return false;
        }
        return true;
    }

    /// <summary>The expected false-positive rate after <paramref name="itemsAdded"/> items: (1 - e^(-k·n/m))^k.</summary>
    public double ExpectedFalsePositiveRate(int itemsAdded) =>
        Math.Pow(1 - Math.Exp(-(double)HashCount * itemsAdded / BitCount), HashCount);

    /// <summary>
    /// k bit positions from a single 64-bit hash, by double hashing: position i = h1 + i·h2 (Kirsch and Mitzenmacher).
    /// As good as k independent hash functions for a Bloom filter, at the cost of one.
    /// </summary>
    private IEnumerable<int> BitPositions(string item)
    {
        var hash = StableHash.Of(item);
        var h1 = (uint)hash;
        var h2 = (uint)(hash >> 32) | 1; // odd, so the steps never all land on the same bit

        for (var i = 0u; i < HashCount; i++)
            yield return (int)((h1 + (ulong)i * h2) % (ulong)BitCount);
    }
}
