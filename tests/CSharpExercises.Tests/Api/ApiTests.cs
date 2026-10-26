using System.Net;
using System.Net.Http.Json;
using CSharpExercises.Api.Endpoints;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace CSharpExercises.Tests.Api;

/// <summary>Starts the real app in memory; the rate limit is raised so ordinary tests never hit it.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("RateLimit:MaxRequests", "100000");
}

/// <summary>
/// Integration tests: real HTTP requests through routing, model binding, validation and JSON,
/// without opening a network port.
/// </summary>
public class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static async Task<ValidationProblemDetails> ReadProblem(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        return (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!;
    }

    [Fact]
    public async Task Health_IsOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiDocument_DescribesTheEndpoints()
    {
        var json = await _client.GetStringAsync("/openapi/v1.json");

        Assert.Contains("/api/sort", json);
        Assert.Contains("/api/routes/shortest", json);
    }

    [Theory]
    [InlineData("quick")]
    [InlineData("Merge")]
    [InlineData("bubble")]
    public async Task Sort_SortsWithTheChosenAlgorithm(string algorithm)
    {
        var response = await _client.PostAsJsonAsync("/api/sort", new SortRequest(algorithm, [5, 2, 9, 1, 5]));
        var result = await response.Content.ReadFromJsonAsync<SortResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([1, 2, 5, 5, 9], result!.Sorted);
        Assert.Equal(algorithm.ToLowerInvariant(), result.Algorithm);
    }

    [Fact]
    public async Task Sort_RejectsUnknownAlgorithm()
    {
        var problem = await ReadProblem(await _client.PostAsJsonAsync("/api/sort", new SortRequest("bogo", [1])));

        Assert.Contains("bubble, merge, quick", problem.Errors["algorithm"][0]);
    }

    [Fact]
    public async Task Sort_LimitsBubbleSortInput()
    {
        var numbers = Enumerable.Range(0, AlgorithmEndpoints.MaxBubbleSortItems + 1).ToArray();

        var tooMany = await _client.PostAsJsonAsync("/api/sort", new SortRequest("bubble", numbers));
        var quickIsFine = await _client.PostAsJsonAsync("/api/sort", new SortRequest("quick", numbers));

        Assert.Contains("numbers", (await ReadProblem(tooMany)).Errors.Keys);
        Assert.Equal(HttpStatusCode.OK, quickIsFine.StatusCode);
    }

    private static readonly Edge[] Roads =
    [
        new("A", "B", 4), new("A", "C", 2), new("C", "B", 1),
        new("B", "D", 5), new("D", "E", 2), new("C", "E", 10)
    ];

    [Fact]
    public async Task ShortestPath_ReturnsCheapestRoute()
    {
        var response = await _client.PostAsJsonAsync("/api/routes/shortest", new ShortestPathRequest(Roads, "A", "E"));
        var route = await response.Content.ReadFromJsonAsync<ShortestPathResponse>();

        Assert.Equal(10, route!.Distance);
        Assert.Equal(["A", "C", "B", "D", "E"], route.Path);
    }

    [Fact]
    public async Task ShortestPath_UnreachableIs404()
    {
        var response = await _client.PostAsJsonAsync("/api/routes/shortest", new ShortestPathRequest(Roads, "E", "A"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("No route", (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Title);
    }

    [Fact]
    public async Task ShortestPath_RejectsNegativeWeights()
    {
        Edge[] edges = [new("A", "B", -1)];

        var problem = await ReadProblem(await _client.PostAsJsonAsync("/api/routes/shortest", new ShortestPathRequest(edges, "A", "B")));

        Assert.Contains("negative", problem.Errors["edges"][0]);
    }

    [Fact]
    public async Task Coins_ReturnsMinimumAndCombinations()
    {
        var result = await _client.GetFromJsonAsync<CoinChangeResponse>("/api/coins?amount=11&coins=1&coins=2&coins=5");

        Assert.Equal(3, result!.MinCoins);
        Assert.Equal(11, result.Ways);
    }

    [Fact]
    public async Task Coins_ImpossibleAmountHasNoMinimum()
    {
        var result = await _client.GetFromJsonAsync<CoinChangeResponse>("/api/coins?amount=3&coins=2");

        Assert.Null(result!.MinCoins);
        Assert.Equal(0, result.Ways);
    }

    [Fact]
    public async Task Coins_TooManyCombinationsIsAClearError()
    {
        var coins = string.Join("&", Enumerable.Range(1, 100).Select(c => $"coins={c}"));

        var problem = await ReadProblem(await _client.GetAsync($"/api/coins?amount=10000&{coins}"));

        Assert.Contains("too many combinations", problem.Errors["amount"][0]);
    }

    [Fact]
    public async Task Text_EditDistanceAndLcs()
    {
        var distance = await _client.GetFromJsonAsync<EditDistanceResponse>("/api/text/edit-distance?source=kitten&target=sitting");
        var lcs = await _client.GetFromJsonAsync<CommonSubsequenceResponse>("/api/text/lcs?a=AGGTAB&b=GXTXAYB");

        Assert.Equal(3, distance!.Distance);
        Assert.Equal("GTAB", lcs!.Subsequence);
    }

    [Fact]
    public async Task Text_Palindrome()
    {
        var result = await _client.GetFromJsonAsync<PalindromeResponse>(
            "/api/text/palindrome?text=" + Uri.EscapeDataString("A man, a plan, a canal: Panama"));

        Assert.True(result!.IsPalindrome);
    }

    [Fact]
    public async Task Text_MissingParameterIs400()
    {
        var response = await _client.GetAsync("/api/text/edit-distance?source=kitten");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Text_RejectsVeryLongInput()
    {
        var longText = new string('a', TextEndpoints.MaxLength + 1);

        var problem = await ReadProblem(await _client.GetAsync($"/api/text/lcs?a={longText}&b=abc"));

        Assert.Contains("a", problem.Errors.Keys);
    }

    [Fact]
    public async Task Shapes_ListAndCreate()
    {
        var names = await _client.GetFromJsonAsync<string[]>("/api/shapes");
        var response = await _client.PostAsJsonAsync("/api/shapes", new ShapeRequest("rectangle 3 4.5"));
        var shape = await response.Content.ReadFromJsonAsync<ShapeResponse>();

        Assert.Equal(["circle", "rectangle", "square"], names!);
        Assert.Equal(new ShapeResponse("Rectangle", 13.5, 15), shape);
    }

    [Theory]
    [InlineData("hexagon 2")]
    [InlineData("circle -1")]
    [InlineData("square")]
    public async Task Shapes_BadDescriptionIs400(string description)
    {
        var problem = await ReadProblem(await _client.PostAsJsonAsync("/api/shapes", new ShapeRequest(description)));

        Assert.DoesNotContain("(Parameter", problem.Errors["description"][0]);
    }
}

/// <summary>The rate limiter tested end to end, with a fake clock so the test never waits a real minute.</summary>
public class RateLimitTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task ApiCalls_AreLimitedPerClient_AndRecoverAfterTheWindow()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var limited = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimit:MaxRequests", "3");
            builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(time));
        });
        var client = limited.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/shapes")).StatusCode);
        }

        var blocked = await client.GetAsync("/api/shapes");
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(60), blocked.Headers.RetryAfter?.Delta);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode); // only /api is limited

        time.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/shapes")).StatusCode);
    }
}
