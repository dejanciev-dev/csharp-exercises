using CSharpExercises.Geometry;

namespace CSharpExercises.Tests;

public class GeometryTests
{
    [Fact]
    public void Orientation_TellsLeftFromRightTurns()
    {
        Assert.True(Vec.Orientation(new(0, 0), new(1, 0), new(1, 1)) > 0);
        Assert.True(Vec.Orientation(new(0, 0), new(1, 0), new(1, -1)) < 0);
        Assert.Equal(0, Vec.Orientation(new(0, 0), new(1, 1), new(2, 2)));
    }

    [Theory]
    [InlineData(0, 0, 4, 4, 0, 4, 4, 0, true)]   // crossing
    [InlineData(0, 0, 2, 2, 3, 3, 5, 5, false)]  // collinear, apart
    [InlineData(0, 0, 2, 2, 2, 2, 5, 0, true)]   // touching at an endpoint
    [InlineData(0, 0, 4, 0, 0, 1, 4, 1, false)]  // parallel
    [InlineData(0, 0, 4, 0, 2, 0, 6, 0, true)]   // collinear, overlapping
    public void Segments_Intersect(double ax, double ay, double bx, double by, double cx, double cy, double dx, double dy, bool expected)
    {
        Assert.Equal(expected, Segments.Intersect(new(ax, ay), new(bx, by), new(cx, cy), new(dx, dy)));
    }

    [Fact]
    public void Segments_LineIntersectionAndDistance()
    {
        Assert.Equal(new Vec(2, 2), Segments.LineIntersection(new(0, 0), new(4, 4), new(0, 4), new(4, 0)));
        Assert.Null(Segments.LineIntersection(new(0, 0), new(1, 0), new(0, 1), new(1, 1)));

        Assert.Equal(1, Segments.DistanceToSegment(new(2, 1), new(0, 0), new(4, 0)), 9);
        Assert.Equal(5, Segments.DistanceToSegment(new(7, 4), new(0, 0), new(4, 0)), 9); // nearest is the endpoint
    }

    [Fact]
    public void ConvexHull_DropsInteriorAndCollinearPoints()
    {
        Vec[] points = [new(0, 0), new(2, 0), new(4, 0), new(4, 4), new(0, 4), new(2, 2), new(1, 3), new(0, 0)];

        var hull = ConvexHull.Compute(points);

        Assert.Equal([new Vec(0, 0), new Vec(4, 0), new Vec(4, 4), new Vec(0, 4)], hull);
        Assert.Equal(Math.Sqrt(32), ConvexHull.Diameter(points), 9);
    }

    [Fact]
    public void Polygon_AreaPerimeterAndCentroid()
    {
        var square = new Polygon([new(0, 0), new(4, 0), new(4, 4), new(0, 4)]);

        Assert.Equal(16, square.Area);
        Assert.True(square.IsCounterClockwise);
        Assert.Equal(16, square.Perimeter);
        Assert.Equal(new Vec(2, 2), square.Centroid);

        var clockwise = new Polygon(square.Vertices.Reverse());
        Assert.Equal(-16, clockwise.SignedArea);
    }

    [Fact]
    public void Polygon_ContainsWorksForConcaveShapes()
    {
        // An L shape: the notch at the top right is outside.
        var l = new Polygon([new(0, 0), new(4, 0), new(4, 2), new(2, 2), new(2, 4), new(0, 4)]);

        Assert.True(l.Contains(new(1, 1)));
        Assert.True(l.Contains(new(1, 3)));
        Assert.False(l.Contains(new(3, 3)));
        Assert.False(l.Contains(new(5, 1)));
        Assert.False(l.IsConvex);
        Assert.Equal(12, l.Area);
    }

    [Fact]
    public void ClosestPair_MatchesBruteForce()
    {
        var random = new Random(42);
        var points = Enumerable.Range(0, 500).Select(_ => new Vec(random.NextDouble() * 1000, random.NextDouble() * 1000)).ToArray();

        Assert.Equal(ClosestPair.BruteForce(points).Distance, ClosestPair.Find(points).Distance, 12);
    }

    [Fact]
    public void Haversine_LondonToParis()
    {
        var london = new GeoPoint(51.5074, -0.1278);
        var paris = new GeoPoint(48.8566, 2.3522);

        Assert.InRange(GeoDistance.HaversineKm(london, paris), 343, 345);
        Assert.InRange(GeoDistance.InitialBearing(london, paris), 148, 149);
        Assert.Equal(0, GeoDistance.HaversineKm(paris, paris));
    }

    [Fact]
    public void Haversine_BoundingBoxContainsTheCircle()
    {
        var center = new GeoPoint(60, 10);
        var (min, max) = GeoDistance.BoundingBox(center, 100);

        Assert.InRange(GeoDistance.HaversineKm(center, new GeoPoint(max.Latitude, 10)), 99.9, 100.1);
        Assert.InRange(GeoDistance.HaversineKm(center, new GeoPoint(60, max.Longitude)), 99, 100.01); // a parallel is not a great circle
        Assert.True(max.Longitude - min.Longitude > max.Latitude - min.Latitude); // longitude lines converge
    }

    [Fact]
    public void RectangleUnion_CountsOverlapsOnce()
    {
        Box[] boxes = [new(0, 0, 4, 4), new(2, 2, 6, 6), new(1, 1, 3, 3)];

        Assert.Equal(28, RectangleUnion.Area(boxes)); // 16 + 16 - 4, the small one is already covered
        Assert.Equal(new Box(2, 2, 4, 4), boxes[0].Intersect(boxes[1]));
        Assert.Null(new Box(0, 0, 1, 1).Intersect(new Box(1, 0, 2, 1))); // touching edges don't overlap
    }
}
