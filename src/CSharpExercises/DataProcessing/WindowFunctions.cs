namespace CSharpExercises.DataProcessing;

/// <summary>
/// SQL window functions in LINQ: unlike GROUP BY, each row keeps its identity and gains a value computed over its
/// partition (ROW_NUMBER, RANK, DENSE_RANK, LAG, running totals, moving averages). Each function partitions, orders
/// within the partition, then walks the rows once.
/// </summary>
public static class WindowFunctions
{
    public static IEnumerable<(T Row, int RowNumber)> RowNumber<T, TPart, TOrder>(
        IEnumerable<T> rows, Func<T, TPart> partition, Func<T, TOrder> order, bool descending = false) =>
        Partitioned(rows, partition, order, descending).SelectMany(p => p.Select((row, i) => (row, i + 1)));

    /// <summary>RANK leaves gaps after ties (1, 2, 2, 4); DENSE_RANK doesn't (1, 2, 2, 3).</summary>
    public static IEnumerable<(T Row, int Rank, int DenseRank)> Rank<T, TPart, TOrder>(
        IEnumerable<T> rows, Func<T, TPart> partition, Func<T, TOrder> order, bool descending = false)
    {
        var comparer = EqualityComparer<TOrder>.Default;
        foreach (var part in Partitioned(rows, partition, order, descending))
        {
            int rank = 0, dense = 0;
            for (var i = 0; i < part.Count; i++)
            {
                if (i == 0 || !comparer.Equals(order(part[i]), order(part[i - 1])))
                {
                    rank = i + 1;
                    dense++;
                }
                yield return (part[i], rank, dense);
            }
        }
    }

    /// <summary>The value from <paramref name="offset"/> rows earlier in the partition, or default at the start.</summary>
    public static IEnumerable<(T Row, TValue? Previous)> Lag<T, TPart, TOrder, TValue>(
        IEnumerable<T> rows, Func<T, TPart> partition, Func<T, TOrder> order, Func<T, TValue> value, int offset = 1) =>
        Partitioned(rows, partition, order, false).SelectMany(p => p.Select((row, i) => (row, i >= offset ? value(p[i - offset]) : default)));

    public static IEnumerable<(T Row, decimal RunningTotal)> RunningTotal<T, TPart, TOrder>(
        IEnumerable<T> rows, Func<T, TPart> partition, Func<T, TOrder> order, Func<T, decimal> value)
    {
        foreach (var part in Partitioned(rows, partition, order, false))
        {
            decimal total = 0;
            foreach (var row in part) yield return (row, total += value(row));
        }
    }

    /// <summary>Average over the current row and the <paramref name="preceding"/> rows before it ("ROWS BETWEEN n PRECEDING AND CURRENT ROW").</summary>
    public static IEnumerable<(T Row, decimal Average)> MovingAverage<T, TPart, TOrder>(
        IEnumerable<T> rows, Func<T, TPart> partition, Func<T, TOrder> order, Func<T, decimal> value, int preceding) =>
        Partitioned(rows, partition, order, false).SelectMany(p => p.Select((row, i) =>
            (row, p.Skip(Math.Max(0, i - preceding)).Take(Math.Min(i, preceding) + 1).Average(value))));

    private static IEnumerable<List<T>> Partitioned<T, TPart, TOrder>(IEnumerable<T> rows, Func<T, TPart> partition, Func<T, TOrder> order, bool descending) =>
        rows.GroupBy(partition).Select(g => (descending ? g.OrderByDescending(order) : g.OrderBy(order)).ToList());
}
