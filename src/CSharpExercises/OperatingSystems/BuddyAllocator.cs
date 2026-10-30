using System.Numerics;

namespace CSharpExercises.OperatingSystems;

/// <summary>
/// The buddy system (used by the Linux page allocator): memory is split into power-of-two blocks. A request is rounded
/// up and a larger block is halved until it fits. Each block's "buddy" is found by flipping one bit of its offset,
/// so when both halves are free they merge back instantly. Fast and fragmentation-resistant, at the cost of
/// internal waste from rounding up.
/// </summary>
public sealed class BuddyAllocator
{
    private readonly int _minOrder;
    private readonly int _maxOrder;
    private readonly SortedSet<int>[] _freeLists; // one per order: offsets of free blocks of size 2^order
    private readonly Dictionary<int, int> _allocatedOrder = new();

    public BuddyAllocator(int totalSize, int minBlockSize = 16)
    {
        if (!BitOperations.IsPow2(totalSize) || !BitOperations.IsPow2(minBlockSize))
            throw new ArgumentException("Sizes must be powers of two.");
        _minOrder = BitOperations.Log2((uint)minBlockSize);
        _maxOrder = BitOperations.Log2((uint)totalSize);
        _freeLists = Enumerable.Range(0, _maxOrder + 1).Select(_ => new SortedSet<int>()).ToArray();
        _freeLists[_maxOrder].Add(0);
    }

    public int AllocatedBytes => _allocatedOrder.Values.Sum(o => 1 << o);

    public int? Allocate(int size)
    {
        var order = Math.Max(_minOrder, BitOperations.Log2(BitOperations.RoundUpToPowerOf2((uint)size)));
        if (order > _maxOrder) return null;

        // Find the smallest free block that's big enough…
        var found = order;
        while (found <= _maxOrder && _freeLists[found].Count == 0) found++;
        if (found > _maxOrder) return null;

        var offset = _freeLists[found].Min;
        _freeLists[found].Remove(offset);

        // …and split it in halves, freeing the upper half each time, until it's the right size.
        while (found > order)
        {
            found--;
            _freeLists[found].Add(offset + (1 << found));
        }

        _allocatedOrder[offset] = order;
        return offset;
    }

    public void Free(int offset)
    {
        if (!_allocatedOrder.Remove(offset, out var order)) throw new InvalidOperationException($"No block allocated at {offset}.");

        while (order < _maxOrder)
        {
            var buddy = offset ^ (1 << order); // the other half of the parent block
            if (!_freeLists[order].Remove(buddy)) break;
            offset = Math.Min(offset, buddy);
            order++;
        }
        _freeLists[order].Add(offset);
    }

    /// <summary>Free blocks per size, e.g. { 64: [64], 128: [128] }.</summary>
    public IReadOnlyDictionary<int, int[]> FreeBlocks =>
        Enumerable.Range(0, _maxOrder + 1).Where(o => _freeLists[o].Count > 0).ToDictionary(o => 1 << o, o => _freeLists[o].ToArray());
}
