namespace CSharpExercises.Web;

/// <summary>
/// Cross-origin resource sharing from the server's side. Browsers block scripts on one origin from reading responses from
/// another unless the server opts in with Access-Control-* headers. Requests with custom headers or methods other than
/// GET/HEAD/POST are "preflighted" with an OPTIONS request first. Two rules people get wrong: an origin must match
/// exactly (scheme, host and port), and a wildcard origin can't be combined with credentials.
/// </summary>
public sealed class CorsPolicy
{
    public IReadOnlySet<string> AllowedOrigins { get; init; } = new HashSet<string>();
    public bool AllowAnyOrigin { get; init; }
    public IReadOnlySet<string> AllowedMethods { get; init; } = new HashSet<string> { "GET", "HEAD", "POST" };
    public IReadOnlySet<string> AllowedHeaders { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public bool AllowCredentials { get; init; }
    public TimeSpan MaxAge { get; init; } = TimeSpan.FromMinutes(10);

    public void Validate()
    {
        if (AllowAnyOrigin && AllowCredentials)
            throw new InvalidOperationException("Browsers reject Access-Control-Allow-Origin: * together with credentials.");
    }

    /// <summary>Headers for an actual (non-preflight) request, or none when the origin isn't allowed.</summary>
    public IReadOnlyDictionary<string, string> ForRequest(string? origin)
    {
        Validate();
        if (origin is null || !IsAllowed(origin)) return new Dictionary<string, string>();
        var headers = new Dictionary<string, string>
        {
            ["Access-Control-Allow-Origin"] = AllowAnyOrigin ? "*" : origin,
        };
        if (!AllowAnyOrigin) headers["Vary"] = "Origin"; // caches must not reuse this answer for other origins
        if (AllowCredentials) headers["Access-Control-Allow-Credentials"] = "true";
        return headers;
    }

    public (int Status, IReadOnlyDictionary<string, string> Headers) Preflight(string? origin, string requestedMethod, IEnumerable<string> requestedHeaders)
    {
        var headers = new Dictionary<string, string>(ForRequest(origin));
        if (headers.Count == 0) return (403, headers);

        var method = requestedMethod.ToUpperInvariant();
        var wantedHeaders = requestedHeaders.ToList();
        if (!AllowedMethods.Contains(method) || wantedHeaders.Any(h => !AllowedHeaders.Contains(h) && !IsSafelisted(h)))
            return (403, new Dictionary<string, string>());

        headers["Access-Control-Allow-Methods"] = string.Join(", ", AllowedMethods.Order());
        if (wantedHeaders.Count > 0) headers["Access-Control-Allow-Headers"] = string.Join(", ", wantedHeaders);
        headers["Access-Control-Max-Age"] = ((int)MaxAge.TotalSeconds).ToString();
        return (204, headers);
    }

    /// <summary>Simple requests (no preflight): GET/HEAD/POST with only safelisted headers and form-like content types.</summary>
    public static bool NeedsPreflight(string method, IReadOnlyDictionary<string, string> headers) =>
        method.ToUpperInvariant() is not ("GET" or "HEAD" or "POST") ||
        headers.Any(h => !IsSafelisted(h.Key)) ||
        (headers.TryGetValue("Content-Type", out var type) &&
         type.Split(';')[0].Trim().ToLowerInvariant() is not ("application/x-www-form-urlencoded" or "multipart/form-data" or "text/plain"));

    private bool IsAllowed(string origin) => AllowAnyOrigin || AllowedOrigins.Contains(origin);

    private static bool IsSafelisted(string header) =>
        header.ToLowerInvariant() is "accept" or "accept-language" or "content-language" or "content-type";
}
