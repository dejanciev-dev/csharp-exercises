namespace CSharpExercises.Statistics;

/// <summary>
/// The median of a growing stream in O(log n) per value: a max-heap holds the lower half and a min-heap the upper
/// half, kept within one element of each other, so the median is always at the top of one or both.
/// </summary>
public sealed class StreamingMedian
{
    private readonly PriorityQueue<double, double> _lower = new(Comparer<double>.Create((a, b) => b.CompareTo(a)));
    private readonly PriorityQueue<double, double> _upper = new();

    public int Count => _lower.Count + _upper.Count;

    public void Add(double value)
    {
        if (_lower.Count == 0 || value <= _lower.Peek()) _lower.Enqueue(value, value);
        else _upper.Enqueue(value, value);

        // Rebalance so the lower half has the same number of elements or one more.
        if (_lower.Count > _upper.Count + 1) Move(_lower, _upper);
        else if (_upper.Count > _lower.Count) Move(_upper, _lower);
    }

    public double Median
    {
        get
        {
            if (Count == 0) throw new InvalidOperationException("No values yet.");
            return _lower.Count > _upper.Count ? _lower.Peek() : (_lower.Peek() + _upper.Peek()) / 2;
        }
    }

    private static void Move(PriorityQueue<double, double> from, PriorityQueue<double, double> to)
    {
        var value = from.Dequeue();
        to.Enqueue(value, value);
    }
}
