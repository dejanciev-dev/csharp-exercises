using CSharpExercises.Collections;

namespace CSharpExercises.Tests;

public class CollectionsTests
{
    [Fact]
    public void Stack_IsLastInFirstOut_AndGrows()
    {
        var stack = new Collections.Stack<int>();
        for (var i = 1; i <= 10; i++) stack.Push(i);

        Assert.Equal(10, stack.Count);
        Assert.Equal(10, stack.Pop());
        Assert.Equal(9, stack.Peek());
        Assert.Equal(9, stack.Count);
    }

    [Fact]
    public void Stack_PopOnEmpty_Throws() =>
        Assert.Throws<InvalidOperationException>(() => new Collections.Stack<string>().Pop());

    [Theory]
    [InlineData("()[]{}", true)]
    [InlineData("{[()()]}", true)]
    [InlineData("var x = list[0] + (a * b);", true)]
    [InlineData("(]", false)]
    [InlineData("([)]", false)]
    [InlineData("((", false)]
    [InlineData(")", false)]
    public void BracketValidator_IsBalanced(string input, bool expected) =>
        Assert.Equal(expected, BracketValidator.IsBalanced(input));

    [Fact]
    public void LruCache_EvictsLeastRecentlyUsed()
    {
        var cache = new LruCache<string, int>(2);
        cache.Put("a", 1);
        cache.Put("b", 2);
        cache.TryGet("a", out _); // "b" is now least recently used
        cache.Put("c", 3);

        Assert.False(cache.TryGet("b", out _));
        Assert.True(cache.TryGet("a", out var a));
        Assert.Equal(1, a);
        Assert.Equal(2, cache.Count);
    }

    [Fact]
    public void LruCache_PutExistingKey_UpdatesValue()
    {
        var cache = new LruCache<int, string>(2);
        cache.Put(1, "one");
        cache.Put(1, "uno");

        Assert.True(cache.TryGet(1, out var value));
        Assert.Equal("uno", value);
        Assert.Equal(1, cache.Count);
    }
}
