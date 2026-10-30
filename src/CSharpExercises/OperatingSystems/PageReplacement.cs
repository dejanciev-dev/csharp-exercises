namespace CSharpExercises.OperatingSystems;

/// <summary>
/// Which page to evict when physical memory is full. FIFO is simple but suffers Belady's anomaly (more frames can mean
/// more faults); LRU approximates "not needed soon" by "not used recently"; OPT evicts the page used furthest in the
/// future, unbeatable but needs to know the future, so it's the benchmark. Clock is the cheap LRU approximation real
/// kernels use: a reference bit per frame and a sweeping hand.
/// </summary>
public static class PageReplacement
{
    public static int Fifo(IReadOnlyList<int> references, int frames)
    {
        var resident = new HashSet<int>();
        var order = new Queue<int>();
        var faults = 0;
        foreach (var page in references)
        {
            if (resident.Contains(page)) continue;
            faults++;
            if (resident.Count == frames) resident.Remove(order.Dequeue());
            resident.Add(page);
            order.Enqueue(page);
        }
        return faults;
    }

    public static int Lru(IReadOnlyList<int> references, int frames)
    {
        var lastUsed = new Dictionary<int, int>();
        var faults = 0;
        for (var t = 0; t < references.Count; t++)
        {
            var page = references[t];
            if (!lastUsed.ContainsKey(page))
            {
                faults++;
                if (lastUsed.Count == frames) lastUsed.Remove(lastUsed.MinBy(p => p.Value).Key);
            }
            lastUsed[page] = t;
        }
        return faults;
    }

    public static int Optimal(IReadOnlyList<int> references, int frames)
    {
        var resident = new HashSet<int>();
        var faults = 0;
        for (var t = 0; t < references.Count; t++)
        {
            var page = references[t];
            if (resident.Contains(page)) continue;
            faults++;
            if (resident.Count == frames)
            {
                int NextUse(int p)
                {
                    for (var u = t + 1; u < references.Count; u++)
                        if (references[u] == p) return u;
                    return int.MaxValue; // never used again: the perfect victim
                }
                resident.Remove(resident.MaxBy(NextUse));
            }
            resident.Add(page);
        }
        return faults;
    }

    /// <summary>Second chance: the hand skips (and clears) frames whose reference bit is set.</summary>
    public static int Clock(IReadOnlyList<int> references, int frames)
    {
        var pages = new int?[frames];
        var referenced = new bool[frames];
        var hand = 0;
        var faults = 0;

        foreach (var page in references)
        {
            var slot = Array.IndexOf(pages, page);
            if (slot >= 0)
            {
                referenced[slot] = true;
                continue;
            }

            faults++;
            while (pages[hand] is not null && referenced[hand])
            {
                referenced[hand] = false;
                hand = (hand + 1) % frames;
            }
            pages[hand] = page;
            referenced[hand] = true;
            hand = (hand + 1) % frames;
        }
        return faults;
    }
}
