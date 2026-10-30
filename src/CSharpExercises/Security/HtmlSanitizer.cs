using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace CSharpExercises.Security;

/// <summary>
/// Cleaning user-supplied HTML (comments, rich-text fields) to prevent cross-site scripting. An allowlist, never a
/// blocklist: only known-safe tags and attributes survive, everything else is escaped or dropped, and link URLs must use
/// a safe scheme (no javascript:). Scripts and styles lose their content too. For production use a maintained library;
/// the point here is the allowlist mindset.
/// </summary>
public static partial class HtmlSanitizer
{
    private static readonly Dictionary<string, string[]> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        ["b"] = [], ["i"] = [], ["em"] = [], ["strong"] = [], ["p"] = [], ["br"] = [], ["ul"] = [], ["ol"] = [], ["li"] = [],
        ["code"] = [], ["pre"] = [], ["blockquote"] = [], ["a"] = ["href", "title"],
    };

    private static readonly HashSet<string> DropWithContent = new(StringComparer.OrdinalIgnoreCase) { "script", "style", "iframe", "object" };
    private static readonly string[] SafeSchemes = ["http", "https", "mailto"];

    public static string Sanitize(string html)
    {
        var output = new StringBuilder();
        var position = 0;
        string? dropping = null;

        foreach (Match tag in TagPattern().Matches(html))
        {
            if (dropping is null) output.Append(Encode(html[position..tag.Index]));
            position = tag.Index + tag.Length;

            var closing = tag.Groups["close"].Success;
            var name = tag.Groups["name"].Value;

            if (dropping is not null)
            {
                if (closing && name.Equals(dropping, StringComparison.OrdinalIgnoreCase)) dropping = null;
                continue;
            }
            if (DropWithContent.Contains(name))
            {
                if (!closing) dropping = name;
                continue;
            }
            if (!AllowedTags.TryGetValue(name, out var allowedAttributes)) continue; // unknown tag: drop the tag, keep the text

            if (closing)
            {
                output.Append($"</{name.ToLowerInvariant()}>");
                continue;
            }

            output.Append('<').Append(name.ToLowerInvariant());
            foreach (Match attribute in AttributePattern().Matches(tag.Groups["attrs"].Value))
            {
                var attrName = attribute.Groups["name"].Value.ToLowerInvariant();
                var value = WebUtility.HtmlDecode(attribute.Groups["value"].Value);
                if (!allowedAttributes.Contains(attrName)) continue;          // onclick, style, … are gone
                if (attrName == "href" && !IsSafeUrl(value)) continue;
                output.Append($" {attrName}=\"{WebUtility.HtmlEncode(value)}\"");
            }
            if (name.Equals("a", StringComparison.OrdinalIgnoreCase)) output.Append(" rel=\"nofollow noopener\"");
            output.Append('>');
        }
        if (dropping is null) output.Append(Encode(html[position..]));
        return output.ToString();
    }

    /// <summary>Relative URLs are fine; absolute ones need an allowed scheme. Whitespace and control characters are
    /// stripped first, because browsers ignore them ("java\tscript:" still runs).</summary>
    public static bool IsSafeUrl(string url)
    {
        var cleaned = new string(url.Where(c => !char.IsWhiteSpace(c) && !char.IsControl(c)).ToArray());
        var colon = cleaned.IndexOf(':');
        if (colon < 0 || cleaned.IndexOfAny(['/', '?', '#']) is var first && first >= 0 && first < colon) return true; // relative
        return SafeSchemes.Contains(cleaned[..colon].ToLowerInvariant());
    }

    private static string Encode(string text) => WebUtility.HtmlEncode(WebUtility.HtmlDecode(text));

    [GeneratedRegex(@"<(?<close>/)?(?<name>[a-zA-Z][a-zA-Z0-9]*)(?<attrs>[^>]*)>")]
    private static partial Regex TagPattern();

    [GeneratedRegex(@"(?<name>[a-zA-Z-]+)\s*=\s*(?:""(?<value>[^""]*)""|'(?<value>[^']*)'|(?<value>[^\s>]+))")]
    private static partial Regex AttributePattern();
}
