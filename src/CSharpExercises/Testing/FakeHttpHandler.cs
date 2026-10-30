using System.Net;
using System.Text;

namespace CSharpExercises.Testing;

/// <summary>
/// Testing code that uses <see cref="HttpClient"/> without a network: HttpClient delegates every request to an
/// <see cref="HttpMessageHandler"/>, so a fake handler can answer from canned rules, simulate failures and timeouts, and
/// record what was sent for assertions. No mocking library needed, and the code under test stays unchanged.
/// </summary>
public sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly List<(Func<HttpRequestMessage, bool> Matches, Func<HttpRequestMessage, Task<HttpResponseMessage>> Respond)> _rules = [];

    public List<(HttpMethod Method, string Url, string? Body)> Requests { get; } = [];

    public FakeHttpHandler When(HttpMethod method, string pathAndQuery, HttpStatusCode status, string body = "", string mediaType = "application/json") =>
        When(r => r.Method == method && r.RequestUri!.PathAndQuery == pathAndQuery,
             _ => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, mediaType) }));

    public FakeHttpHandler When(Func<HttpRequestMessage, bool> matches, Func<HttpRequestMessage, Task<HttpResponseMessage>> respond)
    {
        _rules.Add((matches, respond));
        return this;
    }

    /// <summary>Responds with each status in turn, e.g. 503, 503, 200 to test retries.</summary>
    public FakeHttpHandler Sequence(string pathAndQuery, params HttpStatusCode[] statuses)
    {
        var call = 0;
        return When(r => r.RequestUri!.PathAndQuery == pathAndQuery,
            _ => Task.FromResult(new HttpResponseMessage(statuses[Math.Min(call++, statuses.Length - 1)])));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        lock (Requests) Requests.Add((request.Method, request.RequestUri!.ToString(), body));

        // The most recently added matching rule wins, so tests can override defaults.
        for (var i = _rules.Count - 1; i >= 0; i--)
            if (_rules[i].Matches(request)) return await _rules[i].Respond(request);

        return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent($"No fake rule for {request.Method} {request.RequestUri}") };
    }
}

/// <summary>A tiny client to test: fetches a value, retrying on 503.</summary>
public sealed class WeatherClient(HttpClient http)
{
    public async Task<string> TemperatureAsync(string city, int attempts = 3)
    {
        for (var attempt = 1; ; attempt++)
        {
            using var response = await http.GetAsync($"/weather/{Uri.EscapeDataString(city)}");
            if (response.StatusCode == HttpStatusCode.ServiceUnavailable && attempt < attempts) continue;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
