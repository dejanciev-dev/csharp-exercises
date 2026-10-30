namespace CSharpExercises.Ciphers;

/// <summary>
/// Vigenère uses a keyword: each letter is shifted by the matching letter of the repeated key. It resisted attack for
/// three centuries, but it falls to statistics: the index of coincidence (how likely two random letters are equal) is
/// about 0.066 for English and 0.038 for random text, so splitting the ciphertext into every k-th letter and finding the
/// k where the columns look like English reveals the key length; each column is then just a Caesar cipher.
/// </summary>
public static class Vigenere
{
    public static string Encrypt(string text, string key) => Apply(text, key, +1);
    public static string Decrypt(string text, string key) => Apply(text, key, -1);

    public static double IndexOfCoincidence(string text)
    {
        var letters = text.Where(char.IsAsciiLetter).Select(char.ToLowerInvariant).ToArray();
        if (letters.Length < 2) return 0;
        var counts = letters.GroupBy(c => c).Select(g => (double)g.Count());
        return counts.Sum(n => n * (n - 1)) / (letters.Length * (letters.Length - 1.0));
    }

    /// <summary>The key length (up to <paramref name="maxLength"/>) whose columns have the most English-like coincidence.</summary>
    public static int GuessKeyLength(string ciphertext, int maxLength = 12)
    {
        var letters = new string(ciphertext.Where(char.IsAsciiLetter).Select(char.ToLowerInvariant).ToArray());
        var scores = Enumerable.Range(1, maxLength)
            .Select(k => (k, Score: Enumerable.Range(0, k).Average(offset => IndexOfCoincidence(Column(letters, k, offset)))))
            .ToList();
        // Multiples of the true length score as well; take the shortest length that is nearly as good as the best.
        var best = scores.Max(s => s.Score);
        return scores.First(s => s.Score > 0.9 * best).k;
    }

    public static string Crack(string ciphertext, int maxKeyLength = 12)
    {
        var letters = new string(ciphertext.Where(char.IsAsciiLetter).Select(char.ToLowerInvariant).ToArray());
        var length = GuessKeyLength(ciphertext, maxKeyLength);
        return new string(Enumerable.Range(0, length).Select(i => (char)('a' + Caesar.Crack(Column(letters, length, i)))).ToArray());
    }

    private static string Column(string letters, int keyLength, int offset) =>
        new(letters.Where((_, i) => i % keyLength == offset).ToArray());

    private static string Apply(string text, string key, int direction)
    {
        if (key.Length == 0 || !key.All(char.IsAsciiLetter)) throw new ArgumentException("The key must be letters only.", nameof(key));
        var k = key.ToLowerInvariant();
        var position = 0;
        return new string(text.Select(c =>
        {
            if (!char.IsAsciiLetter(c)) return c; // non-letters don't advance the key
            var shift = direction * (k[position++ % k.Length] - 'a');
            return Caesar.Encrypt(c.ToString(), shift)[0];
        }).ToArray());
    }
}
