using CSharpExercises.DataProcessing;

namespace CSharpExercises.Tests;

public class DataProcessingTests
{
    private sealed record Customer(int Id, string Name);
    private sealed record Order(int CustomerId, string Item);
    private sealed record Sale(string Region, string Quarter, decimal Amount);

    private static readonly Customer[] Customers = [new(1, "Ada"), new(2, "Grace"), new(3, "Alan")];
    private static readonly Order[] Orders = [new(1, "book"), new(2, "pen"), new(1, "lamp"), new(4, "ghost")];

    [Fact]
    public void Joins_AllAlgorithmsAgree()
    {
        static string Describe(IEnumerable<(Customer Left, Order Right)> rows) =>
            string.Join(",", rows.Select(r => $"{r.Left.Name}:{r.Right.Item}").Order());

        var nested = Describe(Joins.NestedLoop(Customers, Orders, c => c.Id, o => o.CustomerId));
        var hash = Describe(Joins.Hash(Customers, Orders, c => c.Id, o => o.CustomerId));
        var merge = Describe(Joins.Merge(Customers.OrderBy(c => c.Id).ToList(), Orders.OrderBy(o => o.CustomerId).ToList(), c => c.Id, o => o.CustomerId));

        Assert.Equal("Ada:book,Ada:lamp,Grace:pen", nested);
        Assert.Equal(nested, hash);
        Assert.Equal(nested, merge);
    }

    [Fact]
    public void Joins_MergeHandlesDuplicateKeysOnBothSides()
    {
        int[] left = [1, 2, 2, 3];
        int[] right = [2, 2, 2, 3, 4];

        Assert.Equal(7, Joins.Merge(left, right, x => x, x => x).Count()); // 2×3 for key 2, plus 3
        Assert.Equal(7, Joins.Hash(left, right, x => x, x => x).Count());
    }

    [Fact]
    public void Joins_LeftOuterKeepsUnmatchedRows()
    {
        var rows = Joins.LeftOuter(Customers, Orders, c => c.Id, o => o.CustomerId).ToList();

        Assert.Equal(4, rows.Count);
        Assert.Contains(rows, r => r.Left.Name == "Alan" && r.Right is null);
    }

    [Fact]
    public void ExternalSort_SortsInRunsAndMerges()
    {
        var random = new Random(1);
        var lines = Enumerable.Range(0, 5_000).Select(_ => random.Next(1_000_000).ToString("D7")).ToList();
        var output = new StringWriter();

        var stats = ExternalSort.Sort(new StringReader(string.Join('\n', lines)), output, linesPerChunk: 700);

        Assert.Equal(8, stats.Runs);
        Assert.Equal(5_000, stats.Lines);
        Assert.Equal(lines.Order(StringComparer.Ordinal), output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
    }

    [Fact]
    public void ExternalSort_KWayMergeIsLazyAndStable()
    {
        IEnumerable<(int Key, string Source)> Run(string name, params int[] keys) => keys.Select(k => (k, name));

        var merged = ExternalSort.KWayMerge([Run("a", 1, 4, 9), Run("b", 2, 4), Run("c", 3)],
            Comparer<(int Key, string Source)>.Create((x, y) => x.Key.CompareTo(y.Key))).ToList();

        Assert.Equal([1, 2, 3, 4, 4, 9], merged.Select(m => m.Key));
        Assert.Equal("a", merged[3].Source); // equal keys keep source order
        // Lazy: merging with an endless sequence works as long as we only take a few.
        Assert.Equal([1, 2, 2, 3, 3], ExternalSort.KWayMerge([Enumerable.Range(1, int.MaxValue), [2, 3]]).Take(5));
    }

    private static readonly Sale[] Sales =
    [
        new("North", "Q1", 100), new("North", "Q2", 150), new("South", "Q1", 80), new("North", "Q1", 20), new("East", "Q2", 0),
    ];

    [Fact]
    public void Pivot_AggregatesAndTotals()
    {
        var pivot = new PivotTable<Sale>(Sales, s => s.Region, s => s.Quarter, s => s.Amount);

        Assert.Equal(["East", "North", "South"], pivot.RowKeys);
        Assert.Equal(120m, pivot["North", "Q1"]);
        Assert.Null(pivot["South", "Q2"]);           // no sales recorded
        Assert.Equal(0m, pivot["East", "Q2"]);        // sales of zero
        Assert.Equal(270m, pivot.RowTotal("North"));
        Assert.Equal(350m, pivot.GrandTotal);
        Assert.Equal(
            """
                  |  Q1 |  Q2 | Total
            East  |   - |   0 |     0
            North | 120 | 150 |   270
            South |  80 |   - |    80
            Total | 200 | 150 |   350

            """.ReplaceLineEndings(), pivot.Render());
    }

    [Fact]
    public void Pivot_CustomAggregate()
    {
        var counts = new PivotTable<Sale>(Sales, s => s.Region, s => s.Quarter, s => s.Amount, v => v.Count());
        Assert.Equal(2m, counts["North", "Q1"]);
    }

    private static readonly DateTimeOffset T0 = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TimeSeries_BucketsAndFillsGaps()
    {
        (DateTimeOffset, double)[] points = [(T0.AddSeconds(5), 10), (T0.AddSeconds(50), 20), (T0.AddMinutes(3).AddSeconds(1), 40)];
        var minute = TimeSpan.FromMinutes(1);

        Assert.Equal([15.0, null, null, 40], TimeSeries.Resample(points, minute, v => v.Average()).Select(r => r.Value));
        Assert.Equal([15.0, 0, 0, 40], TimeSeries.Resample(points, minute, v => v.Average(), GapFill.Zero).Select(r => r.Value));
        Assert.Equal([15.0, 15, 15, 40], TimeSeries.Resample(points, minute, v => v.Average(), GapFill.ForwardFill).Select(r => r.Value));
        Assert.Equal([15.0, 23.333333333333332, 31.666666666666664, 40], TimeSeries.Resample(points, minute, v => v.Average(), GapFill.Interpolate).Select(r => r.Value));
        Assert.Equal(T0.AddMinutes(3), TimeSeries.Floor(T0.AddMinutes(3).AddSeconds(59), minute));
    }

    [Fact]
    public void TimeSeries_DifferencesHandleCounterResets()
    {
        (DateTimeOffset, double?)[] counter = [(T0, 100), (T0.AddMinutes(1), 130), (T0.AddMinutes(2), 5), (T0.AddMinutes(3), 25)];

        Assert.Equal([30.0, 5, 20], TimeSeries.Differences(counter).Select(d => d.Delta));
    }

    private sealed record Score(string Team, string Player, int Points);

    private static readonly Score[] Scores =
    [
        new("red", "ann", 30), new("red", "bob", 50), new("red", "cid", 30), new("red", "dee", 10),
        new("blue", "eve", 40), new("blue", "fay", 20),
    ];

    [Fact]
    public void Window_RowNumberAndRanks()
    {
        var ranks = WindowFunctions.Rank(Scores, s => s.Team, s => s.Points, descending: true).ToDictionary(r => r.Row.Player);

        Assert.Equal((1, 1), (ranks["bob"].Rank, ranks["bob"].DenseRank));
        Assert.Equal((2, 2), (ranks["ann"].Rank, ranks["ann"].DenseRank));
        Assert.Equal((2, 2), (ranks["cid"].Rank, ranks["cid"].DenseRank));
        Assert.Equal((4, 3), (ranks["dee"].Rank, ranks["dee"].DenseRank));
        Assert.Equal(1, ranks["eve"].Rank); // each team ranks separately

        var numbers = WindowFunctions.RowNumber(Scores, s => s.Team, s => s.Points).Where(r => r.Row.Team == "blue");
        Assert.Equal([("fay", 1), ("eve", 2)], numbers.Select(n => (n.Row.Player, n.RowNumber)));
    }

    [Fact]
    public void Window_LagRunningTotalAndMovingAverage()
    {
        var days = Enumerable.Range(1, 5).Select(d => (Day: d, Store: "A", Revenue: (decimal)(d * 10))).ToArray();

        Assert.Equal([0m, 10, 20, 30, 40], WindowFunctions.Lag(days, d => d.Store, d => d.Day, d => d.Revenue).Select(l => l.Previous));
        Assert.Equal([10m, 30, 60, 100, 150], WindowFunctions.RunningTotal(days, d => d.Store, d => d.Day, d => d.Revenue).Select(r => r.RunningTotal));
        Assert.Equal([10m, 15, 20, 30, 40], WindowFunctions.MovingAverage(days, d => d.Store, d => d.Day, d => d.Revenue, preceding: 2).Select(m => m.Average));
    }

    [Fact]
    public void SnapshotDiff_FindsAddedRemovedAndModified()
    {
        Dictionary<string, string> Row(string id, string name, string city, string updated) => new() { ["id"] = id, ["name"] = name, ["city"] = city, ["updated"] = updated };

        var yesterday = new[] { Row("1", "Ada", "London", "mon"), Row("2", "Grace", "NYC", "mon"), Row("3", "Alan", "Wilmslow", "mon") };
        var today = new[] { Row("1", "Ada", "Paris", "tue"), Row("3", "Alan", "Wilmslow", "tue"), Row("4", "Linus", "Helsinki", "tue") };

        var changes = SnapshotDiff.Compare(yesterday, today, "id", ignoreFields: ["updated"]);

        Assert.Equal([("1", ChangeKind.Modified), ("2", ChangeKind.Removed), ("4", ChangeKind.Added)], changes.Select(c => (c.Key, c.Kind)));
        Assert.Equal(new FieldChange("city", "London", "Paris"), changes[0].Fields.Single());
        Assert.Throws<ArgumentException>(() => SnapshotDiff.Compare([Row("1", "a", "b", "c"), Row("1", "x", "y", "z")], [], "id"));
    }
}
