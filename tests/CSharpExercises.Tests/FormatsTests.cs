using CSharpExercises.Formats;

namespace CSharpExercises.Tests;

public class FormatsTests
{
    [Fact]
    public void Csv_HandlesQuotesCommasAndNewlines()
    {
        const string text = "name,quote\r\n\"Smith, J\",\"He said \"\"hi\"\"\"\n\"multi\nline\",x\n,\n";

        var rows = Csv.Parse(text).ToArray();

        Assert.Equal(4, rows.Length);
        Assert.Equal(["Smith, J", "He said \"hi\""], rows[1]);
        Assert.Equal(["multi\nline", "x"], rows[2]);
        Assert.Equal(["", ""], rows[3]);
    }

    [Fact]
    public void Csv_RoundTripsAndValidates()
    {
        string[] fields = ["plain", "with,comma", "with \"quotes\"", "two\nlines", ""];

        var line = Csv.FormatRecord(fields);

        Assert.Equal("plain,\"with,comma\",\"with \"\"quotes\"\"\",\"two\nlines\",", line);
        Assert.Equal(fields, Csv.Parse(line).Single());
        Assert.Throws<FormatException>(() => Csv.Parse("\"open").ToArray());
        Assert.Throws<FormatException>(() => Csv.Parse("ab\"c").ToArray());
        Assert.Equal(["a", "b"], Csv.Parse("a;b", ';').Single());
    }

    [Fact]
    public void Csv_MapsRowsByHeader()
    {
        var rows = Csv.ParseWithHeaders("id,name\n1,Ada\n2,Grace").ToArray();

        Assert.Equal("Grace", rows[1]["name"]);
        Assert.Throws<FormatException>(() => Csv.ParseWithHeaders("a,b\n1").ToArray());
    }

    [Fact]
    public void Ini_ParsesSectionsCommentsAndReferences()
    {
        var ini = IniFile.Parse("""
            ; global settings
            app = demo

            [Database]
            host = db.local   ; inline comment
            port = 5432
            url = postgres://${database.host}:${Database.port}/${app}
            password = "  spaces; kept "
            """);

        Assert.Equal(["Database"], ini.Sections);
        Assert.Equal("db.local", ini.Get("database", "HOST"));
        Assert.Equal(5432, ini.Get("Database", "port", 0));
        Assert.Equal(30, ini.Get("Database", "timeout", 30));
        Assert.Equal("postgres://db.local:5432/demo", ini.Get("Database", "url"));
        Assert.Equal("  spaces; kept ", ini.Get("Database", "password"));
        Assert.Null(ini.Get("Missing", "x"));
        Assert.Throws<FormatException>(() => IniFile.Parse("[broken"));
    }

    [Fact]
    public void Json_ParsesNestedValues()
    {
        var json = MiniJson.Parse("""
            { "name": "Ada", "age": 36, "langs": ["en", "fr"], "address": { "city": "London" },
              "active": true, "spouse": null, "pi": -3.14e0, "esc": "tab\there é" }
            """);

        Assert.Equal(new JsonValue.String("Ada"), json["name"]);
        Assert.Equal(new JsonValue.Number(36), json["age"]);
        Assert.Equal(new JsonValue.String("fr"), json["langs"][1]);
        Assert.Equal(new JsonValue.String("London"), json["address"]["city"]);
        Assert.Equal(JsonValue.Null.Instance, json["spouse"]);
        Assert.Equal(new JsonValue.Number(-3.14), json["pi"]);
        Assert.Equal(new JsonValue.String("tab\there é"), json["esc"]);
    }

    [Fact]
    public void Json_RoundTrips()
    {
        const string compact = """{"a":[1,2.5,true,null],"b":{"c":"line\nbreak \"q\""}}""";
        Assert.Equal(compact, MiniJson.Serialize(MiniJson.Parse(compact)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("[1,]")]
    [InlineData("{\"a\":1,}")]
    [InlineData("01")]
    [InlineData("1.")]
    [InlineData("tru")]
    [InlineData("\"unterminated")]
    [InlineData("{\"a\":1,\"a\":2}")]
    [InlineData("[1] 2")]
    public void Json_RejectsInvalidInput(string text) => Assert.Throws<FormatException>(() => MiniJson.Parse(text));

    [Fact]
    public void Json_LimitsNesting()
    {
        var deep = new string('[', 1000) + new string(']', 1000);
        var error = Assert.Throws<FormatException>(() => MiniJson.Parse(deep));
        Assert.Contains("Nesting", error.Message);
    }

    [Fact]
    public void SemVer_FollowsThePrecedenceRules()
    {
        string[] ordered = ["1.0.0-alpha", "1.0.0-alpha.1", "1.0.0-alpha.beta", "1.0.0-beta", "1.0.0-beta.2", "1.0.0-beta.11", "1.0.0-rc.1", "1.0.0", "1.0.1", "1.10.0", "2.0.0"];
        var versions = ordered.Select(v => SemanticVersion.Parse(v)).ToArray();

        Assert.Equal(ordered, versions.Order().Select(v => v.ToString()));
        Assert.Equal(0, SemanticVersion.Parse("1.0.0+build.1").CompareTo(SemanticVersion.Parse("1.0.0+build.2")));
        Assert.True(SemanticVersion.Parse("1.0.0-rc.1") < SemanticVersion.Parse("1.0.0"));
    }

    [Theory]
    [InlineData("1.2")]
    [InlineData("01.2.3")]
    [InlineData("1.2.3-01")]
    [InlineData("1.2.3-")]
    [InlineData("1.2.3-a..b")]
    [InlineData("v1.2.3")]
    public void SemVer_RejectsInvalidVersions(string text) => Assert.False(SemanticVersion.TryParse(text, null, out _));

    [Fact]
    public void SemVer_CaretRangesAndBumps()
    {
        var min = SemanticVersion.Parse("1.2.3");
        Assert.True(SemanticVersion.Parse("1.9.0").SatisfiesCaret(min));
        Assert.False(SemanticVersion.Parse("2.0.0").SatisfiesCaret(min));
        Assert.False(SemanticVersion.Parse("1.3.0-beta").SatisfiesCaret(min));
        Assert.False(SemanticVersion.Parse("0.3.0").SatisfiesCaret(SemanticVersion.Parse("0.2.0"))); // 0.x minors are breaking

        Assert.Equal("1.3.0", min.BumpMinor().ToString());
        Assert.Equal("1.2.3", SemanticVersion.Parse("1.2.3-rc.1").BumpPatch().ToString());
    }

    [Fact]
    public void CommandLine_ParsesOptionsFlagsAndPositionals()
    {
        var cli = CommandLine.Parse(
            ["build", "--config", "Release", "--out=bin/x", "-vf", "--verbose", "src", "--tag", "a", "--tag", "b", "--offset", "-5", "--", "--not-an-option"],
            booleanFlags: ["verbose"]);

        Assert.Equal(["build", "src", "--not-an-option"], cli.Positional);
        Assert.Equal("Release", cli.Get("config"));
        Assert.Equal("bin/x", cli.Get("out"));
        Assert.True(cli.Has("v") && cli.Has("f") && cli.Has("verbose"));
        Assert.Equal(["a", "b"], cli.GetAll("tag"));
        Assert.Equal(8080, cli.Get("port", 8080));
        Assert.Equal(-5, cli.Get("offset", 0));
    }

    [Fact]
    public void CommandLine_SplitsWithQuotes()
    {
        Assert.Equal(["copy", "my file.txt", "other dir/x", "it's"], CommandLine.Split("copy 'my file.txt' \"other dir\"/x it\\'s"));
        Assert.Equal(["a", ""], CommandLine.Split("a \"\""));
        Assert.Throws<FormatException>(() => CommandLine.Split("echo 'oops"));
    }

    [Theory]
    [InlineData(0L, true, "0 B")]
    [InlineData(1023L, true, "1023 B")]
    [InlineData(1536L, true, "1.5 KiB")]
    [InlineData(1_000_000_000_000L, true, "931 GiB")]
    [InlineData(1_000_000_000_000L, false, "1 TB")]
    [InlineData(-2048L, true, "-2 KiB")]
    public void Sizes_Format(long bytes, bool binary, string expected) => Assert.Equal(expected, HumanReadable.FormatBytes(bytes, binary));

    [Theory]
    [InlineData("512", 512L)]
    [InlineData("1.5 KiB", 1536L)]
    [InlineData("2MB", 2_000_000L)]
    [InlineData("1 GiB", 1_073_741_824L)]
    public void Sizes_Parse(string text, long expected) => Assert.Equal(expected, HumanReadable.ParseBytes(text));

    [Fact]
    public void Durations_RoundTrip()
    {
        var duration = new TimeSpan(1, 2, 3, 4, 500);

        Assert.Equal("1d2h3m4s500ms", HumanReadable.FormatDuration(duration));
        Assert.Equal(duration, HumanReadable.ParseDuration("1d2h3m4s500ms"));
        Assert.Equal(TimeSpan.FromMinutes(90), HumanReadable.ParseDuration("1.5h"));
        Assert.Equal(TimeSpan.FromSeconds(150), HumanReadable.ParseDuration("2m 30s"));
        Assert.Equal("0s", HumanReadable.FormatDuration(TimeSpan.Zero));
        Assert.Throws<FormatException>(() => HumanReadable.ParseDuration("5 parsecs"));
    }
}
