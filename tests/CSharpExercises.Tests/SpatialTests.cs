using CSharpExercises.Spatial;

namespace CSharpExercises.Tests;

public class SpatialTests
{
    private static (Point2 Point, int Value)[] RandomPoints(int count, int seed)
    {
        var random = new Random(seed);
        return Enumerable.Range(0, count).Select(i => (new Point2(random.NextDouble() * 1000, random.NextDouble() * 1000), i)).ToArray();
    }

    [Fact]
    public void QuadTree_RangeQueryMatchesBruteForce()
    {
        var points = RandomPoints(5000, 1);
        var tree = new QuadTree<int>(new Bounds(0, 0, 1000, 1000));
        foreach (var (p, v) in points) Assert.True(tree.Insert(p, v));

        var area = new Bounds(100, 200, 250, 300);
        var expected = points.Where(p => area.Contains(p.Point)).Select(p => p.Value).Order();

        Assert.Equal(expected, tree.Query(area).Select(i => i.Value).Order());
        Assert.Equal(5000, tree.Count);
        Assert.True(tree.NodeCount > 100);
        Assert.False(tree.Insert(new Point2(-5, 5), -1)); // outside the root bounds
    }

    [Fact]
    public void QuadTree_RadiusSearchAndDuplicates()
    {
        var tree = new QuadTree<string>(new Bounds(0, 0, 100, 100), capacity: 2, maxDepth: 4);
        for (var i = 0; i < 20; i++) tree.Insert(new Point2(50, 50), $"same{i}"); // depth limit stops endless splitting
        tree.Insert(new Point2(53, 54), "near");
        tree.Insert(new Point2(90, 90), "far");

        Assert.Equal(21, tree.Within(new Point2(50, 50), 5).Count());
        Assert.Contains(tree.Within(new Point2(50, 50), 5), i => i.Value == "near");
    }

    [Fact]
    public void KdTree_NearestMatchesBruteForce()
    {
        var points = RandomPoints(5000, 2);
        var tree = new KdTree<int>(points);
        var random = new Random(3);

        for (var i = 0; i < 100; i++)
        {
            var q = new Point2(random.NextDouble() * 1000, random.NextDouble() * 1000);
            var expected = points.MinBy(p => p.Point.DistanceSquaredTo(q));
            Assert.Equal(expected.Value, tree.Nearest(q)!.Value.Value);
        }
        Assert.True(tree.LastVisited < 100); // a handful of nodes, not 5,000
    }

    [Fact]
    public void KdTree_KNearestInOrder()
    {
        var points = RandomPoints(1000, 4);
        var tree = new KdTree<int>(points);
        var q = new Point2(500, 500);

        var expected = points.OrderBy(p => p.Point.DistanceSquaredTo(q)).Take(5).Select(p => p.Value);
        var actual = tree.KNearest(q, 5);

        Assert.Equal(expected, actual.Select(r => r.Value));
        Assert.True(actual.Zip(actual.Skip(1)).All(p => p.First.Distance <= p.Second.Distance));
        Assert.Null(new KdTree<int>([]).Nearest(q));
    }

    [Fact]
    public void GeoHash_EncodesAndDecodes()
    {
        Assert.Equal("u4pruydqq", GeoHash.Encode(57.64911, 10.40744)); // the example on Wikipedia
        Assert.Equal("ezs42", GeoHash.Encode(42.605, -5.603, 5));

        var (lat, lon, latErr, lonErr) = GeoHash.Decode("ezs42");
        Assert.InRange(42.605, lat - latErr, lat + latErr);
        Assert.InRange(-5.603, lon - lonErr, lon + lonErr);
        Assert.Throws<FormatException>(() => GeoHash.Decode("ai"));
    }

    [Fact]
    public void GeoHash_SharedPrefixMeansNearbyAndNeighboursCoverBorders()
    {
        var eiffel = GeoHash.Encode(48.8584, 2.2945, 6);
        var louvre = GeoHash.Encode(48.8606, 2.3376, 6);
        var sydney = GeoHash.Encode(-33.8568, 151.2153, 6);

        Assert.Equal(eiffel[..4], louvre[..4]);
        Assert.NotEqual(eiffel[0], sydney[0]);

        var neighbours = GeoHash.Neighbours(eiffel);
        Assert.Equal(8, neighbours.Distinct().Count());
        Assert.DoesNotContain(eiffel, neighbours);
        Assert.Equal(8, GeoHash.Neighbours(GeoHash.Encode(0, 179.99, 5)).Count); // wraps across the date line
    }

    [Fact]
    public void SpatialHash_FindsTheSamePairsWithFarFewerChecks()
    {
        var random = new Random(5);
        var circles = Enumerable.Range(0, 2000).Select(i => new Circle2(i, random.NextDouble() * 2000, random.NextDouble() * 2000, 5 + random.NextDouble() * 5)).ToArray();
        var grid = new SpatialHashGrid(cellSize: 20);
        foreach (var c in circles) grid.Insert(c);

        var pairs = grid.CollidingPairs();

        Assert.True(pairs.SetEquals(SpatialHashGrid.BruteForce(circles)));
        Assert.NotEmpty(pairs);
        Assert.True(grid.PairChecks < 2000 * 1999 / 2 / 100); // under 1% of the n² pairs
    }

    [Fact]
    public void IntervalTree_FindsOverlaps()
    {
        (long, long, string)[] meetings = [(9, 10, "standup"), (9, 12, "workshop"), (13, 14, "lunch talk"), (15, 17, "review"), (11, 11, "call")];
        var tree = new IntervalTree<string>(meetings);

        Assert.Equal(["workshop", "call"], tree.Overlapping(11, 12).Select(m => m.Value));
        Assert.Equal(["standup", "workshop"], tree.Containing(10).Select(m => m.Value));
        Assert.Empty(tree.Overlapping(18, 20));
    }

    [Fact]
    public void IntervalTree_MatchesBruteForceAndPrunes()
    {
        var random = new Random(6);
        var intervals = Enumerable.Range(0, 10_000).Select(i =>
        {
            var start = random.Next(1_000_000);
            return ((long)start, (long)start + random.Next(1, 100), i);
        }).ToArray();
        var tree = new IntervalTree<int>(intervals);

        var expected = intervals.Where(i => i.Item1 <= 500_100 && i.Item2 >= 500_000).Select(i => i.i).Order();

        Assert.Equal(expected, tree.Overlapping(500_000, 500_100).Select(r => r.Value).Order());
        Assert.True(tree.LastVisited < 200);
    }
}
