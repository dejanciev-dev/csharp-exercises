namespace CSharpExercises.OperatingSystems;

public enum FitStrategy { FirstFit, BestFit, WorstFit }

/// <summary>
/// A heap allocator over one contiguous region, keeping free blocks in an address-ordered list. Allocation splits a
/// free block; freeing merges it with free neighbours (coalescing) so the memory doesn't crumble into pieces too small
/// to use. Fragmentation is measured as the share of free memory that is not in the largest block.
/// </summary>
public sealed class FreeListAllocator
{
    private readonly SortedDictionary<int, int> _free = new();   // offset → size
    private readonly Dictionary<int, int> _allocated = new();     // offset → size
    private readonly FitStrategy _strategy;

    public FreeListAllocator(int size, FitStrategy strategy = FitStrategy.FirstFit)
    {
        Size = size;
        _strategy = strategy;
        _free[0] = size;
    }

    public int Size { get; }
    public int FreeBytes => _free.Values.Sum();
    public int LargestFreeBlock => _free.Count == 0 ? 0 : _free.Values.Max();

    public double Fragmentation => FreeBytes == 0 ? 0 : 1 - (double)LargestFreeBlock / FreeBytes;

    /// <summary>Returns the offset of the new block, or null when no free block is big enough.</summary>
    public int? Allocate(int size)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        var candidates = _free.Where(b => b.Value >= size);
        KeyValuePair<int, int>? chosen = _strategy switch
        {
            FitStrategy.FirstFit => candidates.Cast<KeyValuePair<int, int>?>().FirstOrDefault(),
            FitStrategy.BestFit => candidates.Cast<KeyValuePair<int, int>?>().MinBy(b => b!.Value.Value),
            FitStrategy.WorstFit => candidates.Cast<KeyValuePair<int, int>?>().MaxBy(b => b!.Value.Value),
            _ => throw new InvalidOperationException(),
        };
        if (chosen is not { } block) return null;

        _free.Remove(block.Key);
        if (block.Value > size) _free[block.Key + size] = block.Value - size; // keep the remainder free
        _allocated[block.Key] = size;
        return block.Key;
    }

    public void Free(int offset)
    {
        if (!_allocated.Remove(offset, out var size)) throw new InvalidOperationException($"No block allocated at {offset} (double free?).");

        // Merge with the following free block…
        if (_free.Remove(offset + size, out var nextSize)) size += nextSize;

        // …and with the preceding one if it ends exactly here.
        var previous = _free.LastOrDefault(b => b.Key < offset);
        if (_free.Count > 0 && previous.Value > 0 && previous.Key + previous.Value == offset)
        {
            _free[previous.Key] = previous.Value + size;
            return;
        }
        _free[offset] = size;
    }

    public IReadOnlyList<(int Offset, int Size)> FreeBlocks => _free.Select(b => (b.Key, b.Value)).ToList();
}
