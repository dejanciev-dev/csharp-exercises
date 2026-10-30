namespace CSharpExercises.TextAlgorithms;

/// <summary>
/// Rabin–Karp: compare hashes instead of strings, with a rolling hash that updates in O(1) as the window slides
/// one character. Hash matches are verified, because different strings can share a hash.
/// </summary>
public static class RabinKarp
{
    private const ulong Base = 131;
    private const ulong Modulus = (1UL << 61) - 1; // a Mersenne prime keeps collisions rare

    public static IEnumerable<int> FindAll(string text, string pattern)
    {
        if (pattern.Length == 0 || pattern.Length > text.Length) yield break;

        var m = pattern.Length;
        var patternHash = Hash(pattern.AsSpan());
        var windowHash = Hash(text.AsSpan(0, m));
        var highest = Power(Base, m - 1); // weight of the character leaving the window

        for (var i = 0; ; i++)
        {
            if (windowHash == patternHash && string.CompareOrdinal(text, i, pattern, 0, m) == 0) yield return i;
            if (i + m >= text.Length) yield break;

            windowHash = Sub(windowHash, Mul(text[i], highest));
            windowHash = Add(Mul(windowHash, Base), text[i + m]);
        }
    }

    /// <summary>
    /// The longest substring that occurs at least twice: binary search on the length, checking each candidate length
    /// in O(n) with a set of rolling hashes. If a length-k repeat exists, so does every shorter one.
    /// </summary>
    public static string LongestRepeatedSubstring(string text)
    {
        int low = 0, high = text.Length - 1, bestStart = 0;
        while (low < high)
        {
            var length = (low + high + 1) / 2;
            var start = FindRepeat(text, length);
            if (start >= 0) (low, bestStart) = (length, start);
            else high = length - 1;
        }
        return text.Substring(bestStart, Math.Max(low, 0));
    }

    private static int FindRepeat(string text, int length)
    {
        var seen = new Dictionary<ulong, List<int>>();
        var hash = Hash(text.AsSpan(0, length));
        var highest = Power(Base, length - 1);

        for (var i = 0; ; i++)
        {
            if (seen.TryGetValue(hash, out var starts))
            {
                foreach (var s in starts)
                    if (string.CompareOrdinal(text, s, text, i, length) == 0) return i;
                starts.Add(i);
            }
            else seen[hash] = [i];

            if (i + length >= text.Length) return -1;
            hash = Add(Mul(Sub(hash, Mul(text[i], highest)), Base), text[i + length]);
        }
    }

    private static ulong Hash(ReadOnlySpan<char> s)
    {
        ulong h = 0;
        foreach (var c in s) h = Add(Mul(h, Base), c);
        return h;
    }

    private static ulong Power(ulong b, int e)
    {
        ulong result = 1;
        for (var i = 0; i < e; i++) result = Mul(result, b);
        return result;
    }

    private static ulong Add(ulong a, ulong b) => (a + b) % Modulus;
    private static ulong Sub(ulong a, ulong b) => (a + Modulus - b % Modulus) % Modulus;
    private static ulong Mul(ulong a, ulong b) => (ulong)((UInt128)a * b % Modulus);
}
