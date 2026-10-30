namespace CSharpExercises.Ciphers;

/// <summary>
/// The Caesar cipher shifts every letter by a fixed amount, and shows why such ciphers are broken: there are only 25
/// keys, and even without trying them all, English letter frequencies give the key away. For each shift, compare the
/// decrypted letter counts with English using the chi-squared statistic; the lowest score wins.
/// </summary>
public static class Caesar
{
    /// <summary>Relative frequencies of a–z in English text.</summary>
    public static readonly double[] EnglishFrequencies =
    [
        .0817, .0149, .0278, .0425, .1270, .0223, .0202, .0609, .0697, .0015, .0077, .0403, .0241,
        .0675, .0751, .0193, .0010, .0599, .0633, .0906, .0276, .0098, .0236, .0015, .0197, .0007,
    ];

    public static string Encrypt(string text, int shift) => new(text.Select(c => Shift(c, shift)).ToArray());

    public static string Decrypt(string text, int shift) => Encrypt(text, -shift);

    /// <summary>Finds the shift whose decryption looks most like English.</summary>
    public static int Crack(string ciphertext) =>
        Enumerable.Range(0, 26).MinBy(shift => ChiSquared(Decrypt(ciphertext, shift)));

    /// <summary>Σ (observed - expected)² / expected over the letters: small when the text resembles English.</summary>
    public static double ChiSquared(string text)
    {
        var letters = text.Where(char.IsAsciiLetter).Select(char.ToLowerInvariant).ToArray();
        if (letters.Length == 0) return double.MaxValue;
        var counts = new int[26];
        foreach (var c in letters) counts[c - 'a']++;
        return Enumerable.Range(0, 26).Sum(i =>
        {
            var expected = EnglishFrequencies[i] * letters.Length;
            return (counts[i] - expected) * (counts[i] - expected) / expected;
        });
    }

    private static char Shift(char c, int shift)
    {
        var k = (shift % 26 + 26) % 26;
        return c switch
        {
            >= 'a' and <= 'z' => (char)('a' + (c - 'a' + k) % 26),
            >= 'A' and <= 'Z' => (char)('A' + (c - 'A' + k) % 26),
            _ => c,
        };
    }
}
