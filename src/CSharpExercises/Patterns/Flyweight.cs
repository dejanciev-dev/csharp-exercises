namespace CSharpExercises.Patterns;

/// <summary>Intrinsic state: shared and immutable, the same for every use of this style.</summary>
public sealed record TextStyle(string Font, int Size, bool Bold, string Color);

/// <summary>
/// Flyweight: when millions of objects share most of their data, store the shared part once and keep only the varying
/// part per object. A document with a million characters needs a handful of distinct styles; each character stores its
/// letter and a reference to a shared style instead of its own copy. The factory guarantees one instance per distinct
/// style. String interning is the same idea built into .NET.
/// </summary>
public sealed class StyleFactory
{
    private readonly Dictionary<TextStyle, TextStyle> _cache = new();

    public int DistinctStyles => _cache.Count;

    public TextStyle Get(string font, int size, bool bold, string color)
    {
        var candidate = new TextStyle(font, size, bold, color);
        if (_cache.TryGetValue(candidate, out var shared)) return shared;
        _cache[candidate] = candidate;
        return candidate;
    }
}

/// <summary>Extrinsic state: per character, just the letter and which shared style it uses.</summary>
public readonly record struct StyledChar(char Value, TextStyle Style);

public sealed class StyledDocument(StyleFactory styles)
{
    private readonly List<StyledChar> _chars = [];

    public int Length => _chars.Count;

    public void Append(string text, string font, int size, bool bold = false, string color = "black")
    {
        var style = styles.Get(font, size, bold, color); // reused, not reallocated, for every character
        foreach (var c in text) _chars.Add(new StyledChar(c, style));
    }

    public int DistinctStyleObjects => _chars.Select(c => c.Style).Distinct(ReferenceEqualityComparer.Instance).Count();

    public string PlainText => new(_chars.Select(c => c.Value).ToArray());
}
