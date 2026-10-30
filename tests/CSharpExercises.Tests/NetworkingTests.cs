using System.Text;
using CSharpExercises.Networking;

namespace CSharpExercises.Tests;

public class NetworkingTests
{
    [Fact]
    public void Cidr_ParsesAndTestsMembership()
    {
        var net = Ipv4Network.Parse("192.168.1.0/24");

        Assert.True(net.Contains("192.168.1.200"));
        Assert.False(net.Contains("192.168.2.1"));
        Assert.Equal("192.168.1.255", Ipv4Network.ToAddress(net.Broadcast));
        Assert.Equal(254, net.UsableHosts);
        Assert.Equal(2, Ipv4Network.Parse("10.0.0.0/31").UsableHosts);
        Assert.True(Ipv4Network.Parse("10.0.0.0/8").Contains(Ipv4Network.Parse("10.20.0.0/16")));
        Assert.True(Ipv4Network.Parse("0.0.0.0/0").Contains("8.8.8.8"));

        var error = Assert.Throws<FormatException>(() => Ipv4Network.Parse("192.168.1.5/24"));
        Assert.Contains("192.168.1.0/24", error.Message);
        Assert.Throws<FormatException>(() => Ipv4Network.Parse("300.1.1.1/8"));
    }

    [Fact]
    public void Cidr_SubnetsAndSummarises()
    {
        Assert.Equal(["10.0.0.0/26", "10.0.0.64/26", "10.0.0.128/26", "10.0.0.192/26"],
            Ipv4Network.Parse("10.0.0.0/24").Subnets(26).Select(n => n.ToString()));

        Assert.Equal(["10.0.0.5/32", "10.0.0.6/31", "10.0.0.8/29", "10.0.0.16/30", "10.0.0.20/32"],
            Ipv4Network.Summarise("10.0.0.5", "10.0.0.20").Select(n => n.ToString()));
        Assert.Equal(["0.0.0.0/0"], Ipv4Network.Summarise("0.0.0.0", "255.255.255.255").Select(n => n.ToString()));
    }

    [Fact]
    public void Routing_LongestPrefixWins()
    {
        var table = new RoutingTable<string>();
        table.Add("0.0.0.0/0", "internet");
        table.Add("10.0.0.0/8", "corp");
        table.Add("10.1.0.0/16", "branch");
        table.Add("10.1.2.0/24", "lab");

        string Route(string ip) => table.TryLookup(ip, out var hop) ? hop : "none";

        Assert.Equal("lab", Route("10.1.2.3"));
        Assert.Equal("branch", Route("10.1.9.9"));
        Assert.Equal("corp", Route("10.200.0.1"));
        Assert.Equal("internet", Route("8.8.8.8"));

        Assert.True(table.Remove("10.1.0.0/16"));
        Assert.Equal("corp", Route("10.1.9.9"));
        Assert.Equal(3, table.Count);
    }

    [Fact]
    public void Checksum_MatchesTheRfcExample()
    {
        // RFC 1071 section 3: 00 01 f2 03 f4 f5 f6 f7 sums to ddf2, so the checksum is 220d.
        Assert.Equal(0x220D, InternetChecksum.Compute([0x00, 0x01, 0xF2, 0x03, 0xF4, 0xF5, 0xF6, 0xF7]));
        Assert.Equal(InternetChecksum.Compute([0xAB, 0x00]), InternetChecksum.Compute([0xAB])); // odd length is zero-padded
    }

    [Fact]
    public void Checksum_VerifiesAndUpdatesHeaders()
    {
        var header = InternetChecksum.Ipv4Header("192.168.0.1", "192.168.0.199", ttl: 64, protocol: 17, totalLength: 60);
        Assert.True(InternetChecksum.Verify(header));

        // A router decrements the TTL (high byte of the word at offset 8) and patches the checksum incrementally.
        var oldWord = (ushort)(header[8] << 8 | header[9]);
        header[8]--;
        var newWord = (ushort)(header[8] << 8 | header[9]);
        var patched = InternetChecksum.Update((ushort)(header[10] << 8 | header[11]), oldWord, newWord);
        header[10] = (byte)(patched >> 8);
        header[11] = (byte)patched;

        Assert.True(InternetChecksum.Verify(header));
        header[15] ^= 1;
        Assert.False(InternetChecksum.Verify(header)); // any corruption is noticed
    }

    [Fact]
    public void Http_ParsesHeadersAndContentLength()
    {
        var raw = Encoding.ASCII.GetBytes("POST /api/items?x=1 HTTP/1.1\r\nHost: example.com\r\nAccept: text/html\r\naccept: application/json\r\nContent-Length: 5\r\n\r\nhelloNEXT");

        var (request, consumed) = HttpMessageParser.TryParse(raw)!.Value;

        Assert.Equal(("POST", "/api/items?x=1", "HTTP/1.1"), (request.Method, request.Target, request.Version));
        Assert.Equal("text/html, application/json", request.Header("ACCEPT"));
        Assert.Equal("hello", Encoding.ASCII.GetString(request.Body));
        Assert.Equal(raw.Length - 4, consumed); // "NEXT" belongs to the next request on the connection
    }

    [Fact]
    public void Http_WaitsForMoreDataAndDecodesChunks()
    {
        var full = Encoding.ASCII.GetBytes("PUT /f HTTP/1.1\r\nTransfer-Encoding: chunked\r\n\r\n4\r\nWiki\r\n6;ext=1\r\npedia \r\n0\r\n\r\n");

        for (var cut = 0; cut < full.Length; cut++) Assert.Null(HttpMessageParser.TryParse(full.AsSpan(0, cut)));
        var (request, consumed) = HttpMessageParser.TryParse(full)!.Value;

        Assert.Equal("Wikipedia ", Encoding.ASCII.GetString(request.Body));
        Assert.Equal(full.Length, consumed);
    }

    [Theory]
    [InlineData("GET /\r\n\r\n")]
    [InlineData("GET / HTTP/1.1\r\nBad Header: x\r\n\r\n")]
    [InlineData("POST / HTTP/1.1\r\nContent-Length: 3\r\nContent-Length: 4\r\n\r\nabcd")]
    [InlineData("POST / HTTP/1.1\r\nContent-Length: 4\r\nTransfer-Encoding: chunked\r\n\r\n0\r\n\r\n")]
    public void Http_RejectsMalformedAndAmbiguousRequests(string raw) =>
        Assert.Throws<InvalidDataException>(() => HttpMessageParser.TryParse(Encoding.ASCII.GetBytes(raw)));

    [Fact]
    public void Http_LimitsHeaderSize()
    {
        var huge = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nX: " + new string('a', 10_000));
        Assert.Throws<InvalidDataException>(() => HttpMessageParser.TryParse(huge));
    }

    [Fact]
    public void Backoff_JitterFlattensTheRetrySpike()
    {
        var baseDelay = TimeSpan.FromMilliseconds(100);
        var cap = TimeSpan.FromSeconds(10);

        Assert.Equal([100.0, 200, 400, 800, 1600], Backoff.Exponential(baseDelay, cap, 5).Select(d => d.TotalMilliseconds));
        Assert.Equal(10_000, Backoff.Exponential(baseDelay, cap, 20).Last().TotalMilliseconds);

        var plain = Backoff.PeakLoad(1000, _ => Backoff.Exponential(baseDelay, cap, 5));
        var full = Backoff.PeakLoad(1000, r => Backoff.FullJitter(baseDelay, cap, 5, r));
        var decorrelated = Backoff.PeakLoad(1000, r => Backoff.DecorrelatedJitter(baseDelay, cap, 5, r));

        Assert.Equal(1000, plain);         // every client retries in the same 10 ms
        Assert.True(full < plain / 3);
        Assert.True(decorrelated < plain / 3);
        Assert.All(Backoff.EqualJitter(baseDelay, cap, 5, new Random(1)).Zip(Backoff.Exponential(baseDelay, cap, 5)),
            p => Assert.InRange(p.First, p.Second / 2, p.Second));
    }

    [Theory]
    [InlineData("/users/{id}", "/users/42")]
    [InlineData("/search{?q,page,missing}", "/search?q=caf%C3%A9%20au%20lait&page=2")]
    [InlineData("/files{/path}", "/files/a%2Fb")]
    [InlineData("{+base}/x", "http://example.com/api/x")]
    [InlineData("/tags/{tags}", "/tags/red,green")]
    [InlineData("/doc{#section}", "/doc#intro")]
    [InlineData("api{.format}", "api.json")]
    [InlineData("/list?x=1{&page}", "/list?x=1&page=2")]
    public void UriTemplate_Expands(string template, string expected)
    {
        var variables = new Dictionary<string, object?>
        {
            ["id"] = 42, ["q"] = "café au lait", ["page"] = 2, ["path"] = "a/b", ["base"] = "http://example.com/api",
            ["tags"] = new[] { "red", "green" }, ["section"] = "intro", ["format"] = "json", ["missing"] = null,
        };

        Assert.Equal(expected, UriTemplate.Expand(template, variables));
    }
}
