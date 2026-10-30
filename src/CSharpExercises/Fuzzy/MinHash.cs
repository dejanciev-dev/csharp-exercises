using CSharpExercises.Hashing;

namespace CSharpExercises.Fuzzy;

/// <summary>
/// MinHash estimates the Jaccard similarity of two large sets (documents as sets of word shingles) from small fixed-size
/// signatures. For a random hash function, the chance that both sets have the same minimum hash equals their Jaccard
/// similarity, so with k hash functions the fraction of agreeing minimums estimates it with error about 1/√k.
/// Used for near-duplicate detection of web pages.
/// </summary>
public sealed class MinHash(int signatureSize = 128)
{
    private readonly ulong[] _seeds = Enumerable.Range(1, signatureSize).Select(i => StableHash.Mix((ulong)i * 0x9E3779B97F4A7C15)).ToArray();

    public ulong[] Signature(IEnumerable<string> set)
    {
        var signature = Enumerable.Repeat(ulong.MaxValue, signatureSize).ToArray();
        foreach (var item in set)
        {
            var h = StableHash.Of(item);
            for (var i = 0; i < signatureSize; i++)
                signature[i] = Math.Min(signature[i], StableHash.Mix(h ^ _seeds[i])); // one cheap "hash function" per seed
        }
        return signature;
    }

    public static double EstimateSimilarity(ulong[] a, ulong[] b) =>
        (double)a.Zip(b).Count(p => p.First == p.Second) / a.Length;

    /// <summary>Word k-shingles: "the quick brown fox" with k = 2 → "the quick", "quick brown", "brown fox".</summary>
    public static HashSet<string> Shingles(string text, int k = 3)
    {
        var words = text.ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return Enumerable.Range(0, Math.Max(0, words.Length - k + 1)).Select(i => string.Join(' ', words[i..(i + k)])).ToHashSet();
    }

    public static double ExactJaccard<T>(IReadOnlySet<T> a, IReadOnlySet<T> b)
    {
        var union = a.Union(b).Count();
        return union == 0 ? 1 : (double)a.Intersect(b).Count() / union;
    }
}
