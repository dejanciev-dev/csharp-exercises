using System.Text;
using CSharpExercises.Algorithms;
using CSharpExercises.Strings;

namespace CSharpExercises.Tests;

public class TextProcessingTests
{
    private const string ErrorLine = """2026-10-27 14:03:12.123 [ERROR] Payments: Charge failed (orderId=42, user="Ana Markovska", amount=19.99)""";

    [Fact]
    public void LogParser_ParsesAllParts()
    {
        var entry = LogParser.TryParse(ErrorLine);

        Assert.NotNull(entry);
        Assert.Equal(new DateTime(2026, 10, 27, 14, 3, 12, 123), entry.Timestamp);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal("Payments", entry.Source);
        Assert.StartsWith("Charge failed", entry.Message);
        Assert.Equal("42", entry.Properties["orderId"]);
        Assert.Equal("Ana Markovska", entry.Properties["user"]);
        Assert.Equal("19.99", entry.Properties["amount"]);
    }

    [Theory]
    [InlineData("2026-10-27 14:03:12 [info] Api.Orders: started", LogLevel.Info)]
    [InlineData("2026-10-27 14:03:12.5 [Warn] Api: slow", LogLevel.Warn)]
    public void LogParser_AcceptsOptionalMillisecondsAndAnyCase(string line, LogLevel level)
    {
        Assert.Equal(level, LogParser.TryParse(line)?.Level);
    }

    [Theory]
    [InlineData("   at Payments.Charge() in Payments.cs:line 12")]
    [InlineData("2026-10-27 14:03:12 [LOUD] Api: unknown level")]
    [InlineData("2026-13-45 14:03:12 [INFO] Api: impossible date")]
    public void LogParser_RejectsLinesThatDoNotFit(string line)
    {
        Assert.Null(LogParser.TryParse(line));
    }

    [Fact]
    public void LogParser_ReportsUnparsedLinesAndHotspots()
    {
        string[] lines =
        [
            ErrorLine,
            "   at Payments.Charge()",
            "",
            "2026-10-27 14:04:00 [FATAL] Payments: Gateway down",
            "2026-10-27 14:05:00 [ERROR] Search: Index missing",
            "2026-10-27 14:06:00 [WARN] Search: Slow query",
        ];

        var (entries, unparsed) = LogParser.ParseAll(lines);

        Assert.Equal(4, entries.Count);
        Assert.Equal([2], unparsed); // the blank line is not an error
        Assert.Equal([("Payments", 2), ("Search", 1)], LogParser.ErrorHotspots(entries));
    }

    [Fact]
    public void LogParser_RedactsEmailsAndCardNumbers()
    {
        var line = "Refund for ana.markovska@example.mk using card 4111 1111 1111 1234 and 5500-0000-0000-0004";

        Assert.Equal(
            "Refund for a***@example.mk using card **** **** **** 1234 and **** **** **** 0004",
            LogParser.Redact(line));
    }

    [Fact]
    public void Template_FillsPlaceholdersWithFilters()
    {
        var result = TemplateEngine.Render(
            "Hello {{ name | trim | upper }}, you have {{count}} new {{ count | plural:message,messages }}.",
            new Dictionary<string, object?> { ["name"] = "  ana ", ["count"] = 3 });

        Assert.Equal("Hello ANA, you have 3 new messages.", result);
    }

    [Fact]
    public void Template_PluralForOne()
    {
        Assert.Equal("1 message", TemplateEngine.Render("{{n}} {{n|plural:message,messages}}", new Dictionary<string, object?> { ["n"] = 1 }));
    }

    [Fact]
    public void Template_FormatsNumbersTheSameEverywhere()
    {
        Assert.Equal("Total: 19.5", TemplateEngine.Render("Total: {{ total }}", new Dictionary<string, object?> { ["total"] = 19.5m }));
    }

    [Fact]
    public void Template_DefaultCoversMissingValues()
    {
        Assert.Equal("Untitled", TemplateEngine.Render("{{ title | default:Untitled }}", new Dictionary<string, object?>()));
    }

    [Fact]
    public void Template_MissingValueThrows()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() => TemplateEngine.Render("Hi {{ nmae }}", new Dictionary<string, object?> { ["name"] = "Ana" }));
        Assert.Contains("nmae", ex.Message);
    }

    [Fact]
    public void Template_EscapesHtmlAndTruncates()
    {
        var values = new Dictionary<string, object?> { ["comment"] = "<script>alert('hi')</script> and more text" };

        Assert.Equal("&lt;script&gt;alert(&#39;hi&#39;)&lt;/script&gt; and more text", TemplateEngine.Render("{{ comment | html }}", values));
        Assert.Equal("&lt;script&gt;…", TemplateEngine.Render("{{ comment | truncate:9 | html }}", values));
    }

    [Fact]
    public void Template_UnknownFilterThrows()
    {
        Assert.Throws<FormatException>(() => TemplateEngine.Render("{{ x | shout }}", new Dictionary<string, object?> { ["x"] = "a" }));
    }

    [Fact]
    public void Template_ListsPlaceholderNames()
    {
        var names = TemplateEngine.PlaceholderNames("{{name}} {{ count }} {{count|plural:a,b}}");

        Assert.True(names.SetEquals(["name", "count"]));
    }

    [Fact]
    public void Unicode_CountsWhatUsersSee()
    {
        Assert.Equal(4, "👍🏽".Length);
        Assert.Equal(1, UnicodeText.CountCharacters("👍🏽"));
        Assert.Equal(1, UnicodeText.CountCharacters("👨‍👩‍👧"));        // family: 3 people joined into one emoji
        Assert.Equal(4, UnicodeText.CountCharacters("noël")); // "noël" with a combining diaeresis
    }

    [Fact]
    public void Unicode_ReversingCharsBreaksText()
    {
        var reversedChars = StringExercises.Reverse("a👍b");

        // The surrogate pair is now in the wrong order: not valid UTF-16, so it cannot even be encoded.
        Assert.Throws<EncoderFallbackException>(() => new UTF8Encoding(false, throwOnInvalidBytes: true).GetBytes(reversedChars));
        Assert.Equal("b👍a", UnicodeText.Reverse("a👍b"));
    }

    [Fact]
    public void Unicode_ReverseKeepsAccentsOnTheirLetters()
    {
        Assert.Equal("lëon", UnicodeText.Reverse("noël"));
        Assert.Equal("l̈eon", StringExercises.Reverse("noël")); // the accent jumped onto the l
    }

    [Theory]
    [InlineData("Hello world", 8, "Hello w…")]
    [InlineData("Short", 10, "Short")]
    [InlineData("👍🏽👍🏽👍🏽👍🏽", 3, "👍🏽👍🏽…")]
    [InlineData("abc", 0, "")]
    public void Unicode_TruncateNeverSplitsACharacter(string text, int max, string expected)
    {
        Assert.Equal(expected, UnicodeText.Truncate(text, max));
    }

    [Theory]
    [InlineData("Crème Brûlée à la carte!", "creme-brulee-a-la-carte")]
    [InlineData("  Straße & Smørrebrød  ", "strasse-smorrebrod")]
    [InlineData("C# 14 — What's new?", "c-14-what-s-new")]
    [InlineData("Đoković wins", "dokovic-wins")]
    [InlineData("---", "")]
    public void Unicode_Slugify(string title, string slug)
    {
        Assert.Equal(slug, UnicodeText.Slugify(title));
    }

    [Theory]
    [InlineData("Résumé", "resume", true)]
    [InlineData("ÅNGSTRÖM", "angstrom", true)]
    [InlineData("resume", "resumes", false)]
    public void Unicode_EqualsIgnoringAccents(string a, string b, bool expected)
    {
        Assert.Equal(expected, UnicodeText.EqualsIgnoringAccents(a, b));
    }

    [Fact]
    public void Kmp_FindsOverlappingMatches()
    {
        Assert.Equal([0, 2, 4], StringSearch.IndexesOf("abababa", "aba"));
        Assert.Equal([0, 1], StringSearch.IndexesOf("aaa", "aa"));
        Assert.Empty(StringSearch.IndexesOf("abc", "abcd"));
    }

    [Theory]
    [InlineData("abab", new[] { 0, 0, 1, 2 })]
    [InlineData("aabaaab", new[] { 0, 1, 0, 1, 2, 2, 3 })]
    [InlineData("abcd", new[] { 0, 0, 0, 0 })]
    public void Kmp_FallbackTable(string pattern, int[] expected)
    {
        Assert.Equal(expected, StringSearch.Fallback(pattern));
    }

    [Fact]
    public void Kmp_AgreesWithANaiveSearch()
    {
        var random = new Random(7);
        for (var round = 0; round < 200; round++)
        {
            var text = new string([.. Enumerable.Range(0, random.Next(0, 60)).Select(_ => "ab"[random.Next(2)])]);
            var pattern = new string([.. Enumerable.Range(0, random.Next(1, 5)).Select(_ => "ab"[random.Next(2)])]);

            var naive = Enumerable.Range(0, Math.Max(0, text.Length - pattern.Length + 1))
                .Where(i => string.CompareOrdinal(text, i, pattern, 0, pattern.Length) == 0);

            Assert.Equal(naive, StringSearch.IndexesOf(text, pattern));
        }
    }

    [Fact]
    public void Kmp_IsLinearOnTheNaiveWorstCase()
    {
        // A naive search would compare about a billion characters here.
        var text = new string('a', 1_000_000) + "b";
        var pattern = new string('a', 1_000) + "b";

        Assert.Equal([999_000], StringSearch.IndexesOf(text, pattern));
    }
}
