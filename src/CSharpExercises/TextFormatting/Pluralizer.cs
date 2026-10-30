namespace CSharpExercises.TextFormatting;

/// <summary>
/// English plurals for UI messages ("1 file", "3 files", "2 boxes", "4 children"). Irregular and unchanging words come
/// from a table, then suffix rules apply in order (-y after a consonant → -ies, sibilants → -es, -f/-fe → -ves for the
/// common cases). The original capitalisation is preserved. Other languages have very different plural rules, which
/// is why real apps use ICU message formats.
/// </summary>
public static class Pluralizer
{
    private static readonly Dictionary<string, string> Irregular = new(StringComparer.OrdinalIgnoreCase)
    {
        ["person"] = "people", ["child"] = "children", ["man"] = "men", ["woman"] = "women", ["mouse"] = "mice",
        ["goose"] = "geese", ["tooth"] = "teeth", ["foot"] = "feet", ["ox"] = "oxen", ["datum"] = "data",
        ["index"] = "indices", ["matrix"] = "matrices", ["criterion"] = "criteria", ["cactus"] = "cacti",
    };

    private static readonly HashSet<string> Unchanging = new(StringComparer.OrdinalIgnoreCase)
    {
        "sheep", "fish", "deer", "series", "species", "news", "information", "equipment", "software",
    };

    private static readonly HashSet<string> FToVes = new(StringComparer.OrdinalIgnoreCase)
    {
        "leaf", "loaf", "half", "knife", "life", "wife", "wolf", "shelf", "calf", "thief",
    };

    public static string Plural(string word)
    {
        if (word.Length == 0 || Unchanging.Contains(word)) return word;
        if (Irregular.TryGetValue(word, out var irregular))
            return word.All(char.IsUpper) && word.Length > 1 ? irregular.ToUpperInvariant() : MatchCase(word, irregular);

        var lower = word.ToLowerInvariant();
        string result;
        if (FToVes.Contains(lower)) result = lower.EndsWith("fe") ? lower[..^2] + "ves" : lower[..^1] + "ves";
        else if (lower.EndsWith('y') && lower.Length > 1 && !"aeiou".Contains(lower[^2])) result = lower[..^1] + "ies";
        else if (lower.EndsWith('s') || lower.EndsWith('x') || lower.EndsWith('z') || lower.EndsWith("ch") || lower.EndsWith("sh")) result = lower + "es";
        else if (lower is "hero" or "potato" or "tomato" or "echo" or "veto") result = lower + "es"; // most -o words just add -s
        else result = lower + "s";
        return MatchCase(word, result);
    }

    /// <summary>"0 files", "1 file", "2 files".</summary>
    public static string Quantity(int count, string singular) => $"{count} {(count == 1 ? singular : Plural(singular))}";

    private static string MatchCase(string original, string replacement) =>
        original.All(c => !char.IsLetter(c) || char.IsUpper(c)) && original.Length > 1 && replacement.StartsWith(original, StringComparison.OrdinalIgnoreCase)
            ? original + replacement[original.Length..] // acronyms: URL → URLs
            : original.All(c => !char.IsLetter(c) || char.IsUpper(c)) && original.Length > 1 ? replacement.ToUpperInvariant()
        : char.IsUpper(original[0]) ? char.ToUpperInvariant(replacement[0]) + replacement[1..]
        : replacement;
}
