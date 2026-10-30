using CSharpExercises.TextFormatting;

namespace CSharpExercises.Tests;

public class TextFormattingTests
{
    [Theory]
    [InlineData("parseHTTPResponse", "parse_http_response", "parseHttpResponse", "ParseHttpResponse")]
    [InlineData("user_id", "user_id", "userId", "UserId")]
    [InlineData("XMLHttpRequest", "xml_http_request", "xmlHttpRequest", "XmlHttpRequest")]
    [InlineData("first-name", "first_name", "firstName", "FirstName")]
    [InlineData("utf8Encoder", "utf8_encoder", "utf8Encoder", "Utf8Encoder")]
    public void CaseConverter_ConvertsBetweenConventions(string input, string snake, string camel, string pascal)
    {
        Assert.Equal(snake, CaseConverter.ToSnake(input));
        Assert.Equal(camel, CaseConverter.ToCamel(input));
        Assert.Equal(pascal, CaseConverter.ToPascal(input));
        Assert.Equal(snake.Replace('_', '-'), CaseConverter.ToKebab(input));
        Assert.Equal(snake.ToUpperInvariant(), CaseConverter.ToConstant(input));
    }

    [Fact]
    public void CaseConverter_TitleCase() =>
        Assert.Equal("The Lord of the Rings", CaseConverter.ToTitle("the lord of the rings"));

    [Fact]
    public void NaturalSort_OrdersNumbersByValue()
    {
        string[] files = ["file10.txt", "file2.txt", "File1.txt", "file1.txt", "file02.txt", "file100.txt", "a", "file99999999999999999999.txt"];

        var sorted = files.Order(NaturalComparer.Instance).ToArray();

        Assert.Equal(["a", "File1.txt", "file1.txt", "file2.txt", "file02.txt", "file10.txt", "file100.txt", "file99999999999999999999.txt"], sorted);
        Assert.Equal(["v1.2.9", "v1.2.10", "v1.10.0"], new[] { "v1.10.0", "v1.2.10", "v1.2.9" }.Order(NaturalComparer.Instance));
    }

    [Theory]
    [InlineData("*.cs", "Program.cs", true)]
    [InlineData("*.cs", "src/Program.cs", false)]          // * stays within one folder
    [InlineData("**/*.cs", "src/a/b/Program.cs", true)]
    [InlineData("**/*.cs", "Program.cs", true)]            // ** can be zero folders
    [InlineData("src/**/test?.txt", "src/x/test1.txt", true)]
    [InlineData("file[0-9].log", "file7.log", true)]
    [InlineData("file[!0-9].log", "file7.log", false)]
    [InlineData("a*b*c", "aXXbYYc", true)]
    [InlineData("a*b*c", "aXXbYY", false)]
    [InlineData("\\*.txt", "*.txt", true)]
    [InlineData("**/b.cs", "ab.cs", false)]                // ** swallows whole folders, not part of a name
    [InlineData("**/b.cs", "x/y/b.cs", true)]
    [InlineData("src/**", "src/a/b.txt", true)]
    public void Glob_Matches(string pattern, string path, bool expected) => Assert.Equal(expected, Glob.IsMatch(pattern, path));

    [Fact]
    public void Glob_IsFastOnPathologicalPatterns()
    {
        var text = new string('a', 5000);
        var pattern = string.Concat(Enumerable.Repeat("a*", 50)) + "b";
        var watch = System.Diagnostics.Stopwatch.StartNew();

        Assert.False(Glob.IsMatch(pattern, text));
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void TextTable_AlignsColumns()
    {
        var table = new TextTable("Name", "Qty", "Price")
            .AddRow("Apple", 3, 1.5)
            .AddRow("Watermelon", 12, 4.25);

        Assert.Equal(
            "Name       | Qty | Price\n" +
            "-----------+-----+------\n" +
            "Apple      |   3 |   1.5\n" +
            "Watermelon |  12 |  4.25\n",
            table.Render());
        Assert.Throws<ArgumentException>(() => table.AddRow("too few"));
    }

    [Fact]
    public void TextTable_TruncatesAndCenters()
    {
        var table = new TextTable("Id", "Description") { MaxColumnWidth = 8 }.AddRow("x", "a very long description").AlignColumn(0, Align.Center);

        Assert.Contains("x  | a very …", table.Render()); // centred "x", description cut to 8 characters
    }

    [Theory]
    [InlineData("file", "files")]
    [InlineData("box", "boxes")]
    [InlineData("church", "churches")]
    [InlineData("city", "cities")]
    [InlineData("day", "days")]
    [InlineData("knife", "knives")]
    [InlineData("child", "children")]
    [InlineData("Person", "People")]
    [InlineData("sheep", "sheep")]
    [InlineData("hero", "heroes")]
    [InlineData("photo", "photos")]
    [InlineData("URL", "URLs")]
    [InlineData("CHILD", "CHILDREN")]
    public void Pluralizer_HandlesRulesAndExceptions(string word, string plural) => Assert.Equal(plural, Pluralizer.Plural(word));

    [Fact]
    public void Pluralizer_Quantities()
    {
        Assert.Equal(["0 files", "1 file", "2 files"], new[] { 0, 1, 2 }.Select(n => Pluralizer.Quantity(n, "file")));
    }

    [Fact]
    public void Markdown_ConvertsBlocksAndInlines()
    {
        const string md = """
            # Title

            Some **bold** and *italic* text with `a <b> tag` and [a link](https://example.com).
            Continued line.

            - one
            - two

            1. first
            2. second

            ```
            if (a < b) { }
            ```
            """;

        Assert.Equal(
            "<h1>Title</h1>\n" +
            "<p>Some <strong>bold</strong> and <em>italic</em> text with <code>a &lt;b&gt; tag</code> and <a href=\"https://example.com\">a link</a>. Continued line.</p>\n" +
            "<ul>\n<li>one</li>\n<li>two</li>\n</ul>\n" +
            "<ol>\n<li>first</li>\n<li>second</li>\n</ol>\n" +
            "<pre><code>if (a &lt; b) { }\n</code></pre>\n",
            MiniMarkdown.ToHtml(md));
    }

    [Fact]
    public void Markdown_EscapesHtmlAndUnsafeLinks()
    {
        Assert.Equal("<p>&lt;script&gt;alert(1)&lt;/script&gt;</p>\n", MiniMarkdown.ToHtml("<script>alert(1)</script>"));
        Assert.Equal("<p>click</p>\n", MiniMarkdown.ToHtml("[click](javascript:alert(1))"));
        Assert.Equal("<p><code>**not bold**</code></p>\n", MiniMarkdown.ToHtml("`**not bold**`"));
    }
}
