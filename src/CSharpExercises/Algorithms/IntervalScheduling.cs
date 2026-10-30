namespace CSharpExercises.Algorithms;

public readonly record struct Meeting(string Name, int Start, int End, int Weight = 1);

/// <summary>
/// Greedy algorithms over intervals, and where greed stops working. Picking the meeting that ends first is
/// provably optimal for "most meetings in one room"; once meetings have weights, it takes dynamic programming.
/// </summary>
public static class IntervalScheduling
{
    /// <summary>Most non-overlapping meetings: always take the one that frees the room soonest.</summary>
    public static IReadOnlyList<Meeting> MaxNonOverlapping(IEnumerable<Meeting> meetings)
    {
        var chosen = new List<Meeting>();
        var freeAt = int.MinValue;
        foreach (var meeting in meetings.OrderBy(m => m.End))
        {
            if (meeting.Start < freeAt) continue;
            chosen.Add(meeting);
            freeAt = meeting.End;
        }
        return chosen;
    }

    /// <summary>
    /// Fewest rooms to hold every meeting, and which room each goes to: process by start time and reuse the room
    /// that frees up earliest, tracked with a priority queue.
    /// </summary>
    public static Dictionary<string, int> AssignRooms(IEnumerable<Meeting> meetings)
    {
        var rooms = new Dictionary<string, int>();
        var busyUntil = new PriorityQueue<int, int>(); // room number by end time
        var roomCount = 0;

        foreach (var meeting in meetings.OrderBy(m => m.Start))
        {
            int room;
            if (busyUntil.TryPeek(out var free, out var end) && end <= meeting.Start)
            {
                busyUntil.Dequeue();
                room = free;
            }
            else room = roomCount++;

            rooms[meeting.Name] = room;
            busyUntil.Enqueue(room, meeting.End);
        }
        return rooms;
    }

    /// <summary>
    /// Weighted interval scheduling: best[i] is the best total among the first i meetings by end time, either
    /// skipping meeting i or taking it plus the best among the meetings that end before it starts.
    /// </summary>
    public static (int Weight, IReadOnlyList<Meeting> Chosen) MaxWeight(IEnumerable<Meeting> meetings)
    {
        var sorted = meetings.OrderBy(m => m.End).ToArray();
        var ends = sorted.Select(m => m.End).ToArray();
        var best = new int[sorted.Length + 1];

        int LastCompatible(int i) // number of meetings ending at or before sorted[i] starts
        {
            var index = Array.BinarySearch(ends, 0, i, sorted[i].Start + 1);
            return index >= 0 ? FirstAtLeast(ends, index) : ~index;
        }

        for (var i = 0; i < sorted.Length; i++)
            best[i + 1] = Math.Max(best[i], sorted[i].Weight + best[LastCompatible(i)]);

        var chosen = new List<Meeting>();
        for (var i = sorted.Length; i > 0;)
        {
            if (best[i] == best[i - 1]) { i--; continue; }
            chosen.Add(sorted[i - 1]);
            i = LastCompatible(i - 1);
        }
        chosen.Reverse();
        return (best[sorted.Length], chosen);
    }

    private static int FirstAtLeast(int[] sorted, int index)
    {
        while (index > 0 && sorted[index - 1] == sorted[index]) index--;
        return index;
    }
}
