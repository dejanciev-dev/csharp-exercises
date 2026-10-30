using System.Globalization;

namespace CSharpExercises.Web;

/// <summary>
/// HTTP content negotiation: the client lists what it accepts with quality weights ("Accept: text/html,
/// application/json;q=0.9, */*;q=0.1") and the server picks the best format it can produce. More specific ranges
/// override less specific ones (text/html beats text/* beats */*), q=0 means "never", and ties go to the server's
/// preference order. The same algorithm handles Accept-Language.
/// </summary>
public static class ContentNegotiation
{
    public sealed record MediaRange(string Type, string Subtype, double Quality)
    {
        public int Specificity => Type == "*" ? 0 : Subtype == "*" ? 1 : 2;
        public bool Matches(string type, string subtype) => (Type == "*" || Type == type) && (Subtype == "*" || Subtype == subtype);
    }

    public static IReadOnlyList<MediaRange> ParseAccept(string? header)
    {
        if (string.IsNullOrWhiteSpace(header)) return [new MediaRange("*", "*", 1)];
        return header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(part =>
        {
            var pieces = part.Split(';', StringSplitOptions.TrimEntries);
            var slash = pieces[0].IndexOf('/');
            var (type, subtype) = slash < 0 ? (pieces[0], "*") : (pieces[0][..slash], pieces[0][(slash + 1)..]);
            var q = pieces.Skip(1).Where(p => p.StartsWith("q=", StringComparison.OrdinalIgnoreCase))
                .Select(p => double.TryParse(p[2..], NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? Math.Clamp(v, 0, 1) : 1)
                .DefaultIfEmpty(1).First();
            return new MediaRange(type.ToLowerInvariant(), subtype.ToLowerInvariant(), q);
        }).ToList();
    }

    /// <returns>The best of the server's formats, or null meaning 406 Not Acceptable.</returns>
    public static string? Choose(string? acceptHeader, IReadOnlyList<string> available)
    {
        var ranges = ParseAccept(acceptHeader);
        return available
            .Select((format, preference) =>
            {
                var (type, subtype) = (format.Split('/')[0], format.Split('/')[1]);
                // The quality of a format comes from the most specific range that matches it.
                var range = ranges.Where(r => r.Matches(type, subtype)).MaxBy(r => r.Specificity);
                return (format, preference, Quality: range?.Quality ?? 0);
            })
            .Where(c => c.Quality > 0)
            .OrderByDescending(c => c.Quality)
            .ThenBy(c => c.preference)
            .Select(c => c.format)
            .FirstOrDefault();
    }

    /// <summary>Language negotiation with fallback from a region to its base language (fr-CA → fr).</summary>
    public static string ChooseLanguage(string? acceptLanguage, IReadOnlyList<string> supported, string fallback)
    {
        var wanted = (acceptLanguage ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part =>
            {
                var pieces = part.Split(";q=");
                return (Tag: pieces[0].ToLowerInvariant(), Q: pieces.Length > 1 && double.TryParse(pieces[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var q) ? q : 1);
            })
            .Where(w => w.Q > 0)
            .OrderByDescending(w => w.Q);

        foreach (var (tag, _) in wanted)
        {
            if (supported.FirstOrDefault(s => s.Equals(tag, StringComparison.OrdinalIgnoreCase)) is { } exact) return exact;
            var baseLanguage = tag.Split('-')[0];
            if (supported.FirstOrDefault(s => s.Split('-')[0].Equals(baseLanguage, StringComparison.OrdinalIgnoreCase)) is { } partial) return partial;
        }
        return fallback;
    }
}
