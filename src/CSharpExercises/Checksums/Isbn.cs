namespace CSharpExercises.Checksums;

/// <summary>
/// ISBN check digits, two generations. ISBN-10 weights digits 10 down to 1 mod 11, which catches every single error
/// and every swap of adjacent digits (the check can be 10, written X). ISBN-13 (the EAN-13 barcode) weights
/// alternately 1 and 3 mod 10: simpler, but it misses some swaps. Converting adds the 978 prefix and recomputes.
/// </summary>
public static class Isbn
{
    public static bool IsValid10(string isbn)
    {
        var s = Clean(isbn);
        if (s.Length != 10 || !s[..9].All(char.IsAsciiDigit) || !(char.IsAsciiDigit(s[9]) || s[9] == 'X')) return false;
        var sum = 0;
        for (var i = 0; i < 10; i++) sum += (10 - i) * (s[i] == 'X' ? 10 : s[i] - '0');
        return sum % 11 == 0;
    }

    public static bool IsValid13(string isbn)
    {
        var s = Clean(isbn);
        return s.Length == 13 && s.All(char.IsAsciiDigit) && CheckDigit13(s[..12]) == s[12] - '0';
    }

    public static int CheckDigit13(string first12)
    {
        var sum = 0;
        for (var i = 0; i < 12; i++) sum += (first12[i] - '0') * (i % 2 == 0 ? 1 : 3);
        return (10 - sum % 10) % 10;
    }

    public static string ToIsbn13(string isbn10)
    {
        if (!IsValid10(isbn10)) throw new FormatException($"'{isbn10}' is not a valid ISBN-10.");
        var body = "978" + Clean(isbn10)[..9];
        return body + CheckDigit13(body);
    }

    private static string Clean(string isbn) => new(isbn.Where(c => c is not ('-' or ' ')).Select(char.ToUpperInvariant).ToArray());
}
