namespace CSharpExercises.Imaging;

/// <summary>
/// Resizing. Each output pixel maps back to a fractional position in the source. Nearest neighbour takes the closest
/// pixel (blocky, but right for pixel art); bilinear blends the four surrounding pixels by distance (smooth).
/// Pixel centres sit at +0.5, and forgetting that shifts the whole image by half a pixel.
/// </summary>
public static class Resampling
{
    public static GrayImage NearestNeighbour(GrayImage source, int width, int height) =>
        GrayImage.Create(width, height, (x, y) =>
        {
            var sx = (int)((x + 0.5) * source.Width / width);
            var sy = (int)((y + 0.5) * source.Height / height);
            return source[Math.Min(sx, source.Width - 1), Math.Min(sy, source.Height - 1)];
        });

    public static GrayImage Bilinear(GrayImage source, int width, int height) =>
        GrayImage.Create(width, height, (x, y) =>
        {
            var fx = (x + 0.5) * source.Width / width - 0.5;
            var fy = (y + 0.5) * source.Height / height - 0.5;
            int x0 = (int)Math.Floor(fx), y0 = (int)Math.Floor(fy);
            double tx = fx - x0, ty = fy - y0;

            var top = source.Clamped(x0, y0) * (1 - tx) + source.Clamped(x0 + 1, y0) * tx;
            var bottom = source.Clamped(x0, y0 + 1) * (1 - tx) + source.Clamped(x0 + 1, y0 + 1) * tx;
            return GrayImage.ToByte(top * (1 - ty) + bottom * ty);
        });

    /// <summary>Thumbnail size that fits a box while keeping the aspect ratio.</summary>
    public static (int Width, int Height) FitWithin(int width, int height, int maxWidth, int maxHeight)
    {
        var scale = Math.Min(1, Math.Min((double)maxWidth / width, (double)maxHeight / height));
        return (Math.Max(1, (int)Math.Round(width * scale)), Math.Max(1, (int)Math.Round(height * scale)));
    }
}
