namespace CSharpExercises.Spatial;

/// <summary>
/// A 2-d tree: split the points at the median alternately by x and by y. Nearest-neighbour search goes down the side
/// the query is on first, then only checks the other side if the splitting line is closer than the best match so far,
/// usually O(log n). The k-nearest version keeps the k best in a max-heap.
/// </summary>
public sealed class KdTree<T>
{
    private sealed record Node(Point2 Point, T Value, Node? Left, Node? Right, bool SplitByX);

    private readonly Node? _root;

    public KdTree(IEnumerable<(Point2 Point, T Value)> items)
    {
        _root = Build([.. items], depth: 0);
    }

    public int LastVisited { get; private set; }

    public (Point2 Point, T Value, double Distance)? Nearest(Point2 query) =>
        KNearest(query, 1) is [var best] ? best : null;

    public IReadOnlyList<(Point2 Point, T Value, double Distance)> KNearest(Point2 query, int k)
    {
        var heap = new PriorityQueue<Node, double>(Comparer<double>.Create((a, b) => b.CompareTo(a))); // farthest on top
        LastVisited = 0;

        void Search(Node? node)
        {
            if (node is null) return;
            LastVisited++;

            var d = node.Point.DistanceSquaredTo(query);
            if (heap.Count < k) heap.Enqueue(node, d);
            else if (heap.TryPeek(out _, out var worst) && d < worst) heap.DequeueEnqueue(node, d);

            var diff = node.SplitByX ? query.X - node.Point.X : query.Y - node.Point.Y;
            var (near, far) = diff < 0 ? (node.Left, node.Right) : (node.Right, node.Left);
            Search(near);

            // The far side can only help if the splitting line is closer than the current k-th best.
            if (heap.Count < k || (heap.TryPeek(out _, out var kth) && diff * diff < kth)) Search(far);
        }

        Search(_root);
        var result = new List<(Point2, T, double)>();
        while (heap.TryDequeue(out var node, out var d)) result.Add((node.Point, node.Value, Math.Sqrt(d)));
        result.Reverse();
        return result;
    }

    private static Node? Build(Span<(Point2 Point, T Value)> items, int depth)
    {
        if (items.IsEmpty) return null;
        var byX = depth % 2 == 0;
        items.Sort((a, b) => byX ? a.Point.X.CompareTo(b.Point.X) : a.Point.Y.CompareTo(b.Point.Y));
        var mid = items.Length / 2;
        return new Node(items[mid].Point, items[mid].Value, Build(items[..mid], depth + 1), Build(items[(mid + 1)..], depth + 1), byX);
    }
}
