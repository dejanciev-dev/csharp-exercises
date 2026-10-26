using CSharpExercises.Strings;

namespace CSharpExercises.Tests;

public class StringExercisesTests
{
    [Fact]
    public void Reverse() => Assert.Equal("olleh", StringExercises.Reverse("hello"));

    [Theory]
    [InlineData("A man, a plan, a canal: Panama", true)]
    [InlineData("racecar", true)]
    [InlineData("hello", false)]
    [InlineData("", true)]
    public void IsPalindrome(string input, bool expected) =>
        Assert.Equal(expected, StringExercises.IsPalindrome(input));

    [Theory]
    [InlineData("listen", "silent", true)]
    [InlineData("Dormitory", "Dirty room", true)]
    [InlineData("hello", "world", false)]
    public void AreAnagrams(string a, string b, bool expected) =>
        Assert.Equal(expected, StringExercises.AreAnagrams(a, b));

    [Fact]
    public void CountVowels() => Assert.Equal(5, StringExercises.CountVowels("Education"));

    [Theory]
    [InlineData("aaabcc", "a3b1c2")]
    [InlineData("a", "a1")]
    [InlineData("", "")]
    public void Compress(string input, string expected) =>
        Assert.Equal(expected, StringExercises.Compress(input));

    [Fact]
    public void WordFrequency_IsCaseInsensitive()
    {
        var result = StringExercises.WordFrequency("The cat and the hat. THE end!");

        Assert.Equal(3, result["the"]);
        Assert.Equal(1, result["cat"]);
        Assert.Equal(5, result.Count);
    }
}
