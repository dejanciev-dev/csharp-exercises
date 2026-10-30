namespace CSharpExercises.Compression;

/// <summary>
/// LZW, the algorithm in GIF and Unix compress: start with a dictionary of all single bytes, and each time a known string
/// is followed by a new byte, emit the code for the known string and add the longer string as a new code. The decoder
/// rebuilds the same dictionary from the codes alone, so it is never transmitted. One subtle case: a code that refers to
/// the entry being defined in that very step ("cScSc").
/// </summary>
public static class Lzw
{
    public static List<int> Compress(ReadOnlySpan<byte> input, int maxCodes = 1 << 16)
    {
        var dictionary = new Dictionary<string, int>();
        for (var i = 0; i < 256; i++) dictionary[((char)i).ToString()] = i;

        var codes = new List<int>();
        var current = "";
        foreach (var b in input)
        {
            var extended = current + (char)b;
            if (dictionary.ContainsKey(extended))
            {
                current = extended;
                continue;
            }
            codes.Add(dictionary[current]);
            if (dictionary.Count < maxCodes) dictionary[extended] = dictionary.Count; // stop growing when full
            current = ((char)b).ToString();
        }
        if (current.Length > 0) codes.Add(dictionary[current]);
        return codes;
    }

    public static byte[] Decompress(IReadOnlyList<int> codes, int maxCodes = 1 << 16)
    {
        if (codes.Count == 0) return [];
        var dictionary = Enumerable.Range(0, 256).Select(i => ((char)i).ToString()).ToList();
        var previous = Entry(codes[0], dictionary, null);
        var output = new System.Text.StringBuilder(previous);

        foreach (var code in codes.Skip(1))
        {
            var entry = Entry(code, dictionary, previous);
            output.Append(entry);
            if (dictionary.Count < maxCodes) dictionary.Add(previous + entry[0]);
            previous = entry;
        }
        return output.ToString().Select(c => (byte)c).ToArray();
    }

    private static string Entry(int code, List<string> dictionary, string? previous) =>
        code < dictionary.Count ? dictionary[code]
        : code == dictionary.Count && previous is not null ? previous + previous[0] // the "cScSc" case: not defined yet
        : throw new InvalidDataException($"Invalid code {code}.");
}
