namespace CSharpExercises.Web;

/// <summary>
/// A URL shortener's core: each new URL gets the next id from a counter, written in base 62 (0-9, a-z, A-Z) so 3.5
/// trillion links fit in 7 characters. Sequential codes are guessable, so the id is first scrambled with a reversible
/// multiply-and-mask permutation; custom aliases, duplicate URLs and click counts are handled on the side.
/// </summary>
public sealed class UrlShortener
{
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const long Space = 1L << 40;                    // ids scrambled within 2^40
    private const long Multiplier = 0x5DEECE66DL;            // odd, so it has an inverse modulo 2^40

    private readonly Dictionary<string, string> _byCode = new();
    private readonly Dictionary<string, string> _byUrl = new();
    private readonly Dictionary<string, int> _clicks = new();
    private long _counter;

    public string Shorten(string url, string? alias = null)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new ArgumentException("Only absolute http(s) URLs can be shortened.", nameof(url));

        if (alias is not null)
        {
            if (alias.Length < 3 || !alias.All(c => Alphabet.Contains(c) || c == '-')) throw new ArgumentException("Aliases use letters, digits and '-'.", nameof(alias));
            if (_byCode.ContainsKey(alias)) throw new InvalidOperationException($"'{alias}' is taken.");
            Store(alias, url);
            return alias;
        }

        if (_byUrl.TryGetValue(url, out var existing)) return existing; // same URL, same code
        string code;
        do code = ToBase62(Scramble(++_counter)); while (_byCode.ContainsKey(code)); // skip codes taken by aliases
        Store(code, url);
        return code;
    }

    public string? Resolve(string code)
    {
        if (!_byCode.TryGetValue(code, out var url)) return null;
        _clicks[code] = _clicks.GetValueOrDefault(code) + 1;
        return url;
    }

    public int Clicks(string code) => _clicks.GetValueOrDefault(code);

    public static string ToBase62(long value)
    {
        if (value == 0) return "0";
        var chars = new Stack<char>();
        for (; value > 0; value /= 62) chars.Push(Alphabet[(int)(value % 62)]);
        return new string(chars.ToArray());
    }

    public static long FromBase62(string code) =>
        code.Aggregate(0L, (acc, c) => Alphabet.IndexOf(c) is var digit and >= 0 ? acc * 62 + digit : throw new FormatException($"'{c}' is not base 62."));

    /// <summary>
    /// A bijection on 0..2^40-1: multiplying by an odd number modulo a power of two never collides. Overflow wraps
    /// modulo 2^64, and 2^40 divides 2^64, so masking the low 40 bits gives the right remainder.
    /// </summary>
    public static long Scramble(long id) => unchecked(id * Multiplier) & (Space - 1);

    private void Store(string code, string url)
    {
        _byCode[code] = url;
        _byUrl.TryAdd(url, code);
    }
}
