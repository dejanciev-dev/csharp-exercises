using CSharpExercises.Web;

namespace CSharpExercises.Tests;

public class WebTests
{
    private static readonly RouteMatcher Routes = new RouteMatcher()
        .Add("user", "/users/{id:int}")
        .Add("me", "/users/me")
        .Add("profile", "/users/{name}")
        .Add("posts", "/users/{id:int}/posts/{page:int?}")
        .Add("files", "/files/{*path}");

    [Theory]
    [InlineData("/users/42", "user")]
    [InlineData("/users/me", "me")]          // the literal beats {id:int} and {name}
    [InlineData("/users/ada", "profile")]    // not an int, falls through to {name}
    [InlineData("/users/42/posts", "posts")] // optional page omitted
    [InlineData("/users/42/posts/3?x=1", "posts")]
    [InlineData("/files/docs/2026/report.pdf", "files")]
    public void Routes_PickTheMostSpecificMatch(string path, string expected) => Assert.Equal(expected, Routes.Match(path)!.Name);

    [Fact]
    public void Routes_ExtractTypedValuesAndBuildLinks()
    {
        Assert.Equal(42, Routes.Match("/users/42")!.Values["id"]);
        Assert.Equal("docs/a b.txt", Routes.Match("/files/docs/a%20b.txt")!.Values["path"]);
        Assert.Null(Routes.Match("/unknown"));
        Assert.Null(Routes.Match("/users/42/posts/x"));

        Assert.Equal("/users/7/posts/2", Routes.Link("posts", new Dictionary<string, object> { ["id"] = 7, ["page"] = 2 }));
        Assert.Equal("/users/7/posts", Routes.Link("posts", new Dictionary<string, object> { ["id"] = 7 }));
        Assert.Equal("/users/a%2Fb", Routes.Link("profile", new Dictionary<string, object> { ["name"] = "a/b" }));
    }

    [Theory]
    [InlineData("text/html,application/json;q=0.9", "text/html")]
    [InlineData("application/json;q=0.9, text/html;q=0.8", "application/json")]
    [InlineData("text/*;q=0.5, application/json;q=0.4", "text/html")]
    [InlineData("*/*", "application/json")]                   // server preference breaks the tie
    [InlineData("text/html;q=0, */*;q=0.1", "application/json")] // explicitly refused
    [InlineData("image/png", null)]                            // 406
    [InlineData(null, "application/json")]
    public void ContentNegotiation_PicksTheBestFormat(string? accept, string? expected) =>
        Assert.Equal(expected, ContentNegotiation.Choose(accept, ["application/json", "text/html"]));

    [Fact]
    public void ContentNegotiation_Languages()
    {
        string[] supported = ["en", "fr-FR", "de"];
        Assert.Equal("fr-FR", ContentNegotiation.ChooseLanguage("fr-CA, en;q=0.8", supported, "en"));
        Assert.Equal("de", ContentNegotiation.ChooseLanguage("es;q=0.9, de;q=0.5", supported, "en"));
        Assert.Equal("en", ContentNegotiation.ChooseLanguage("ja", supported, "en"));
    }

    [Fact]
    public void ETags_SaveBandwidthAndPreventLostUpdates()
    {
        var resource = new ConditionalResource();
        var created = resource.Put("doc", "v1");
        Assert.Equal(201, created.Status);

        var get = resource.Get("doc");
        Assert.Equal(304, resource.Get("doc", ifNoneMatch: get.ETag).Status); // unchanged: no body sent

        // Alice and Bob both loaded v1; Alice saves first.
        Assert.Equal(200, resource.Put("doc", "alice's edit", ifMatch: get.ETag).Status);
        Assert.Equal(412, resource.Put("doc", "bob's edit", ifMatch: get.ETag).Status);
        Assert.Equal("alice's edit", resource.Get("doc").Body);

        Assert.Equal(428, resource.Put("doc", "blind write", requireIfMatch: true).Status);
        Assert.Equal(412, resource.Delete("doc", ifMatch: get.ETag).Status);
        Assert.Equal(204, resource.Delete("doc", ifMatch: resource.Get("doc").ETag).Status);
        Assert.Equal(404, resource.Get("doc").Status);
    }

    private sealed record Post(long Id, long CreatedAt);

    [Fact]
    public void Pagination_CursorsSurviveInsertsUnlikeOffsets()
    {
        var posts = Enumerable.Range(1, 10).Select(i => new Post(i, 1000 + i / 2)).ToList(); // duplicate timestamps on purpose

        var first = Pagination.Cursor(posts, p => p.CreatedAt, p => p.Id, null, 4);
        var offsetFirst = Pagination.Offset(posts.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id), 1, 4);

        posts.Insert(0, new Post(99, 900)); // a new item sorts before everything already shown

        var second = Pagination.Cursor(posts, p => p.CreatedAt, p => p.Id, first.NextCursor, 4);
        var offsetSecond = Pagination.Offset(posts.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id), 2, 4);

        Assert.Equal([1L, 2, 3, 4], first.Items.Select(p => p.Id));
        Assert.Equal([5L, 6, 7, 8], second.Items.Select(p => p.Id));      // continues exactly
        Assert.Contains(offsetFirst[^1], offsetSecond);                     // offset paging repeats item 4

        var third = Pagination.Cursor(posts, p => p.CreatedAt, p => p.Id, second.NextCursor, 4);
        Assert.Equal([9L, 10], third.Items.Select(p => p.Id));
        Assert.Null(third.NextCursor);
    }

    [Fact]
    public void Pagination_CursorsAreOpaqueAndValidated()
    {
        var cursor = Pagination.Encode(1500, 42);

        Assert.Equal((1500L, 42L), Pagination.Decode(cursor));
        Assert.Throws<ArgumentException>(() => Pagination.Decode("not-a-cursor!"));
        Assert.Equal($"</api/posts?cursor={cursor}>; rel=\"next\"", Pagination.LinkHeader("/api/posts", cursor));
    }

    [Fact]
    public void Cors_AllowsListedOriginsOnly()
    {
        var policy = new CorsPolicy
        {
            AllowedOrigins = new HashSet<string> { "https://app.example.com" },
            AllowedMethods = new HashSet<string> { "GET", "PUT" },
            AllowedHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Authorization" },
            AllowCredentials = true,
        };

        var ok = policy.ForRequest("https://app.example.com");
        Assert.Equal("https://app.example.com", ok["Access-Control-Allow-Origin"]);
        Assert.Equal("Origin", ok["Vary"]);
        Assert.Empty(policy.ForRequest("http://app.example.com")); // scheme differs: different origin

        var (status, headers) = policy.Preflight("https://app.example.com", "PUT", ["authorization", "content-type"]);
        Assert.Equal(204, status);
        Assert.Equal("GET, PUT", headers["Access-Control-Allow-Methods"]);
        Assert.Equal(403, policy.Preflight("https://app.example.com", "DELETE", []).Status);
    }

    [Fact]
    public void Cors_RulesAndPreflightDetection()
    {
        Assert.Throws<InvalidOperationException>(() => new CorsPolicy { AllowAnyOrigin = true, AllowCredentials = true }.ForRequest("https://x"));
        Assert.Equal("*", new CorsPolicy { AllowAnyOrigin = true }.ForRequest("https://x")["Access-Control-Allow-Origin"]);

        Assert.False(CorsPolicy.NeedsPreflight("POST", new Dictionary<string, string> { ["Content-Type"] = "text/plain" }));
        Assert.True(CorsPolicy.NeedsPreflight("POST", new Dictionary<string, string> { ["Content-Type"] = "application/json" }));
        Assert.True(CorsPolicy.NeedsPreflight("GET", new Dictionary<string, string> { ["Authorization"] = "Bearer x" }));
        Assert.True(CorsPolicy.NeedsPreflight("DELETE", new Dictionary<string, string>()));
    }

    [Fact]
    public void Cookies_ParseAndSerialize()
    {
        Assert.Equal(new Dictionary<string, string> { ["a"] = "1", ["b"] = "hello world" }, Cookies.ParseRequestHeader("a=1; b=hello%20world; a=2; junk"));

        var cookie = SetCookie.Session("sid", "abc123") with { MaxAge = TimeSpan.FromHours(1) };
        Assert.Equal("sid=abc123; Max-Age=3600; Path=/; Secure; HttpOnly; SameSite=Lax", cookie.ToString());
        Assert.Equal(cookie, Cookies.ParseSetCookie(cookie.ToString()));
        Assert.Throws<InvalidOperationException>(() => new SetCookie("x", "1") { SameSite = SameSite.None }.ToString());
    }

    [Fact]
    public void Cookies_MaxAgeBeatsExpires()
    {
        var now = new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
        var cookie = Cookies.ParseSetCookie("theme=dark; Expires=Wed, 21 Oct 2037 07:28:00 GMT; Max-Age=60; Domain=.example.com");

        Assert.Equal(now.AddSeconds(60), Cookies.ExpiresAt(cookie, now));
        Assert.Equal("example.com", cookie.Domain);
        Assert.Null(Cookies.ExpiresAt(Cookies.ParseSetCookie("s=1"), now)); // a session cookie
    }

    [Fact]
    public void Shortener_CreatesUnguessableCodesAndCounts()
    {
        var shortener = new UrlShortener();

        var a = shortener.Shorten("https://example.com/a");
        var b = shortener.Shorten("https://example.com/b");

        Assert.Equal(a, shortener.Shorten("https://example.com/a")); // idempotent
        Assert.NotEqual(a, b);
        Assert.True(Math.Abs(UrlShortener.FromBase62(a) - UrlShortener.FromBase62(b)) > 1000); // not sequential
        Assert.Equal("https://example.com/b", shortener.Resolve(b));
        shortener.Resolve(b);
        Assert.Equal(2, shortener.Clicks(b));
        Assert.Null(shortener.Resolve("nope"));
    }

    [Fact]
    public void Shortener_AliasesBase62AndScrambling()
    {
        var shortener = new UrlShortener();
        Assert.Equal("launch", shortener.Shorten("https://example.com/launch", alias: "launch"));
        Assert.Throws<InvalidOperationException>(() => shortener.Shorten("https://other.com", alias: "launch"));
        Assert.Throws<ArgumentException>(() => shortener.Shorten("javascript:alert(1)"));

        Assert.Equal("1C", UrlShortener.ToBase62(100));
        Assert.Equal(123456789, UrlShortener.FromBase62(UrlShortener.ToBase62(123456789)));
        Assert.Equal(10_000, Enumerable.Range(1, 10_000).Select(i => UrlShortener.Scramble(i)).Distinct().Count()); // no collisions
    }
}
