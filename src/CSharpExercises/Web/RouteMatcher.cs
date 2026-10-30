using System.Globalization;

namespace CSharpExercises.Web;

public sealed record RouteMatch(string Name, IReadOnlyDictionary<string, object> Values);

/// <summary>
/// URL routing as web frameworks do it: templates like "/users/{id:int}/posts/{slug}" with typed constraints, optional
/// parameters ("{page?}") and a catch-all ("{*path}"). When several routes match, the most specific wins: literal
/// segments beat parameters, and constrained parameters beat unconstrained ones, so "/users/me" isn't swallowed by
/// "/users/{id}".
/// </summary>
public sealed class RouteMatcher
{
    private sealed record Segment(string? Literal, string? Parameter, string? Constraint, bool Optional, bool CatchAll);
    private sealed record Route(string Name, string Template, Segment[] Segments, int Specificity);

    private readonly List<Route> _routes = [];

    public RouteMatcher Add(string name, string template)
    {
        var segments = Split(template).Select(ParseSegment).ToArray();
        // Specificity: literal 3, constrained parameter 2, parameter 1, catch-all 0, per segment.
        var specificity = segments.Sum(s => s.Literal is not null ? 3 : s.CatchAll ? 0 : s.Constraint is not null ? 2 : 1);
        _routes.Add(new Route(name, template, segments, specificity));
        return this;
    }

    public RouteMatch? Match(string path)
    {
        var parts = Split(path.Split('?')[0]).Select(Uri.UnescapeDataString).ToArray();
        return _routes
            .OrderByDescending(r => r.Specificity)
            .Select(r => TryMatch(r, parts) is { } values ? new RouteMatch(r.Name, values) : null)
            .FirstOrDefault(m => m is not null);
    }

    /// <summary>The reverse: build a URL for a route from values, escaping them.</summary>
    public string Link(string name, IReadOnlyDictionary<string, object> values)
    {
        var route = _routes.Single(r => r.Name == name);
        var parts = route.Segments
            .Where(s => s.Literal is not null || values.ContainsKey(s.Parameter!) || !s.Optional)
            .Select(s => s.Literal ?? (values.TryGetValue(s.Parameter!, out var v)
                ? s.CatchAll ? Convert.ToString(v, CultureInfo.InvariantCulture)! : Uri.EscapeDataString(Convert.ToString(v, CultureInfo.InvariantCulture)!)
                : throw new ArgumentException($"Missing value for '{s.Parameter}'.")));
        return "/" + string.Join('/', parts);
    }

    private static Dictionary<string, object>? TryMatch(Route route, string[] parts)
    {
        var values = new Dictionary<string, object>();
        for (var i = 0; i < route.Segments.Length; i++)
        {
            var segment = route.Segments[i];
            if (segment.CatchAll)
            {
                values[segment.Parameter!] = string.Join('/', parts.Skip(i));
                return values;
            }
            if (i >= parts.Length) return segment.Optional && route.Segments.Skip(i).All(s => s.Optional) ? values : null;

            if (segment.Literal is not null)
            {
                if (!segment.Literal.Equals(parts[i], StringComparison.OrdinalIgnoreCase)) return null;
                continue;
            }
            if (ConvertValue(parts[i], segment.Constraint) is not { } value) return null;
            values[segment.Parameter!] = value;
        }
        return parts.Length == route.Segments.Length ? values : null;
    }

    private static object? ConvertValue(string text, string? constraint) => constraint switch
    {
        null => text,
        "int" => int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var i) ? i : null,
        "guid" => Guid.TryParse(text, out var g) ? g : null,
        "alpha" => text.All(char.IsAsciiLetter) ? text : null,
        _ => throw new FormatException($"Unknown constraint '{constraint}'."),
    };

    private static Segment ParseSegment(string s)
    {
        if (!s.StartsWith('{')) return new Segment(s, null, null, false, false);
        var inner = s.Trim('{', '}');
        var catchAll = inner.StartsWith('*');
        var optional = inner.EndsWith('?');
        inner = inner.TrimStart('*').TrimEnd('?');
        var colon = inner.IndexOf(':');
        return colon < 0 ? new Segment(null, inner, null, optional, catchAll) : new Segment(null, inner[..colon], inner[(colon + 1)..], optional, catchAll);
    }

    private static string[] Split(string path) => path.Split('/', StringSplitOptions.RemoveEmptyEntries);
}
