namespace CSharpExercises.Randomness;

/// <summary>
/// Reservoir sampling picks k items uniformly from a stream of unknown length, in one pass and O(k) memory: item n
/// replaces a random reservoir slot with probability k/n. Useful for sampling log lines or a huge query result.
/// </summary>
public sealed class ReservoirSampler<T>(int size, Random random)
{
    private readonly List<T> _reservoir = new(size);

    public long Seen { get; private set; }

    public IReadOnlyList<T> Sample => _reservoir;

    public void Add(T item)
    {
        Seen++;
        if (_reservoir.Count < size)
        {
            _reservoir.Add(item);
            return;
        }

        var slot = random.NextInt64(Seen);
        if (slot < size) _reservoir[(int)slot] = item;
    }

    public static IReadOnlyList<T> SampleFrom(IEnumerable<T> source, int size, Random random)
    {
        var sampler = new ReservoirSampler<T>(size, random);
        foreach (var item in source) sampler.Add(item);
        return sampler.Sample;
    }
}

/// <summary>
/// Weighted sampling without replacement from a stream (Efraimidis–Spirakis): give each item the key u^(1/weight)
/// for a uniform u and keep the k largest keys. Heavier items tend to get larger keys.
/// </summary>
public static class WeightedReservoir
{
    public static IReadOnlyList<T> Sample<T>(IEnumerable<(T Item, double Weight)> source, int size, Random random)
    {
        var heap = new PriorityQueue<T, double>(); // smallest key on top, evicted first
        foreach (var (item, weight) in source)
        {
            if (weight <= 0) continue;
            var key = Math.Pow(random.NextDouble(), 1 / weight);
            if (heap.Count < size) heap.Enqueue(item, key);
            else if (heap.TryPeek(out _, out var smallest) && key > smallest) heap.DequeueEnqueue(item, key);
        }
        return heap.UnorderedItems.Select(e => e.Element).ToArray();
    }
}
