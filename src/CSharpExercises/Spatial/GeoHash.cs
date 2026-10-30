namespace CSharpExercises.Spatial;

/// <summary>
/// Geohash turns a latitude/longitude into a short string by repeatedly halving the world, alternating longitude and
/// latitude, and writing five bits per base-32 character. Nearby places usually share a prefix, so a plain string
/// index answers "what's near here" with a prefix search. Edge cases: two points on either side of a cell border share
/// nothing, which is why searches also look at the eight neighbouring cells.
/// </summary>
public static class GeoHash
{
    private const string Base32 = "0123456789bcdefghjkmnpqrstuvwxyz";

    public static string Encode(double latitude, double longitude, int precision = 9)
    {
        double latMin = -90, latMax = 90, lonMin = -180, lonMax = 180;
        var chars = new char[precision];
        var evenBit = true; // longitude first
        for (var c = 0; c < precision; c++)
        {
            var value = 0;
            for (var bit = 0; bit < 5; bit++)
            {
                if (evenBit) Halve(longitude, ref lonMin, ref lonMax, ref value);
                else Halve(latitude, ref latMin, ref latMax, ref value);
                evenBit = !evenBit;
            }
            chars[c] = Base32[value];
        }
        return new string(chars);
    }

    /// <summary>The cell a hash stands for, as its centre and half-size: the hash's built-in error bars.</summary>
    public static (double Latitude, double Longitude, double LatError, double LonError) Decode(string hash)
    {
        double latMin = -90, latMax = 90, lonMin = -180, lonMax = 180;
        var evenBit = true;
        foreach (var ch in hash)
        {
            var value = Base32.IndexOf(ch);
            if (value < 0) throw new FormatException($"'{ch}' is not a geohash character.");
            for (var bit = 4; bit >= 0; bit--)
            {
                var high = (value >> bit & 1) == 1;
                if (evenBit) Narrow(high, ref lonMin, ref lonMax);
                else Narrow(high, ref latMin, ref latMax);
                evenBit = !evenBit;
            }
        }
        return ((latMin + latMax) / 2, (lonMin + lonMax) / 2, (latMax - latMin) / 2, (lonMax - lonMin) / 2);
    }

    /// <summary>The eight surrounding cells, found by stepping one cell size from the centre and re-encoding.</summary>
    public static IReadOnlyList<string> Neighbours(string hash)
    {
        var (lat, lon, latErr, lonErr) = Decode(hash);
        var result = new List<string>();
        for (var dy = -1; dy <= 1; dy++)
            for (var dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                var nLat = lat + dy * 2 * latErr;
                var nLon = lon + dx * 2 * lonErr;
                if (nLat is > 90 or < -90) continue;
                nLon = (nLon + 540) % 360 - 180; // wrap around the antimeridian
                result.Add(Encode(nLat, nLon, hash.Length));
            }
        return result;
    }

    private static void Halve(double value, ref double min, ref double max, ref int bits)
    {
        var mid = (min + max) / 2;
        bits <<= 1;
        if (value >= mid) { bits |= 1; min = mid; }
        else max = mid;
    }

    private static void Narrow(bool high, ref double min, ref double max)
    {
        var mid = (min + max) / 2;
        if (high) min = mid;
        else max = mid;
    }
}
