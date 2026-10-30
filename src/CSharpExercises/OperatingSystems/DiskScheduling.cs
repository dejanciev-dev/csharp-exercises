namespace CSharpExercises.OperatingSystems;

/// <summary>
/// Ordering disk head movements. FCFS zig-zags; shortest-seek-time-first is greedy and can starve distant requests;
/// SCAN (the elevator algorithm) sweeps in one direction then reverses; C-SCAN only serves on the way up and jumps
/// back, giving more uniform waiting times. The same ideas schedule elevators and delivery routes on a line.
/// </summary>
public static class DiskScheduling
{
    public static (IReadOnlyList<int> Order, int Movement) Fcfs(int head, IEnumerable<int> requests) => Measure(head, requests.ToList());

    public static (IReadOnlyList<int> Order, int Movement) ShortestSeekFirst(int head, IEnumerable<int> requests)
    {
        var pending = requests.ToList();
        var order = new List<int>();
        var position = head;
        while (pending.Count > 0)
        {
            var next = pending.MinBy(r => (Math.Abs(r - position), r));
            pending.Remove(next);
            order.Add(next);
            position = next;
        }
        return Measure(head, order);
    }

    /// <summary>Elevator: serve everything upward, then everything below on the way back (LOOK: no trip to the end).</summary>
    public static (IReadOnlyList<int> Order, int Movement) Scan(int head, IEnumerable<int> requests, bool upFirst = true)
    {
        var sorted = requests.Order().ToList();
        var up = sorted.Where(r => r >= head).ToList();
        var down = sorted.Where(r => r < head).OrderDescending().ToList();
        var order = upFirst ? up.Concat(down) : down.Concat(up);
        return Measure(head, order.ToList());
    }

    /// <summary>Circular: serve upward, then jump to the lowest request and continue upward (the jump counts as movement).</summary>
    public static (IReadOnlyList<int> Order, int Movement) CircularScan(int head, IEnumerable<int> requests)
    {
        var sorted = requests.Order().ToList();
        return Measure(head, [.. sorted.Where(r => r >= head), .. sorted.Where(r => r < head)]);
    }

    private static (IReadOnlyList<int>, int) Measure(int head, List<int> order)
    {
        var movement = 0;
        var position = head;
        foreach (var r in order)
        {
            movement += Math.Abs(r - position);
            position = r;
        }
        return (order, movement);
    }
}
