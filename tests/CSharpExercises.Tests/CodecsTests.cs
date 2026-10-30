using System.Text;
using CSharpExercises.Codecs;

namespace CSharpExercises.Tests;

public class CodecsTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("f", "Zg==")]
    [InlineData("fo", "Zm8=")]
    [InlineData("foo", "Zm9v")]
    [InlineData("foobar", "Zm9vYmFy")]
    public void Base64_MatchesRfc4648Vectors(string input, string expected)
    {
        Assert.Equal(expected, Base64Codec.Encode(Encoding.ASCII.GetBytes(input)));
        Assert.Equal(input, Encoding.ASCII.GetString(Base64Codec.Decode(expected)));
    }

    [Fact]
    public void Base64_AgreesWithTheFrameworkAndSupportsUrlSafe()
    {
        var random = new Random(1);
        for (var length = 0; length < 50; length++)
        {
            var bytes = new byte[length];
            random.NextBytes(bytes);
            Assert.Equal(Convert.ToBase64String(bytes), Base64Codec.Encode(bytes));
            Assert.Equal(bytes, Base64Codec.Decode(Base64Codec.Encode(bytes, urlSafe: true)));
        }

        Assert.Equal("-_8", Base64Codec.Encode([0xFB, 0xFF], urlSafe: true));
        Assert.Throws<FormatException>(() => Base64Codec.Decode("Zm9v!"));
        Assert.Throws<FormatException>(() => Base64Codec.Decode("Zh==")); // stray bits in the padding
    }

    [Fact]
    public void Hex_RoundTripsAndDumps()
    {
        Assert.Equal("00ff10ab", HexDump.ToHex([0x00, 0xFF, 0x10, 0xAB]));
        Assert.Equal("00FF10AB", HexDump.ToHex([0x00, 0xFF, 0x10, 0xAB], upperCase: true));
        Assert.Equal([0xDE, 0xAD, 0xBE, 0xEF], HexDump.FromHex("DeadBeef"));
        Assert.Throws<FormatException>(() => HexDump.FromHex("abc"));

        var dump = HexDump.Dump(Encoding.ASCII.GetBytes("Hello, hex dump!\n\0"));
        Assert.Equal(
            "00000000  48 65 6c 6c 6f 2c 20 68  65 78 20 64 75 6d 70 21  |Hello, hex dump!|\n" +
            "00000010  0a 00                                             |..|\n",
            dump);
    }

    [Theory]
    [InlineData("hello world", false, "hello%20world")]
    [InlineData("hello world", true, "hello+world")]
    [InlineData("a&b=c/d", false, "a%26b%3Dc%2Fd")]
    [InlineData("café", false, "caf%C3%A9")]
    [InlineData("safe-._~", false, "safe-._~")]
    public void Percent_Encodes(string input, bool plus, string expected)
    {
        Assert.Equal(expected, PercentEncoding.Encode(input, plus));
        Assert.Equal(input, PercentEncoding.Decode(expected, plus));
    }

    [Fact]
    public void Percent_ParsesAndBuildsQueries()
    {
        var query = PercentEncoding.ParseQuery("?tag=c%23&tag=.net&q=two+words&empty");

        Assert.Equal(["c#", ".net"], query["tag"]);
        Assert.Equal("two words", query["q"].Single());
        Assert.Equal("", query["empty"].Single());
        Assert.Equal("q=a%26b&lang=en+GB", PercentEncoding.BuildQuery([new("q", "a&b"), new("lang", "en GB")]));
        Assert.Throws<FormatException>(() => PercentEncoding.Decode("50%"));
    }

    [Theory]
    [InlineData("A")]
    [InlineData("é")]
    [InlineData("€")]
    [InlineData("😀")]
    [InlineData("mixed: a é € 😀")]
    public void Utf8_MatchesTheFramework(string text)
    {
        var bytes = Utf8Codec.Encode(text);
        Assert.Equal(Encoding.UTF8.GetBytes(text), bytes);
        Assert.Equal(text, Utf8Codec.Decode(bytes));
    }

    [Theory]
    [InlineData(new byte[] { 0xC0, 0xAF })]             // overlong '/'
    [InlineData(new byte[] { 0xE0, 0x80, 0xAF })]       // overlong, three bytes
    [InlineData(new byte[] { 0xED, 0xA0, 0x80 })]       // a UTF-16 surrogate
    [InlineData(new byte[] { 0xF4, 0x90, 0x80, 0x80 })] // above U+10FFFF
    [InlineData(new byte[] { 0xE2, 0x82 })]             // truncated
    [InlineData(new byte[] { 0x80 })]                   // stray continuation byte
    public void Utf8_RejectsMalformedInput(byte[] bytes)
    {
        Assert.Throws<FormatException>(() => Utf8Codec.Decode(bytes));
    }

    [Fact]
    public void Utf8_TruncatesOnCharacterBoundaries()
    {
        var bytes = Utf8Codec.Encode("a€b"); // 1 + 3 + 1 bytes

        Assert.Equal("a", Utf8Codec.Decode(Utf8Codec.TruncateToBytes(bytes, 3)));
        Assert.Equal("a€", Utf8Codec.Decode(Utf8Codec.TruncateToBytes(bytes, 4)));
    }

    [Theory]
    [InlineData(1, "I")]
    [InlineData(4, "IV")]
    [InlineData(9, "IX")]
    [InlineData(14, "XIV")]
    [InlineData(1994, "MCMXCIV")]
    [InlineData(2026, "MMXXVI")]
    [InlineData(3999, "MMMCMXCIX")]
    public void Roman_RoundTrips(int number, string roman)
    {
        Assert.Equal(roman, RomanNumerals.ToRoman(number));
        Assert.True(RomanNumerals.TryParse(roman, out var parsed));
        Assert.Equal(number, parsed);
    }

    [Theory]
    [InlineData("IIII")]
    [InlineData("VX")]
    [InlineData("IC")]
    [InlineData("MMMM")]
    [InlineData("abc")]
    [InlineData("")]
    public void Roman_RejectsNonCanonicalForms(string roman) => Assert.False(RomanNumerals.TryParse(roman, out _));

    [Theory]
    [InlineData(0, "zero")]
    [InlineData(13, "thirteen")]
    [InlineData(40, "forty")]
    [InlineData(99, "ninety-nine")]
    [InlineData(101, "one hundred one")]
    [InlineData(1_000_001, "one million one")]
    [InlineData(-2_500, "minus two thousand five hundred")]
    [InlineData(123_456_789, "one hundred twenty-three million four hundred fifty-six thousand seven hundred eighty-nine")]
    public void Words_SpellsNumbers(long number, string expected) => Assert.Equal(expected, NumberToWords.Convert(number));

    [Fact]
    public void Words_ExtremesAmountsAndOrdinals()
    {
        Assert.StartsWith("minus nine quintillion", NumberToWords.Convert(long.MinValue));
        Assert.Equal("one thousand two hundred thirty-four dollars and 56/100", NumberToWords.Amount(1234.56m));
        Assert.Equal("two dollars and 00/100", NumberToWords.Amount(1.999m)); // rounding carries into the dollars

        Assert.Equal("first", NumberToWords.Ordinal(1));
        Assert.Equal("twelfth", NumberToWords.Ordinal(12));
        Assert.Equal("twentieth", NumberToWords.Ordinal(20));
        Assert.Equal("twenty-second", NumberToWords.Ordinal(22));
        Assert.Equal("one hundred third", NumberToWords.Ordinal(103));
    }
}
