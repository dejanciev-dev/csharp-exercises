using CSharpExercises.OperatingSystems;

namespace CSharpExercises.Tests;

public class OperatingSystemsTests
{
    private static readonly Process[] Jobs = [new("A", 0, 8), new("B", 1, 4), new("C", 2, 9), new("D", 3, 5)];

    [Fact]
    public void Cpu_FcfsSuffersTheConvoyEffect()
    {
        var fcfs = CpuScheduling.FirstComeFirstServed(Jobs);

        Assert.Equal("|A:0-8|B:8-12|C:12-21|D:21-26|", fcfs.Gantt);
        Assert.Equal(8.75, fcfs.AverageWaiting); // (0 + 7 + 10 + 18) / 4
    }

    [Fact]
    public void Cpu_ShortestJobFirstWaitsLess()
    {
        var sjf = CpuScheduling.ShortestJobFirst(Jobs);

        Assert.Equal("|A:0-8|B:8-12|D:12-17|C:17-26|", sjf.Gantt);
        Assert.Equal(7.75, sjf.AverageWaiting);
    }

    [Fact]
    public void Cpu_RoundRobinSharesTheCore()
    {
        var rr = CpuScheduling.RoundRobin([new("P1", 0, 5), new("P2", 0, 3), new("P3", 0, 1)], quantum: 2);

        Assert.Equal("|P1:0-2|P2:2-4|P3:4-5|P1:5-7|P2:7-8|P1:8-9|", rr.Gantt);
        Assert.Equal(new Dictionary<string, int> { ["P1"] = 4, ["P2"] = 5, ["P3"] = 4 }, rr.Waiting);
    }

    [Fact]
    public void Pages_CompareAlgorithms()
    {
        int[] refs = [7, 0, 1, 2, 0, 3, 0, 4, 2, 3, 0, 3, 2, 1, 2, 0, 1, 7, 0, 1];

        Assert.Equal(15, PageReplacement.Fifo(refs, 3));
        Assert.Equal(12, PageReplacement.Lru(refs, 3));
        Assert.Equal(9, PageReplacement.Optimal(refs, 3));
        Assert.InRange(PageReplacement.Clock(refs, 3), 9, 15);
    }

    [Fact]
    public void Pages_BeladysAnomaly()
    {
        int[] refs = [1, 2, 3, 4, 1, 2, 5, 1, 2, 3, 4, 5];

        Assert.Equal(9, PageReplacement.Fifo(refs, 3));
        Assert.Equal(10, PageReplacement.Fifo(refs, 4)); // more memory, more faults
        Assert.True(PageReplacement.Lru(refs, 4) <= PageReplacement.Lru(refs, 3)); // LRU never does this
    }

    private static BankersAlgorithm TextbookState() => new(
        available: [3, 3, 2],
        maximum: [[7, 5, 3], [3, 2, 2], [9, 0, 2], [2, 2, 2], [4, 3, 3]],
        allocated: [[0, 1, 0], [2, 0, 0], [3, 0, 2], [2, 1, 1], [0, 0, 2]]);

    [Fact]
    public void Bankers_FindsASafeSequence()
    {
        var sequence = TextbookState().SafeSequence();

        Assert.NotNull(sequence);
        Assert.Equal(5, sequence.Distinct().Count());
        Assert.Equal(1, sequence[0]);
    }

    [Fact]
    public void Bankers_RefusesUnsafeRequests()
    {
        var bank = TextbookState();

        Assert.True(bank.TryRequest(1, [1, 0, 2]));
        Assert.Equal([2, 3, 0], bank.Available);
        Assert.False(bank.TryRequest(0, [0, 2, 0]));   // would leave no safe order
        Assert.Equal([2, 3, 0], bank.Available);        // rolled back
        Assert.False(bank.TryRequest(4, [3, 3, 0]));   // simply not available
        Assert.Throws<InvalidOperationException>(() => bank.TryRequest(3, [5, 0, 0]));
    }

    [Fact]
    public void Disk_ElevatorBeatsFcfs()
    {
        int[] requests = [98, 183, 37, 122, 14, 124, 65, 67];

        Assert.Equal(640, DiskScheduling.Fcfs(53, requests).Movement);
        Assert.Equal(236, DiskScheduling.ShortestSeekFirst(53, requests).Movement);
        Assert.Equal([65, 67, 98, 122, 124, 183, 37, 14], DiskScheduling.Scan(53, requests).Order);
        Assert.Equal(299, DiskScheduling.Scan(53, requests).Movement);
        Assert.Equal(322, DiskScheduling.CircularScan(53, requests).Movement);
    }

    [Fact]
    public void FreeList_SplitsAndCoalesces()
    {
        var heap = new FreeListAllocator(1000);
        var a = heap.Allocate(100)!.Value;
        var b = heap.Allocate(200)!.Value;
        var c = heap.Allocate(300)!.Value;

        Assert.Equal((0, 100, 300), (a, b, c));
        heap.Free(a);
        heap.Free(c);
        Assert.Equal([(0, 100), (300, 700)], heap.FreeBlocks);
        Assert.True(heap.Fragmentation > 0);

        heap.Free(b); // bridges both neighbours
        Assert.Equal([(0, 1000)], heap.FreeBlocks);
        Assert.Equal(0, heap.Fragmentation);
        Assert.Throws<InvalidOperationException>(() => heap.Free(b));
    }

    [Fact]
    public void FreeList_StrategiesPickDifferentHoles()
    {
        FreeListAllocator Fragmented(FitStrategy strategy)
        {
            var heap = new FreeListAllocator(100, strategy);
            var blocks = new[] { 30, 10, 20, 10, 30 }.Select(s => heap.Allocate(s)!.Value).ToArray();
            heap.Free(blocks[0]); // hole of 30 at 0
            heap.Free(blocks[2]); // hole of 20 at 40
            return heap;
        }

        Assert.Equal(0, Fragmented(FitStrategy.FirstFit).Allocate(15));
        Assert.Equal(40, Fragmented(FitStrategy.BestFit).Allocate(15));
        Assert.Equal(0, Fragmented(FitStrategy.WorstFit).Allocate(15));
        Assert.Null(Fragmented(FitStrategy.BestFit).Allocate(40)); // 50 bytes free, but not in one piece
    }

    [Fact]
    public void Buddy_SplitsAndMergesWithBuddies()
    {
        var buddy = new BuddyAllocator(256, minBlockSize: 16);

        var a = buddy.Allocate(20)!.Value;  // rounded up to 32
        var b = buddy.Allocate(60)!.Value;  // 64
        Assert.Equal((0, 64), (a, b));
        Assert.Equal(96, buddy.AllocatedBytes);
        Assert.Equal(new[] { 32 }, buddy.FreeBlocks[32]);
        Assert.Equal(new[] { 128 }, buddy.FreeBlocks[128]);

        buddy.Free(a);
        buddy.Free(b); // everything merges back into one block
        Assert.Equal(new[] { 0 }, buddy.FreeBlocks.Single().Value);
        Assert.Null(buddy.Allocate(512));
    }
}
