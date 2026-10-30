using System.Text;

namespace CSharpExercises.TextFormatting;

/// <summary>
/// Converting identifiers between naming conventions (camelCase, PascalCase, snake_case, kebab-case, Title Case).
/// The trick is splitting into words first, and acronyms make that harder: "parseHTTPResponse" should split as
/// parse / HTTP / Response, so an uppercase run ends one letter before a lowercase letter.
/// </summary>
public static class CaseConverter
{
    public static IReadOnlyList<string> SplitWords(string identifier)
    {
        var words = new List<string>();
        var current = new StringBuilder();

        for (var i = 0; i < identifier.Length; i++)
        {
            var c = identifier[i];
            if (!char.IsLetterOrDigit(c))
            {
                Flush();
                continue;
            }

            var previous = i > 0 ? identifier[i - 1] : '\0';
            var next = i + 1 < identifier.Length ? identifier[i + 1] : '\0';
            // Digits stay attached to the word before them ("utf8"), so only upper-case letters start new words.
            var boundary = current.Length > 0 && char.IsUpper(c) && (
                char.IsLower(previous) || char.IsDigit(previous) ||   // fooBar, v2Api
                (char.IsUpper(previous) && char.IsLower(next)));     // HTTPResponse → HTTP | Response
            if (boundary) Flush();
            current.Append(c);
        }
        Flush();
        return words;

        void Flush()
        {
            if (current.Length > 0) words.Add(current.ToString());
            current.Clear();
        }
    }

    public static string ToCamel(string s)
    {
        var words = SplitWords(s);
        return string.Concat(words.Select((w, i) => i == 0 ? w.ToLowerInvariant() : Capitalize(w)));
    }

    public static string ToPascal(string s) => string.Concat(SplitWords(s).Select(Capitalize));
    public static string ToSnake(string s) => string.Join('_', SplitWords(s).Select(w => w.ToLowerInvariant()));
    public static string ToKebab(string s) => string.Join('-', SplitWords(s).Select(w => w.ToLowerInvariant()));
    public static string ToConstant(string s) => string.Join('_', SplitWords(s).Select(w => w.ToUpperInvariant()));

    /// <summary>Title case that leaves short joining words lower-case unless they start or end the title.</summary>
    public static string ToTitle(string s)
    {
        string[] minor = ["a", "an", "the", "and", "but", "or", "of", "in", "on", "at", "to", "for", "by"];
        var words = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words.Select((w, i) =>
            i > 0 && i < words.Length - 1 && minor.Contains(w.ToLowerInvariant()) ? w.ToLowerInvariant() : Capitalize(w)));
    }

    private static string Capitalize(string word) =>
        word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant();
}
