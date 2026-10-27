using System.Globalization;
using System.Text;

namespace CSharpExercises.Strings;

/// <summary>
/// Text as people see it. A C# <see cref="char"/> is a UTF-16 code unit, not a character: an emoji takes two, a flag
/// takes four, and "é" may be one char or "e" plus a combining accent. <see cref="StringInfo"/> works in
/// <em>text elements</em> (grapheme clusters), which is what a user would count, select or delete as one character.
/// </summary>
public static class UnicodeText
{
    /// <summary>Characters as a user would count them; <c>"👍🏽".Length</c> is 4, but this returns 1.</summary>
    public static int CountCharacters(string text) => new StringInfo(text).LengthInTextElements;

    /// <summary>
    /// Reverses whole text elements. <see cref="StringExercises.Reverse"/> reverses chars, which splits surrogate
    /// pairs into invalid UTF-16 and moves accents onto the wrong letter.
    /// </summary>
    public static string Reverse(string text)
    {
        var elements = TextElements(text).ToList();
        elements.Reverse();
        return string.Concat(elements);
    }

    /// <summary>Shortens to at most <paramref name="maxCharacters"/> text elements, ellipsis included, without cutting one in half.</summary>
    public static string Truncate(string text, int maxCharacters, string ellipsis = "…")
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxCharacters);

        var info = new StringInfo(text);
        if (info.LengthInTextElements <= maxCharacters) return text;

        var keep = Math.Max(0, maxCharacters - CountCharacters(ellipsis));
        return info.SubstringByTextElements(0, keep) + (maxCharacters >= CountCharacters(ellipsis) ? ellipsis : "");
    }

    /// <summary>
    /// A URL-friendly version of a title: <c>"Crème Brûlée à la carte!"</c> becomes <c>"creme-brulee-a-la-carte"</c>.
    /// Decomposing (FormD) splits "é" into "e" + a combining accent, which is then dropped. Letters that do not decompose
    /// (ß, æ, ø, đ, ł) are mapped by hand; other scripts are removed, where a real app would transliterate them.
    /// </summary>
    public static string Slugify(string text)
    {
        var slug = new StringBuilder(text.Length);
        var pendingDash = false;

        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;

            var lower = char.ToLowerInvariant(c);
            var mapped = lower switch
            {
                'ß' => "ss",
                'æ' => "ae",
                'ø' => "o",
                'đ' => "d",
                'ł' => "l",
                _ when char.IsAsciiLetterOrDigit(lower) => lower.ToString(),
                _ => null,
            };

            if (mapped is null)
            {
                pendingDash = slug.Length > 0; // collapse runs of separators, and never start with one
                continue;
            }

            if (pendingDash) slug.Append('-');
            pendingDash = false;
            slug.Append(mapped);
        }

        return slug.ToString();
    }

    /// <summary>"Résumé" equals "resume": compares ignoring case and accents, for search boxes and name matching.</summary>
    public static bool EqualsIgnoringAccents(string a, string b) =>
        CultureInfo.InvariantCulture.CompareInfo.Compare(a, b, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0;

    private static IEnumerable<string> TextElements(string text)
    {
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext()) yield return enumerator.GetTextElement();
    }
}
