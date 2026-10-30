using System.Text;
using CSharpExercises.Imaging;

namespace CSharpExercises.Tests;

public class ImagingTests
{
    private static GrayImage Gradient(int width = 64, int height = 8) =>
        GrayImage.Create(width, height, (x, _) => (byte)(x * 255 / (width - 1)));

    [Fact]
    public void Colors_HexAndHslRoundTrip()
    {
        Assert.Equal(new Rgb(255, 136, 0), Rgb.Parse("#ff8800"));
        Assert.Equal(new Rgb(170, 187, 204), Rgb.Parse("abc"));
        Assert.Equal("#ff8800", Rgb.Parse("#FF8800").ToHex());
        Assert.Throws<FormatException>(() => Rgb.Parse("#12345"));

        var (h, s, l) = new Rgb(255, 0, 0).ToHsl();
        Assert.Equal((0.0, 1.0, 0.5), (h, s, l));
        Assert.Equal(120, new Rgb(0, 255, 0).ToHsl().H);

        var random = new Random(1);
        for (var i = 0; i < 200; i++)
        {
            var c = new Rgb((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));
            var hsl = c.ToHsl();
            var back = Rgb.FromHsl(hsl.H, hsl.S, hsl.L);
            Assert.InRange(Math.Abs(back.R - c.R) + Math.Abs(back.G - c.G) + Math.Abs(back.B - c.B), 0, 3);
        }
    }

    [Fact]
    public void Colors_ContrastAndReadableText()
    {
        Assert.Equal(21, Rgb.Black.ContrastWith(Rgb.White), 6);
        Assert.Equal(1, Rgb.Parse("#777").ContrastWith(Rgb.Parse("#777")), 6);
        Assert.InRange(Rgb.Parse("#767676").ContrastWith(Rgb.White), 4.5, 4.6); // the lightest grey that passes AA on white

        Assert.Equal(Rgb.Black, Rgb.Parse("#ffd700").ReadableTextColor()); // gold
        Assert.Equal(Rgb.White, Rgb.Parse("#1e3a8a").ReadableTextColor()); // navy
        Assert.Equal(new Rgb(128, 128, 128), Rgb.Black.Mix(Rgb.White, 0.5));
        Assert.True(Rgb.Parse("#336699").Lighten(0.2).RelativeLuminance() > Rgb.Parse("#336699").RelativeLuminance());
        Assert.Equal(76, new Rgb(255, 0, 0).ToGray());
    }

    [Fact]
    public void Convolution_BlurSmoothsAndKeepsFlatAreas()
    {
        var flat = GrayImage.Create(5, 5, (_, _) => 100);
        Assert.All(Convolution.Apply(flat, Convolution.GaussianBlur).Pixels.ToArray(), p => Assert.Equal(100, p));

        var dot = GrayImage.Create(5, 5, (x, y) => x == 2 && y == 2 ? (byte)255 : (byte)0);
        var blurred = Convolution.Apply(dot, Convolution.BoxBlur);
        Assert.Equal(28, blurred[2, 2]);  // 255 / 9
        Assert.Equal(28, blurred[1, 1]);
        Assert.Equal(0, blurred[0, 0]);
    }

    [Fact]
    public void Convolution_SobelFindsEdges()
    {
        var halves = GrayImage.Create(10, 10, (x, _) => x < 5 ? (byte)0 : (byte)200);

        var edges = Convolution.SobelEdges(halves);

        Assert.Equal(0, edges[1, 5]);
        Assert.Equal(255, edges[4, 5]); // 800 clamped
        Assert.Equal(0, edges[8, 5]);
    }

    [Fact]
    public void Convolution_FastBoxBlurMatchesTheKernel()
    {
        var random = new Random(2);
        var noise = GrayImage.Create(20, 20, (_, _) => (byte)random.Next(256));

        var slow = Convolution.Apply(noise, Convolution.BoxBlur);
        var fast = Convolution.FastBoxBlur(noise, 1);

        for (var y = 1; y < 19; y++) // the borders differ: clamping vs. shrinking the window
            for (var x = 1; x < 19; x++)
                Assert.InRange(Math.Abs(slow[x, y] - fast[x, y]), 0, 1);
    }

    [Fact]
    public void Dithering_PreservesAverageBrightness()
    {
        var gradient = Gradient(128, 32);

        var threshold = Dithering.Threshold(gradient);
        var diffused = Dithering.FloydSteinberg(gradient);
        var ordered = Dithering.Ordered(gradient);

        Assert.All(diffused.Pixels.ToArray(), p => Assert.True(p is 0 or 255));
        Assert.InRange(diffused.Mean, gradient.Mean - 3, gradient.Mean + 3);
        Assert.InRange(ordered.Mean, gradient.Mean - 8, gradient.Mean + 8);

        // A 25% grey area becomes about one white pixel in four with diffusion, but all black with a threshold.
        var quarter = GrayImage.Create(32, 32, (_, _) => 64);
        Assert.Equal(0, Dithering.Threshold(quarter).Mean);
        Assert.InRange(Dithering.FloydSteinberg(quarter).Mean, 58, 70);
        Assert.Equal(63.75, Dithering.Ordered(quarter).Mean); // exactly 4 of the 16 Bayer cells
        Assert.Equal(2, threshold.Pixels.ToArray().Distinct().Count());
    }

    [Fact]
    public void Histogram_EqualizeStretchesALowContrastImage()
    {
        var dull = GrayImage.Create(16, 16, (x, y) => (byte)(100 + (x + y) % 20));

        var equalized = Histogram.Equalize(dull);
        var stretched = Histogram.Stretch(dull);

        Assert.Equal(0, equalized.Pixels.ToArray().Min());
        Assert.Equal(255, equalized.Pixels.ToArray().Max());
        Assert.Equal((0, 255), (stretched.Pixels.ToArray().Min(), stretched.Pixels.ToArray().Max()));
        Assert.Equal(256, Histogram.Of(dull).Length);
    }

    [Fact]
    public void Histogram_OtsuSeparatesTwoGroups()
    {
        var random = new Random(3);
        var scan = GrayImage.Create(40, 40, (x, _) => (byte)(x < 10 ? 40 + random.Next(20) : 200 + random.Next(30)));

        var t = Histogram.OtsuThreshold(scan);

        Assert.InRange(t, 59, 199);
        Assert.Equal(10 * 40, scan.Pixels.ToArray().Count(p => p <= t));
    }

    [Fact]
    public void Resampling_NearestAndBilinear()
    {
        var checker = GrayImage.FromRows([0, 255], [255, 0]);

        var big = Resampling.NearestNeighbour(checker, 4, 4);
        Assert.Equal([0, 0, 255, 255], Enumerable.Range(0, 4).Select(x => big[x, 0]));

        var smooth = Resampling.Bilinear(Gradient(), 128, 8);
        var row = Enumerable.Range(0, 128).Select(x => smooth[x, 4]).ToArray();
        Assert.True(row.Zip(row.Skip(1)).All(p => p.First <= p.Second)); // still a monotonic gradient

        Assert.Equal((200, 150), Resampling.FitWithin(4000, 3000, 200, 200));
        Assert.Equal((100, 50), Resampling.FitWithin(100, 50, 200, 200)); // never enlarges
    }

    [Fact]
    public void Netpbm_RoundTripsBothFlavours()
    {
        var image = Gradient(7, 3);

        var binary = Netpbm.Read(Netpbm.WriteBinary(image));
        var ascii = Netpbm.Read(Encoding.ASCII.GetBytes(Netpbm.WriteAscii(image)));

        Assert.Equal(image.Pixels.ToArray(), binary.Pixels.ToArray());
        Assert.Equal(image.Pixels.ToArray(), ascii.Pixels.ToArray());
    }

    [Fact]
    public void Netpbm_HandlesCommentsAndSmallerMaxValues()
    {
        var image = Netpbm.Read(Encoding.ASCII.GetBytes("P2\n# made by hand\n3 1\n15\n0 15 5\n"));

        Assert.Equal([0, 255, 85], image.Pixels.ToArray());
        Assert.Throws<InvalidDataException>(() => Netpbm.Read(Encoding.ASCII.GetBytes("P6\n1 1\n255\n")));
        Assert.Throws<InvalidDataException>(() => Netpbm.Read([.. Encoding.ASCII.GetBytes("P5\n4 4\n255\n"), 1, 2]));
    }
}
