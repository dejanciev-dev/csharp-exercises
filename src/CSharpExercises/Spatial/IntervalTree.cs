namespace CSharpExercises.Spatial;

/// <summary>
/// "Which intervals overlap this one?" (meetings in a time range, genes in a region) in O(log n + matches). A balanced
/// BST of intervals ordered by start, where each node also stores the largest end in its subtree: a whole subtree can
/// be skipped when its largest end is before the query starts. Built once from sorted data, so it is balanced.
/// </summary>
public sealed class IntervalTree<T>
{
    private sealed class Node(long start, long end, T value)
    {
        public readonly long Start = start, End = end;
        public readonly T Value = value;
        public Node? Left, Right;
        public long MaxEnd = end;
    }

    private readonly Node? _root;

    public IntervalTree(IEnumerable<(long Start, long End, T Value)> intervals)
    {
        var sorted = intervals.OrderBy(i => i.Start).ToArray();
        if (sorted.Any(i => i.End < i.Start)) throw new ArgumentException("An interval ends before it starts.", nameof(intervals));
        _root = Build(sorted, 0, sorted.Length);
    }

    public int LastVisited { get; private set; }

    /// <summary>Intervals overlapping the closed range [start, end].</summary>
    public IReadOnlyList<(long Start, long End, T Value)> Overlapping(long start, long end)
    {
        var results = new List<(long, long, T)>();
        LastVisited = 0;

        void Search(Node? node)
        {
            if (node is null || node.MaxEnd < start) return; // nothing below reaches the query
            LastVisited++;
            Search(node.Left);
            if (node.Start <= end && node.End >= start) results.Add((node.Start, node.End, node.Value));
            if (node.Start <= end) Search(node.Right); // right subtree starts even later
        }

        Search(_root);
        return results;
    }

    public IReadOnlyList<(long Start, long End, T Value)> Containing(long point) => Overlapping(point, point);

    private static Node? Build((long Start, long End, T Value)[] sorted, int from, int to)
    {
        if (from >= to) return null;
        var mid = (from + to) / 2;
        var node = new Node(sorted[mid].Start, sorted[mid].End, sorted[mid].Value)
        {
            Left = Build(sorted, from, mid),
            Right = Build(sorted, mid + 1, to),
        };
        node.MaxEnd = Math.Max(node.End, Math.Max(node.Left?.MaxEnd ?? long.MinValue, node.Right?.MaxEnd ?? long.MinValue));
        return node;
    }
}
