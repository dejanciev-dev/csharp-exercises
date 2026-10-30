namespace CSharpExercises.Collections;

/// <summary>
/// A set of numbers stored as sorted, non-overlapping half-open ranges [start, end). Adding a range merges it with
/// everything it touches; removing one can split a range in two. Good for free/busy time, IP blocks, or which byte
/// ranges of a download have arrived. Lookups binary-search the sorted starts.
/// </summary>
public sealed class IntervalSet
{
    private readonly List<(long Start, long End)> _ranges = [];

    public IReadOnlyList<(long Start, long End)> Ranges => _ranges;

    public long TotalLength => _ranges.Sum(r => r.End - r.Start);

    public void Add(long start, long end)
    {
        if (start >= end) return;

        // Every range that overlaps or touches [start, end) is absorbed into it.
        var i = FirstEndingAtOrAfter(start);
        var j = i;
        while (j < _ranges.Count && _ranges[j].Start <= end)
        {
            start = Math.Min(start, _ranges[j].Start);
            end = Math.Max(end, _ranges[j].End);
            j++;
        }
        _ranges.RemoveRange(i, j - i);
        _ranges.Insert(i, (start, end));
    }

    public void Remove(long start, long end)
    {
        if (start >= end) return;

        var i = FirstEndingAtOrAfter(start + 1); // ranges that end exactly at start don't overlap
        var replacements = new List<(long, long)>();
        var j = i;
        while (j < _ranges.Count && _ranges[j].Start < end)
        {
            var (s, e) = _ranges[j];
            if (s < start) replacements.Add((s, start)); // keep the part on the left
            if (e > end) replacements.Add((end, e));     // and on the right
            j++;
        }
        _ranges.RemoveRange(i, j - i);
        _ranges.InsertRange(i, replacements);
    }

    public bool Contains(long point)
    {
        var i = FirstEndingAtOrAfter(point + 1);
        return i < _ranges.Count && _ranges[i].Start <= point;
    }

    /// <summary>The holes between ranges within [from, to): for example the free slots in a calendar.</summary>
    public IEnumerable<(long Start, long End)> Gaps(long from, long to)
    {
        var cursor = from;
        foreach (var (start, end) in _ranges)
        {
            if (end <= cursor) continue;
            if (start >= to) break;
            if (start > cursor) yield return (cursor, start);
            cursor = Math.Max(cursor, end);
        }
        if (cursor < to) yield return (cursor, to);
    }

    /// <summary>Index of the first range whose end is at or after <paramref name="point"/>.</summary>
    private int FirstEndingAtOrAfter(long point)
    {
        int low = 0, high = _ranges.Count;
        while (low < high)
        {
            var mid = (low + high) / 2;
            if (_ranges[mid].End < point) low = mid + 1;
            else high = mid;
        }
        return low;
    }
}
