using System.Globalization;

namespace CSharpExercises.Web;

public enum SameSite { Strict, Lax, None }

public sealed record SetCookie(string Name, string Value)
{
    public DateTimeOffset? Expires { get; init; }
    public TimeSpan? MaxAge { get; init; }
    public string? Domain { get; init; }
    public string Path { get; init; } = "/";
    public bool Secure { get; init; }
    public bool HttpOnly { get; init; }
    public SameSite? SameSite { get; init; }

    /// <summary>A session cookie with the safe defaults: not readable by scripts, HTTPS only, not sent cross-site.</summary>
    public static SetCookie Session(string name, string value) =>
        new(name, value) { Secure = true, HttpOnly = true, SameSite = Web.SameSite.Lax };

    public override string ToString()
    {
        if (SameSite == Web.SameSite.None && !Secure) throw new InvalidOperationException("SameSite=None requires Secure.");
        var parts = new List<string> { $"{Name}={Uri.EscapeDataString(Value)}" };
        if (Expires is { } e) parts.Add($"Expires={e.UtcDateTime.ToString("R", CultureInfo.InvariantCulture)}");
        if (MaxAge is { } m) parts.Add($"Max-Age={(long)m.TotalSeconds}");
        if (Domain is not null) parts.Add($"Domain={Domain}");
        parts.Add($"Path={Path}");
        if (Secure) parts.Add("Secure");
        if (HttpOnly) parts.Add("HttpOnly");
        if (SameSite is { } s) parts.Add($"SameSite={s}");
        return string.Join("; ", parts);
    }
}

/// <summary>
/// Cookies in both directions: the browser's Cookie request header ("a=1; b=2") and the server's Set-Cookie response
/// header with its attributes. Parsing Set-Cookie is how an HTTP client (or a test) stores cookies; the security
/// attributes (HttpOnly, Secure, SameSite) are what keep session cookies from being stolen or sent cross-site.
/// </summary>
public static class Cookies
{
    public static IReadOnlyDictionary<string, string> ParseRequestHeader(string? header)
    {
        var cookies = new Dictionary<string, string>();
        foreach (var pair in (header ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = pair.IndexOf('=');
            if (eq <= 0) continue; // malformed pairs are ignored, as browsers do
            cookies.TryAdd(pair[..eq], Uri.UnescapeDataString(pair[(eq + 1)..].Trim('"'))); // the first occurrence wins
        }
        return cookies;
    }

    public static SetCookie ParseSetCookie(string header)
    {
        var parts = header.Split(';', StringSplitOptions.TrimEntries);
        var eq = parts[0].IndexOf('=');
        if (eq <= 0) throw new FormatException("A cookie needs a name.");
        var cookie = new SetCookie(parts[0][..eq], Uri.UnescapeDataString(parts[0][(eq + 1)..]));

        foreach (var attribute in parts.Skip(1))
        {
            var (key, value) = attribute.IndexOf('=') is var i and >= 0 ? (attribute[..i], attribute[(i + 1)..]) : (attribute, "");
            cookie = key.ToLowerInvariant() switch
            {
                "expires" => cookie with { Expires = DateTimeOffset.Parse(value, CultureInfo.InvariantCulture) },
                "max-age" => cookie with { MaxAge = TimeSpan.FromSeconds(long.Parse(value, CultureInfo.InvariantCulture)) },
                "domain" => cookie with { Domain = value.TrimStart('.') },
                "path" => cookie with { Path = value },
                "secure" => cookie with { Secure = true },
                "httponly" => cookie with { HttpOnly = true },
                "samesite" => cookie with { SameSite = Enum.Parse<SameSite>(value, ignoreCase: true) },
                _ => cookie, // unknown attributes are ignored
            };
        }
        return cookie;
    }

    /// <summary>Max-Age wins over Expires; neither means a session cookie (null).</summary>
    public static DateTimeOffset? ExpiresAt(SetCookie cookie, DateTimeOffset now) =>
        cookie.MaxAge is { } maxAge ? now + maxAge : cookie.Expires;
}
