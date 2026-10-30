using System.Text;

namespace CSharpExercises.Imaging;

/// <summary>
/// The PGM format from the Netpbm family: maybe the simplest real image format, a short text header ("P5", width,
/// height, max value) followed by raw bytes (P5) or decimal numbers (P2). Useful for writing an image from code without
/// any library, and every image viewer and converter reads it. Comments start with '#'.
/// </summary>
public static class Netpbm
{
    public static byte[] WriteBinary(GrayImage image)
    {
        var header = Encoding.ASCII.GetBytes($"P5\n{image.Width} {image.Height}\n255\n");
        return [.. header, .. image.Pixels];
    }

    public static string WriteAscii(GrayImage image)
    {
        var sb = new StringBuilder($"P2\n{image.Width} {image.Height}\n255\n");
        for (var y = 0; y < image.Height; y++)
            sb.AppendJoin(' ', Enumerable.Range(0, image.Width).Select(x => image[x, y])).Append('\n');
        return sb.ToString();
    }

    public static GrayImage Read(byte[] data)
    {
        var position = 0;

        string NextToken()
        {
            while (true)
            {
                while (position < data.Length && char.IsWhiteSpace((char)data[position])) position++;
                if (position < data.Length && data[position] == '#')
                {
                    while (position < data.Length && data[position] != '\n') position++; // skip comment line
                    continue;
                }
                break;
            }
            var start = position;
            while (position < data.Length && !char.IsWhiteSpace((char)data[position])) position++;
            if (start == position) throw new InvalidDataException("Unexpected end of PGM data.");
            return Encoding.ASCII.GetString(data, start, position - start);
        }

        var magic = NextToken();
        if (magic is not ("P2" or "P5")) throw new InvalidDataException($"Not a PGM file (magic '{magic}').");
        var width = int.Parse(NextToken());
        var height = int.Parse(NextToken());
        var maxValue = int.Parse(NextToken());
        if (maxValue is < 1 or > 255) throw new InvalidDataException("Only 8-bit PGM is supported.");
        var pixels = new byte[width * height];

        if (magic == "P5")
        {
            position++; // exactly one whitespace byte separates the header from the raster
            if (data.Length - position < pixels.Length) throw new InvalidDataException("Truncated pixel data.");
            Array.Copy(data, position, pixels, 0, pixels.Length);
        }
        else
        {
            for (var i = 0; i < pixels.Length; i++) pixels[i] = (byte)int.Parse(NextToken());
        }

        if (maxValue != 255)
            for (var i = 0; i < pixels.Length; i++) pixels[i] = GrayImage.ToByte(pixels[i] * 255.0 / maxValue);
        return new GrayImage(width, height, pixels);
    }
}
