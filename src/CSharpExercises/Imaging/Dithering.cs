namespace CSharpExercises.Imaging;

/// <summary>
/// Reducing an image to pure black and white. A plain threshold turns a smooth gradient into one hard edge. Error
/// diffusion (Floyd–Steinberg) pushes each pixel's rounding error onto its unprocessed neighbours, so the average
/// brightness of every area is preserved. Ordered dithering uses a fixed Bayer pattern instead: worse looking but
/// every pixel is independent, so it parallelises and doesn't shimmer in animations.
/// </summary>
public static class Dithering
{
    public static GrayImage Threshold(GrayImage image, byte threshold = 128) =>
        GrayImage.Create(image.Width, image.Height, (x, y) => image[x, y] >= threshold ? (byte)255 : (byte)0);

    public static GrayImage FloydSteinberg(GrayImage image)
    {
        var (w, h) = (image.Width, image.Height);
        var values = new double[w, h];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++) values[x, y] = image[x, y];

        var result = new GrayImage(w, h);
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var old = values[x, y];
                var chosen = old >= 128 ? 255 : 0;
                result[x, y] = (byte)chosen;
                var error = old - chosen;

                //        *   7/16
                // 3/16  5/16 1/16
                Spread(x + 1, y, 7 / 16.0);
                Spread(x - 1, y + 1, 3 / 16.0);
                Spread(x, y + 1, 5 / 16.0);
                Spread(x + 1, y + 1, 1 / 16.0);

                void Spread(int nx, int ny, double share)
                {
                    if (nx >= 0 && nx < w && ny < h) values[nx, ny] += error * share;
                }
            }
        return result;
    }

    private static readonly int[,] Bayer4 =
    {
        { 0, 8, 2, 10 },
        { 12, 4, 14, 6 },
        { 3, 11, 1, 9 },
        { 15, 7, 13, 5 },
    };

    public static GrayImage Ordered(GrayImage image) =>
        GrayImage.Create(image.Width, image.Height, (x, y) =>
        {
            var threshold = (Bayer4[y % 4, x % 4] + 0.5) * 16; // spread thresholds evenly over 0..255
            return image[x, y] >= threshold ? (byte)255 : (byte)0;
        });
}
