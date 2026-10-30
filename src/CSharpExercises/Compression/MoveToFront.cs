namespace CSharpExercises.Compression;

/// <summary>
/// Move-to-front coding, bzip2's second stage after Burrows–Wheeler: replace each byte with its position in a list of
/// recently used bytes, then move it to the front. Runs of the same byte become runs of zeros and recently seen bytes
/// become small numbers, which an entropy coder (Huffman) then squeezes. The skew is measured with Shannon entropy.
/// </summary>
public static class MoveToFront
{
    public static byte[] Encode(ReadOnlySpan<byte> input)
    {
        var table = Enumerable.Range(0, 256).Select(i => (byte)i).ToList();
        var output = new byte[input.Length];
        for (var i = 0; i < input.Length; i++)
        {
            var index = table.IndexOf(input[i]);
            output[i] = (byte)index;
            table.RemoveAt(index);
            table.Insert(0, input[i]);
        }
        return output;
    }

    public static byte[] Decode(ReadOnlySpan<byte> input)
    {
        var table = Enumerable.Range(0, 256).Select(i => (byte)i).ToList();
        var output = new byte[input.Length];
        for (var i = 0; i < input.Length; i++)
        {
            var value = table[input[i]];
            output[i] = value;
            table.RemoveAt(input[i]);
            table.Insert(0, value);
        }
        return output;
    }

    /// <summary>Bits per symbol a perfect entropy coder would need for this byte distribution.</summary>
    public static double Entropy(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty) return 0;
        var counts = new int[256];
        foreach (var b in data) counts[b]++;
        double total = data.Length; // a span can't be captured by the lambda below
        return counts.Where(c => c > 0).Sum(c => -(c / total) * Math.Log2(c / total));
    }
}
