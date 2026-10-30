using CSharpExercises.Hashing;

namespace CSharpExercises.Statistics;

/// <summary>
/// Count-min sketch: approximate counts for a huge number of keys in fixed memory. Each key increments one counter
/// in each of d rows; collisions only ever add, so the minimum across rows is the best estimate and never too low.
/// Paired with a small heap it finds the "heavy hitters": the most frequent search terms or IP addresses.
/// </summary>
public sealed class CountMinSketch
{
    private readonly long[,] _counts;
    private readonly int _width;
    private readonly int _depth;

    /// <param name="epsilon">Overestimate is at most epsilon × total count…</param>
    /// <param name="delta">…with probability 1 - delta.</param>
    public CountMinSketch(double epsilon = 0.001, double delta = 0.01)
    {
        _width = (int)Math.Ceiling(Math.E / epsilon);
        _depth = (int)Math.Ceiling(Math.Log(1 / delta));
        _counts = new long[_depth, _width];
    }

    public long Total { get; private set; }

    public void Add(string key, long count = 1)
    {
        var (h1, h2) = Hashes(key);
        for (var row = 0; row < _depth; row++) _counts[row, Column(h1, h2, row)] += count;
        Total += count;
    }

    public long Estimate(string key)
    {
        var (h1, h2) = Hashes(key);
        var min = long.MaxValue;
        for (var row = 0; row < _depth; row++) min = Math.Min(min, _counts[row, Column(h1, h2, row)]);
        return min;
    }

    /// <summary>The k keys with the highest estimated counts from a stream, keeping only k candidates in memory.</summary>
    public static IReadOnlyList<(string Key, long Count)> TopK(IEnumerable<string> stream, int k)
    {
        var sketch = new CountMinSketch();
        var top = new Dictionary<string, long>();

        foreach (var key in stream)
        {
            sketch.Add(key);
            var estimate = sketch.Estimate(key);
            if (top.ContainsKey(key) || top.Count < k) top[key] = estimate;
            else
            {
                var smallest = top.MinBy(p => p.Value);
                if (estimate > smallest.Value)
                {
                    top.Remove(smallest.Key);
                    top[key] = estimate;
                }
            }
        }
        return top.OrderByDescending(p => p.Value).Select(p => (p.Key, p.Value)).ToArray();
    }

    private static (ulong, ulong) Hashes(string key)
    {
        var h = StableHash.Of(key);
        return (h, StableHash.Mix(h ^ 0x5bd1e995) | 1);
    }

    /// <summary>Double hashing: row i uses h1 + i·h2, as good as d independent hash functions in practice.</summary>
    private int Column(ulong h1, ulong h2, int row) => (int)((h1 + (ulong)row * h2) % (ulong)_width);
}
