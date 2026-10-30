using System.Text;

namespace CSharpExercises.Checksums;

/// <summary>
/// International bank account numbers (ISO 13616). Move the country code and check digits to the end, turn letters
/// into numbers (A = 10 … Z = 35) and the result mod 97 must be 1. The number has up to ~70 digits, so the remainder
/// is computed piece by piece instead of parsing it into one huge integer.
/// </summary>
public static class Iban
{
    private static readonly Dictionary<string, int> Lengths = new()
    {
        ["DE"] = 22, ["GB"] = 22, ["FR"] = 27, ["NL"] = 18, ["BE"] = 16, ["ES"] = 24, ["IT"] = 27, ["CH"] = 21, ["MK"] = 19, ["AT"] = 20,
    };

    public static bool IsValid(string iban, out string? error)
    {
        var compact = Normalise(iban);
        error = compact switch
        {
            { Length: < 5 } => "Too short.",
            _ when !char.IsAsciiLetterUpper(compact[0]) || !char.IsAsciiLetterUpper(compact[1]) => "Must start with a country code.",
            _ when !char.IsAsciiDigit(compact[2]) || !char.IsAsciiDigit(compact[3]) => "Check digits must be numeric.",
            _ when compact.Any(c => !char.IsAsciiLetterOrDigit(c)) => "Only letters and digits are allowed.",
            _ when Lengths.TryGetValue(compact[..2], out var length) && compact.Length != length =>
                $"{compact[..2]} IBANs have {length} characters, not {compact.Length}.",
            _ when Mod97(compact[4..] + compact[..4]) != 1 => "Check digits don't match: probably a typo.",
            _ => null,
        };
        return error is null;
    }

    /// <summary>Computes the two check digits for a country code and account number (the BBAN).</summary>
    public static string Create(string countryCode, string bban)
    {
        var body = Normalise(bban);
        var check = 98 - Mod97(body + countryCode.ToUpperInvariant() + "00");
        return $"{countryCode.ToUpperInvariant()}{check:00}{body}";
    }

    /// <summary>Groups of four characters, as printed on statements.</summary>
    public static string Format(string iban) => string.Join(' ', Normalise(iban).Chunk(4).Select(c => new string(c)));

    private static string Normalise(string iban) => new(iban.Where(c => !char.IsWhiteSpace(c)).Select(char.ToUpperInvariant).ToArray());

    /// <summary>Long division mod 97, one digit at a time: (remainder · 10 + digit) mod 97.</summary>
    private static int Mod97(string text)
    {
        var remainder = 0;
        foreach (var c in text)
        {
            var value = char.IsAsciiDigit(c) ? c - '0' : c - 'A' + 10;
            remainder = (value < 10 ? remainder * 10 + value : remainder * 100 + value) % 97;
        }
        return remainder;
    }
}
