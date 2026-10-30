namespace CSharpExercises.Search;

/// <summary>
/// Search-as-you-type over a sorted array: all strings with a given prefix form one contiguous block, found with two
/// binary searches; the top k by popularity come from a small heap. Simpler and more cache-friendly than a trie for
/// a fixed vocabulary, and the same "sorted array + binary search" trick works on disk.
/// </summary>
public sealed class Autocomplete
{
    private readonly (string Phrase, long Weight)[] _entries;

    public Autocomplete(IEnumerable<(string Phrase, long Weight)> entries) =>
        _entries = entries
            .GroupBy(e => e.Phrase.ToLowerInvariant())
            .Select(g => (g.Key, g.Sum(e => e.Weight)))
            .OrderBy(e => e.Key, StringComparer.Ordinal)
            .ToArray();

    public IReadOnlyList<string> Suggest(string prefix, int limit = 5)
    {
        prefix = prefix.ToLowerInvariant();
        var from = LowerBound(prefix);
        var to = LowerBound(prefix + char.MaxValue); // just past every string starting with prefix

        var top = new PriorityQueue<string, (long, string)>(Comparer<(long Weight, string Phrase)>.Create((a, b) =>
            a.Weight != b.Weight ? a.Weight.CompareTo(b.Weight) : string.CompareOrdinal(b.Phrase, a.Phrase)));
        for (var i = from; i < to; i++)
        {
            var (phrase, weight) = _entries[i];
            top.Enqueue(phrase, (weight, phrase));
            if (top.Count > limit) top.Dequeue(); // drop the least popular
        }

        var result = new List<string>();
        while (top.Count > 0) result.Add(top.Dequeue());
        result.Reverse();
        return result;
    }

    private int LowerBound(string key)
    {
        int low = 0, high = _entries.Length;
        while (low < high)
        {
            var mid = (low + high) / 2;
            if (string.CompareOrdinal(_entries[mid].Phrase, key) < 0) low = mid + 1;
            else high = mid;
        }
        return low;
    }
}
