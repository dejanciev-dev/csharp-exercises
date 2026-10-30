namespace CSharpExercises.Geometry;

public readonly record struct GeoPoint(double Latitude, double Longitude);

/// <summary>
/// Distances on the Earth's surface. Treating latitude and longitude as flat x/y is badly wrong away from the
/// equator; the haversine formula measures along a great circle on a sphere instead.
/// </summary>
public static class GeoDistance
{
    public const double EarthRadiusKm = 6371.0088;

    public static double HaversineKm(GeoPoint a, GeoPoint b)
    {
        var lat1 = ToRadians(a.Latitude);
        var lat2 = ToRadians(b.Latitude);
        var dLat = lat2 - lat1;
        var dLon = ToRadians(b.Longitude - a.Longitude);

        var h = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2), 2);
        return 2 * EarthRadiusKm * Math.Asin(Math.Min(1, Math.Sqrt(h)));
    }

    /// <summary>The compass direction to head in from <paramref name="from"/>, in degrees clockwise from north.</summary>
    public static double InitialBearing(GeoPoint from, GeoPoint to)
    {
        var lat1 = ToRadians(from.Latitude);
        var lat2 = ToRadians(to.Latitude);
        var dLon = ToRadians(to.Longitude - from.Longitude);

        var y = Math.Sin(dLon) * Math.Cos(lat2);
        var x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);
        return (ToDegrees(Math.Atan2(y, x)) + 360) % 360;
    }

    /// <summary>A quick pre-filter for "within r km": a lat/long box that is cheap to query with an index.</summary>
    public static (GeoPoint Min, GeoPoint Max) BoundingBox(GeoPoint center, double radiusKm)
    {
        var dLat = ToDegrees(radiusKm / EarthRadiusKm);
        var dLon = ToDegrees(radiusKm / (EarthRadiusKm * Math.Cos(ToRadians(center.Latitude))));
        return (new GeoPoint(center.Latitude - dLat, center.Longitude - dLon),
                new GeoPoint(center.Latitude + dLat, center.Longitude + dLon));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
    private static double ToDegrees(double radians) => radians * 180 / Math.PI;
}
