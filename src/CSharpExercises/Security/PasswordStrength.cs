namespace CSharpExercises.Security;

public enum Strength { VeryWeak, Weak, Fair, Strong, VeryStrong }

public sealed record StrengthReport(double EntropyBits, Strength Strength, IReadOnlyList<string> Warnings);

/// <summary>
/// Estimating password strength the way modern guidance (NIST 800-63B) suggests: length matters most, composition rules
/// ("one uppercase, one symbol") matter little, and common or patterned passwords must be rejected outright because
/// attackers try them first. Entropy is estimated as length × log2(alphabet size), then reduced for repeats and sequences.
/// </summary>
public static class PasswordStrength
{
    private static readonly HashSet<string> Common = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "123456", "12345678", "qwerty", "letmein", "welcome", "admin", "iloveyou", "monkey", "dragon",
        "football", "baseball", "abc123", "111111", "sunshine", "princess", "passw0rd", "p@ssw0rd", "trustno1",
    };

    private static readonly string[] Sequences = ["abcdefghijklmnopqrstuvwxyz", "0123456789", "qwertyuiop", "asdfghjkl", "zxcvbnm"];

    public static StrengthReport Evaluate(string password, IEnumerable<string>? userInputs = null)
    {
        var warnings = new List<string>();
        if (password.Length < 8) warnings.Add("Use at least 8 characters.");

        var lower = password.ToLowerInvariant();
        if (Common.Contains(password) || Common.Contains(Unleet(lower))) warnings.Add("This is a very common password.");
        foreach (var input in userInputs ?? [])
            if (input.Length >= 3 && lower.Contains(input.ToLowerInvariant())) warnings.Add($"Don't include '{input}'.");

        var alphabet = (password.Any(char.IsAsciiLetterLower) ? 26 : 0) + (password.Any(char.IsAsciiLetterUpper) ? 26 : 0)
            + (password.Any(char.IsAsciiDigit) ? 10 : 0) + (password.Any(c => !char.IsAsciiLetterOrDigit(c)) ? 33 : 0);

        // Count only characters that add information: repeats ("aaaa") and runs ("abcd", "4321") add little.
        var effectiveLength = 0.0;
        for (var i = 0; i < password.Length; i++)
        {
            var predictable = i > 0 && (lower[i] == lower[i - 1] || InSequence(lower[i - 1], lower[i]));
            effectiveLength += predictable ? 0.25 : 1;
        }
        if (effectiveLength < password.Length * 0.6) warnings.Add("Avoid repeated characters and sequences like 'abc' or '123'.");

        var entropy = warnings.Any(w => w.Contains("common")) ? 0 : effectiveLength * Math.Log2(Math.Max(alphabet, 1));
        var strength = entropy switch
        {
            < 28 => Strength.VeryWeak,
            < 36 => Strength.Weak,
            < 60 => Strength.Fair,
            < 80 => Strength.Strong,
            _ => Strength.VeryStrong,
        };
        return new StrengthReport(Math.Round(entropy, 1), strength, warnings);
    }

    private static bool InSequence(char a, char b) =>
        Sequences.Any(s => s.IndexOf(a) is var i and >= 0 && (i + 1 < s.Length && s[i + 1] == b || i > 0 && s[i - 1] == b));

    /// <summary>"p@ssw0rd" → "password": substitutions everyone (including attackers) knows.</summary>
    private static string Unleet(string s) =>
        new(s.Select(c => c switch { '@' => 'a', '0' => 'o', '1' => 'i', '3' => 'e', '$' => 's', '5' => 's', '7' => 't', _ => c }).ToArray());
}
