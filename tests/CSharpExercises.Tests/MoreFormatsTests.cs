using CSharpExercises.Formats;

namespace CSharpExercises.Tests;

public class MoreFormatsTests
{
    private static readonly FixedWidth Payroll = new([
        new FieldSpec("id", 0, 6, '0', RightAlign: true),
        new FieldSpec("name", 6, 12),
        new FieldSpec("amount", 18, 9, '0', RightAlign: true),
    ]);

    [Fact]
    public void FixedWidth_ParsesAndFormats()
    {
        const string line = "000042Ada Lovelace000123450";

        var record = Payroll.Parse(line);

        Assert.Equal(("42", "Ada Lovelace", "123450"), (record["id"], record["name"], record["amount"]));
        Assert.Equal(1234.50m, FixedWidth.ParseImpliedDecimal(line[18..27], 2));
        Assert.Equal(line, Payroll.Format(record));
        Assert.Equal(27, Payroll.RecordLength);
        Assert.Throws<FormatException>(() => Payroll.Parse("too short"));
        Assert.Throws<ArgumentException>(() => Payroll.Format(new Dictionary<string, string> { ["name"] = "A name far too long" }));
    }

    [Fact]
    public void FixedWidth_ImpliedDecimalsAndOverlaps()
    {
        Assert.Equal("000012345", FixedWidth.FormatImpliedDecimal(123.45m, 2, 9));
        Assert.Equal("00012345-", FixedWidth.FormatImpliedDecimal(-123.45m, 2, 9));
        Assert.Equal(-123.45m, FixedWidth.ParseImpliedDecimal("00012345-", 2));
        Assert.Equal("0", Payroll.Parse("000000" + new string(' ', 12) + "000000000")["id"]);
        Assert.Throws<ArgumentException>(() => new FixedWidth([new("a", 0, 5), new("b", 3, 5)]));
    }

    [Fact]
    public void Logfmt_ParsesQuotesAndFlags()
    {
        var pairs = Logfmt.Parse("level=info msg=\"user logged in\" user_id=42 admin path=/a=b note=\"say \\\"hi\\\"\"");

        Assert.Equal(
            [("level", "info"), ("msg", "user logged in"), ("user_id", "42"), ("admin", "true"), ("path", "/a=b"), ("note", "say \"hi\"")],
            pairs.Select(p => (p.Key, p.Value)));
        Assert.Throws<FormatException>(() => Logfmt.Parse("msg=\"unterminated"));
    }

    [Fact]
    public void Logfmt_RoundTrips()
    {
        var line = Logfmt.Format([new("at", "error"), new("msg", "disk full"), new("free", 0.5), new("retry", true), new("empty", ""), new("multi", "a\nb")]);

        Assert.Equal("at=error msg=\"disk full\" free=0.5 retry=true empty=\"\" multi=\"a\\nb\"", line);
        Assert.Equal(["error", "disk full", "0.5", "true", "", "a\nb"], Logfmt.Parse(line).Select(p => p.Value));
    }

    private sealed record Event(int Id, string Name);

    [Fact]
    public async Task NdJson_StreamsLineByLine()
    {
        var stream = new MemoryStream();
        await NdJson.WriteAsync(stream, NdJson.ToAsync(Enumerable.Range(1, 3).Select(i => new Event(i, $"event {i}"))));

        var text = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("{\"id\":1,\"name\":\"event 1\"}\n{\"id\":2,\"name\":\"event 2\"}\n{\"id\":3,\"name\":\"event 3\"}\n", text);

        stream.Position = 0;
        var read = new List<Event>();
        await foreach (var e in NdJson.ReadAsync<Event>(stream)) read.Add(e);
        Assert.Equal([1, 2, 3], read.Select(e => e.Id));
    }

    [Fact]
    public async Task NdJson_SkipsAndReportsBadLines()
    {
        var stream = new MemoryStream("{\"id\":1,\"name\":\"ok\"}\n{broken\n\n{\"id\":2,\"name\":\"ok\"}\n"u8.ToArray());
        var errors = new List<int>();

        var read = new List<Event>();
        await foreach (var e in NdJson.ReadAsync<Event>(stream, (line, _) => errors.Add(line))) read.Add(e);

        Assert.Equal([1, 2], read.Select(e => e.Id));
        Assert.Equal([2], errors);

        stream.Position = 0;
        await Assert.ThrowsAnyAsync<System.Text.Json.JsonException>(async () =>
        {
            await foreach (var _ in NdJson.ReadAsync<Event>(stream)) { }
        });
    }

    private static readonly FeedItem[] Items =
    [
        new("First post", "https://example.com/1", new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero), "Hello & welcome"),
        new("Second <post>", "https://example.com/2", new DateTimeOffset(2026, 10, 15, 9, 0, 0, TimeSpan.Zero)),
    ];

    [Fact]
    public void Feeds_RssRoundTripsAndEscapes()
    {
        var rss = RssFeed.ToRss("My blog", "https://example.com", Items);

        Assert.Contains("<title>Second &lt;post&gt;</title>", rss);
        Assert.Contains("<pubDate>Thu, 15 Oct 2026 09:00:00 GMT</pubDate>", rss);
        Assert.Equal(Items.Reverse(), RssFeed.Parse(rss)); // newest first
    }

    [Fact]
    public void Feeds_AtomRoundTripsAndUnknownFormatsFail()
    {
        var atom = RssFeed.ToAtom("My blog", "urn:example:blog", Items);

        Assert.Contains("xmlns=\"http://www.w3.org/2005/Atom\"", atom);
        Assert.Equal(Items.Reverse(), RssFeed.Parse(atom));
        Assert.Throws<FormatException>(() => RssFeed.Parse("<html/>"));
    }
}
