namespace CSharpExercises.Compression;

public readonly record struct Lz77Token(int Distance, int Length, byte Next);

/// <summary>
/// LZ77, the idea inside gzip, zip and PNG: replace repeated byte runs with a back-reference "copy Length bytes from
/// Distance bytes ago", found by searching a sliding window of recent input. A reference may overlap the bytes it is
/// producing ("abcabcabc" from one "abc"), which is how runs compress. A hash of 3-byte prefixes finds candidate
/// matches without scanning the whole window.
/// </summary>
public static class Lz77
{
    public static List<Lz77Token> Compress(ReadOnlySpan<byte> input, int windowSize = 4096, int maxLength = 255)
    {
        var tokens = new List<Lz77Token>();
        var chains = new Dictionary<int, List<int>>(); // 3-byte prefix → positions where it occurred
        var position = 0;

        while (position < input.Length)
        {
            var (bestDistance, bestLength) = (0, 0);
            if (position + 3 <= input.Length && chains.TryGetValue(Key(input, position), out var candidates))
            {
                for (var c = candidates.Count - 1; c >= 0 && position - candidates[c] <= windowSize; c--) // newest first
                {
                    var start = candidates[c];
                    var length = 0;
                    while (length < maxLength && position + length < input.Length - 1 && input[start + length] == input[position + length]) length++;
                    if (length > bestLength) (bestDistance, bestLength) = (position - start, length);
                }
            }

            var end = position + bestLength;
            tokens.Add(new Lz77Token(bestDistance, bestLength, input[end]));
            for (var p = position; p <= end && p + 3 <= input.Length; p++)
            {
                var key = Key(input, p);
                if (!chains.TryGetValue(key, out var list)) chains[key] = list = [];
                list.Add(p);
            }
            position = end + 1;
        }
        return tokens;
    }

    public static byte[] Decompress(IEnumerable<Lz77Token> tokens)
    {
        var output = new List<byte>();
        foreach (var (distance, length, next) in tokens)
        {
            if (distance > output.Count || (length > 0 && distance == 0)) throw new InvalidDataException("Back-reference before the start of the data.");
            var start = output.Count - distance;
            for (var i = 0; i < length; i++) output.Add(output[start + i]); // byte by byte so overlapping copies work
            output.Add(next);
        }
        return [.. output];
    }

    private static int Key(ReadOnlySpan<byte> data, int i) => data[i] << 16 | data[i + 1] << 8 | data[i + 2];
}
