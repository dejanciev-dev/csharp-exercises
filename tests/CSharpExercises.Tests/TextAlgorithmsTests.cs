using CSharpExercises.TextAlgorithms;

namespace CSharpExercises.Tests;

public class TextAlgorithmsTests
{
    [Fact]
    public void RabinKarp_FindsOverlappingMatches()
    {
        Assert.Equal([0, 2, 4], RabinKarp.FindAll("abababa", "aba"));
        Assert.Equal([5], RabinKarp.FindAll("the cat sat", "at s"));
        Assert.Empty(RabinKarp.FindAll("short", "longer pattern"));
    }

    [Theory]
    [InlineData("banana", "ana")]
    [InlineData("abcd", "")]
    [InlineData("aaaa", "aaa")]
    [InlineData("mississippi", "issi")]
    public void RabinKarp_LongestRepeatedSubstring(string text, string expected) =>
        Assert.Equal(expected, RabinKarp.LongestRepeatedSubstring(text));

    [Fact]
    public void ZFunction_ComputesPrefixMatches()
    {
        Assert.Equal([7, 0, 1, 0, 3, 0, 1], ZFunction.Compute("abacaba"));
        Assert.Equal([0, 2, 4], ZFunction.FindAll("abababa", "aba"));
    }

    [Theory]
    [InlineData("abab", "ab")]
    [InlineData("aaaa", "a")]
    [InlineData("abcabcabc", "abc")]
    [InlineData("abcab", "abcab")]
    public void ZFunction_SmallestPeriod(string s, string expected) => Assert.Equal(expected, ZFunction.SmallestPeriod(s));

    [Theory]
    [InlineData("babad", "bab")]
    [InlineData("cbbd", "bb")]
    [InlineData("forgeeksskeegfor", "geeksskeeg")]
    [InlineData("a", "a")]
    [InlineData("", "")]
    public void Manacher_LongestPalindrome(string s, string expected) => Assert.Equal(expected, Manacher.LongestPalindrome(s));

    [Fact]
    public void Manacher_AgreesWithTheSimpleVersion()
    {
        var random = new Random(3);
        for (var i = 0; i < 200; i++)
        {
            var s = new string(Enumerable.Range(0, random.Next(1, 30)).Select(_ => (char)('a' + random.Next(3))).ToArray());
            Assert.Equal(Manacher.LongestPalindromeSimple(s).Length, Manacher.LongestPalindrome(s).Length);
        }
    }

    [Fact]
    public void SuffixArray_SortsSuffixes()
    {
        var sa = new SuffixArray("banana");

        // a, ana, anana, banana, na, nana
        Assert.Equal([5, 3, 1, 0, 4, 2], sa.Suffixes);
        Assert.Equal([0, 1, 3, 0, 0, 2], sa.Lcp);
        Assert.Equal("ana", sa.LongestRepeatedSubstring());
        Assert.Equal(15, sa.CountDistinctSubstrings());
    }

    [Fact]
    public void SuffixArray_FindsAllOccurrences()
    {
        var sa = new SuffixArray("mississippi");

        Assert.Equal([1, 4], sa.FindAll("issi"));
        Assert.Equal([2, 3, 5, 6], sa.FindAll("s"));
        Assert.Equal([6], sa.FindAll("sip"));
        Assert.Empty(sa.FindAll("spa"));
        Assert.Equal([9], sa.FindAll("pi"));
    }

    [Fact]
    public void AhoCorasick_FindsEveryPatternInOnePass()
    {
        var matcher = new AhoCorasick(["he", "she", "his", "hers"]);

        var matches = matcher.FindAll("ushers").ToArray();

        Assert.Equal([(1, "she"), (2, "he"), (2, "hers")], matches);
    }

    [Fact]
    public void AhoCorasick_Censors()
    {
        var filter = new AhoCorasick(["darn", "heck"]);
        Assert.Equal("oh ****, what the ****!", filter.Censor("oh darn, what the heck!"));
    }

    [Theory]
    [InlineData("Robert", "R163")]
    [InlineData("Rupert", "R163")]
    [InlineData("Rubin", "R150")]
    [InlineData("Ashcraft", "A261")]
    [InlineData("Tymczak", "T522")]
    [InlineData("Pfister", "P236")]
    [InlineData("Lee", "L000")]
    public void Soundex_EncodesBySound(string name, string expected) => Assert.Equal(expected, Soundex.Encode(name));

    [Fact]
    public void WordWrap_BalancedIsLessRagged()
    {
        const string text = "aaa bb cc ddddd";

        var greedy = WordWrap.Greedy(text, 6);
        var balanced = WordWrap.Balanced(text, 6);

        Assert.Equal(["aaa bb", "cc", "ddddd"], greedy);
        Assert.Equal(["aaa", "bb cc", "ddddd"], balanced);
        Assert.True(WordWrap.Raggedness(balanced, 6) < WordWrap.Raggedness(greedy, 6));
        Assert.All(balanced, line => Assert.True(line.Length <= 6));
    }

    [Fact]
    public void WordWrap_RejectsWordsLongerThanTheLine()
    {
        Assert.Throws<ArgumentException>(() => WordWrap.Greedy("supercalifragilistic", 10));
    }
}
