namespace CSharpExercises.Imaging;

/// <summary>
/// An 8-bit greyscale image: a width × height grid of brightness values from 0 (black) to 255 (white), stored row by
/// row. Reads outside the image clamp to the nearest edge pixel, the usual way filters handle borders.
/// </summary>
public sealed class GrayImage
{
    private readonly byte[] _pixels;

    public GrayImage(int width, int height, byte[]? pixels = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        if (pixels is not null && pixels.Length != width * height) throw new ArgumentException("Wrong number of pixels.", nameof(pixels));
        (Width, Height) = (width, height);
        _pixels = pixels is null ? new byte[width * height] : [.. pixels];
    }

    public static GrayImage FromRows(params byte[][] rows) => new(rows[0].Length, rows.Length, rows.SelectMany(r => r).ToArray());

    public static GrayImage Create(int width, int height, Func<int, int, byte> pixel)
    {
        var image = new GrayImage(width, height);
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++) image[x, y] = pixel(x, y);
        return image;
    }

    public int Width { get; }
    public int Height { get; }

    public byte this[int x, int y]
    {
        get => _pixels[y * Width + x];
        set => _pixels[y * Width + x] = value;
    }

    public byte Clamped(int x, int y) => this[Math.Clamp(x, 0, Width - 1), Math.Clamp(y, 0, Height - 1)];

    public ReadOnlySpan<byte> Pixels => _pixels;

    public double Mean => _pixels.Average(p => (double)p);

    public static byte ToByte(double value) => (byte)Math.Clamp(Math.Round(value), 0, 255);
}
