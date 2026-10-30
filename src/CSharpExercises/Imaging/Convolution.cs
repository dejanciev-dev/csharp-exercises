namespace CSharpExercises.Imaging;

/// <summary>
/// Image filters as convolution: every output pixel is a weighted sum of the input pixels around it, with the weights
/// given by a small kernel. One routine gives blur (average the neighbourhood), sharpen (boost the centre against
/// it) and edge detection (Sobel: the brightness gradient in x and y).
/// </summary>
public static class Convolution
{
    public static readonly double[,] BoxBlur = Normalise(new double[,] { { 1, 1, 1 }, { 1, 1, 1 }, { 1, 1, 1 } });
    public static readonly double[,] GaussianBlur = Normalise(new double[,] { { 1, 2, 1 }, { 2, 4, 2 }, { 1, 2, 1 } });
    public static readonly double[,] Sharpen = { { 0, -1, 0 }, { -1, 5, -1 }, { 0, -1, 0 } };
    public static readonly double[,] SobelX = { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
    public static readonly double[,] SobelY = { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };

    public static GrayImage Apply(GrayImage image, double[,] kernel) =>
        GrayImage.Create(image.Width, image.Height, (x, y) => GrayImage.ToByte(Sum(image, kernel, x, y)));

    /// <summary>Gradient magnitude √(gx² + gy²): bright where brightness changes quickly, i.e. at edges.</summary>
    public static GrayImage SobelEdges(GrayImage image) =>
        GrayImage.Create(image.Width, image.Height, (x, y) =>
        {
            var gx = Sum(image, SobelX, x, y);
            var gy = Sum(image, SobelY, x, y);
            return GrayImage.ToByte(Math.Sqrt(gx * gx + gy * gy));
        });

    /// <summary>
    /// A large box blur in O(1) per pixel regardless of radius, via a summed-area table: the sum over any rectangle is
    /// four lookups. Running it three times approximates a Gaussian, a trick used by fast blur implementations.
    /// </summary>
    public static GrayImage FastBoxBlur(GrayImage image, int radius)
    {
        var (w, h) = (image.Width, image.Height);
        var sums = new long[w + 1, h + 1];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                sums[x + 1, y + 1] = image[x, y] + sums[x, y + 1] + sums[x + 1, y] - sums[x, y];

        return GrayImage.Create(w, h, (x, y) =>
        {
            int x0 = Math.Max(0, x - radius), y0 = Math.Max(0, y - radius);
            int x1 = Math.Min(w, x + radius + 1), y1 = Math.Min(h, y + radius + 1);
            var total = sums[x1, y1] - sums[x0, y1] - sums[x1, y0] + sums[x0, y0];
            return GrayImage.ToByte((double)total / ((x1 - x0) * (y1 - y0)));
        });
    }

    private static double Sum(GrayImage image, double[,] kernel, int x, int y)
    {
        var half = kernel.GetLength(0) / 2;
        var sum = 0.0;
        for (var ky = 0; ky < kernel.GetLength(0); ky++)
            for (var kx = 0; kx < kernel.GetLength(1); kx++)
                sum += kernel[ky, kx] * image.Clamped(x + kx - half, y + ky - half);
        return sum;
    }

    private static double[,] Normalise(double[,] kernel)
    {
        var total = kernel.Cast<double>().Sum();
        var result = (double[,])kernel.Clone();
        for (var i = 0; i < result.GetLength(0); i++)
            for (var j = 0; j < result.GetLength(1); j++) result[i, j] /= total;
        return result;
    }
}
