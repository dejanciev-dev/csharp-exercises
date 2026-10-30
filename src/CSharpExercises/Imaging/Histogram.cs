namespace CSharpExercises.Imaging;

/// <summary>
/// Working with the brightness histogram. Equalisation spreads a washed-out image's crowded grey levels over the whole
/// range by mapping each level through the cumulative distribution. Otsu's method picks the threshold that best
/// separates foreground from background by maximising the variance between the two classes, used to binarise scanned
/// documents before OCR.
/// </summary>
public static class Histogram
{
    public static int[] Of(GrayImage image)
    {
        var counts = new int[256];
        foreach (var p in image.Pixels) counts[p]++;
        return counts;
    }

    public static GrayImage Equalize(GrayImage image)
    {
        var counts = Of(image);
        var cdf = new int[256];
        for (int i = 0, running = 0; i < 256; i++) cdf[i] = running += counts[i];

        var cdfMin = cdf.First(c => c > 0);
        var total = image.Width * image.Height;
        if (total == cdfMin) return GrayImage.Create(image.Width, image.Height, (x, y) => image[x, y]); // a single grey level

        var map = cdf.Select(c => GrayImage.ToByte((double)(c - cdfMin) / (total - cdfMin) * 255)).ToArray();
        return GrayImage.Create(image.Width, image.Height, (x, y) => map[image[x, y]]);
    }

    public static byte OtsuThreshold(GrayImage image)
    {
        var counts = Of(image);
        var total = image.Width * image.Height;
        var sumAll = Enumerable.Range(0, 256).Sum(i => (double)i * counts[i]);

        double sumBackground = 0, bestVariance = -1;
        int weightBackground = 0;
        byte best = 0;
        for (var t = 0; t < 256; t++)
        {
            weightBackground += counts[t];
            if (weightBackground == 0) continue;
            var weightForeground = total - weightBackground;
            if (weightForeground == 0) break;

            sumBackground += t * counts[t];
            var meanBackground = sumBackground / weightBackground;
            var meanForeground = (sumAll - sumBackground) / weightForeground;
            var betweenVariance = (double)weightBackground * weightForeground * Math.Pow(meanBackground - meanForeground, 2);
            if (betweenVariance > bestVariance) (bestVariance, best) = (betweenVariance, (byte)t);
        }
        return best; // pixels <= best are background
    }

    /// <summary>Contrast stretch: map the darkest pixel to 0 and the brightest to 255, linearly.</summary>
    public static GrayImage Stretch(GrayImage image)
    {
        var (min, max) = (image.Pixels.ToArray().Min(), image.Pixels.ToArray().Max());
        if (min == max) return GrayImage.Create(image.Width, image.Height, (x, y) => image[x, y]);
        return GrayImage.Create(image.Width, image.Height, (x, y) => GrayImage.ToByte((image[x, y] - min) * 255.0 / (max - min)));
    }
}
