using System.Globalization;

namespace CSharpExercises.Imaging;

/// <summary>
/// Colour conversions that come up in UI work: hex strings, RGB ↔ HSL (adjust lightness without changing the hue),
/// mixing, and the WCAG contrast ratio that decides whether text is readable on a background. Contrast uses relative
/// luminance, which first undoes the sRGB gamma curve, because perceived brightness isn't linear in the byte values.
/// </summary>
public readonly record struct Rgb(byte R, byte G, byte B)
{
    public static Rgb Parse(string hex)
    {
        var h = hex.TrimStart('#');
        if (h.Length == 3) h = string.Concat(h.Select(c => $"{c}{c}")); // #abc → #aabbcc
        if (h.Length != 6 || !int.TryParse(h, NumberStyles.HexNumber, null, out var value))
            throw new FormatException($"'{hex}' is not a hex colour.");
        return new Rgb((byte)(value >> 16), (byte)(value >> 8), (byte)value);
    }

    public string ToHex() => $"#{R:x2}{G:x2}{B:x2}";

    /// <summary>Hue in degrees, saturation and lightness from 0 to 1.</summary>
    public (double H, double S, double L) ToHsl()
    {
        double r = R / 255.0, g = G / 255.0, b = B / 255.0;
        double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
        var l = (max + min) / 2;
        if (max == min) return (0, 0, l); // grey: no hue

        var d = max - min;
        var s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
        var h = max == r ? (g - b) / d + (g < b ? 6 : 0) : max == g ? (b - r) / d + 2 : (r - g) / d + 4;
        return (h * 60, s, l);
    }

    public static Rgb FromHsl(double h, double s, double l)
    {
        if (s == 0) return Gray(l);
        var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        var p = 2 * l - q;
        h /= 360;
        return new Rgb(Channel(p, q, h + 1.0 / 3), Channel(p, q, h), Channel(p, q, h - 1.0 / 3));

        static byte Channel(double p, double q, double t)
        {
            t = (t % 1 + 1) % 1;
            var v = t < 1.0 / 6 ? p + (q - p) * 6 * t : t < 0.5 ? q : t < 2.0 / 3 ? p + (q - p) * (2.0 / 3 - t) * 6 : p;
            return GrayImage.ToByte(v * 255);
        }
    }

    public Rgb Lighten(double amount)
    {
        var (h, s, l) = ToHsl();
        return FromHsl(h, s, Math.Clamp(l + amount, 0, 1));
    }

    public Rgb Mix(Rgb other, double weight) => new(
        GrayImage.ToByte(R + (other.R - R) * weight),
        GrayImage.ToByte(G + (other.G - G) * weight),
        GrayImage.ToByte(B + (other.B - B) * weight));

    /// <summary>Luma: the weighted sum that turns colour into perceived grey (ITU-R BT.601).</summary>
    public byte ToGray() => GrayImage.ToByte(0.299 * R + 0.587 * G + 0.114 * B);

    public double RelativeLuminance()
    {
        static double Linear(byte c)
        {
            var v = c / 255.0;
            return v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Linear(R) + 0.7152 * Linear(G) + 0.0722 * Linear(B);
    }

    /// <summary>From 1 (no contrast) to 21 (black on white). WCAG AA asks for 4.5 for body text.</summary>
    public double ContrastWith(Rgb other)
    {
        var (a, b) = (RelativeLuminance(), other.RelativeLuminance());
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    /// <summary>Black or white, whichever reads better on this background.</summary>
    public Rgb ReadableTextColor() => ContrastWith(Black) >= ContrastWith(White) ? Black : White;

    public static Rgb Black => new(0, 0, 0);
    public static Rgb White => new(255, 255, 255);
    private static Rgb Gray(double l) => new(GrayImage.ToByte(l * 255), GrayImage.ToByte(l * 255), GrayImage.ToByte(l * 255));
}
