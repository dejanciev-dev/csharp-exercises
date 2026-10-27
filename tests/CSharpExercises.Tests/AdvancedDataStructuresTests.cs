using CSharpExercises.Collections;

namespace CSharpExercises.Tests;

public class AdvancedDataStructuresTests
{
    private static Trie Words(params (string Word, int Frequency)[] words)
    {
        var trie = new Trie();
        foreach (var (word, frequency) in words) trie.Add(word, frequency);
        return trie;
    }

    [Fact]
    public void Trie_FindsWordsByPrefixInAlphabeticalOrder()
    {
        var trie = Words(("car", 1), ("cart", 1), ("care", 1), ("cat", 1), ("dog", 1));

        Assert.Equal(["car", "care", "cart", "cat"], trie.WithPrefix("ca"));
        Assert.Equal(["car", "care", "cart"], trie.WithPrefix("car"));
        Assert.Empty(trie.WithPrefix("x"));
        Assert.Equal(5, trie.WithPrefix("").Count());
    }

    [Fact]
    public void Trie_ContainsOnlyWholeWords()
    {
        var trie = Words(("cart", 1));

        Assert.True(trie.Contains("cart"));
        Assert.False(trie.Contains("car"));
        Assert.False(trie.Contains("carts"));
    }

    [Fact]
    public void Trie_CountsWordsWithAPrefix()
    {
        var trie = Words(("tea", 1), ("ten", 1), ("to", 1), ("inn", 1));

        Assert.Equal(4, trie.Count);
        Assert.Equal(3, trie.CountWithPrefix("t"));
        Assert.Equal(2, trie.CountWithPrefix("te"));
        Assert.Equal(0, trie.CountWithPrefix("z"));
    }

    [Fact]
    public void Trie_AddingAgainBumpsFrequencyNotCount()
    {
        var trie = new Trie();

        Assert.True(trie.Add("hello"));
        Assert.False(trie.Add("hello"));

        Assert.Equal(1, trie.Count);
    }

    [Fact]
    public void Trie_SuggestsMostFrequentFirst()
    {
        var trie = Words(("console", 50), ("const", 80), ("constant", 20), ("continue", 80), ("class", 99));

        Assert.Equal(["const", "continue", "console"], trie.Suggest("con", 3));
    }

    [Fact]
    public void Trie_RemoveKeepsWordsThatShareThePath()
    {
        var trie = Words(("car", 1), ("cart", 1), ("cat", 1));

        Assert.True(trie.Remove("car"));
        Assert.False(trie.Remove("car"));

        Assert.False(trie.Contains("car"));
        Assert.Equal(["cart", "cat"], trie.WithPrefix("ca"));
        Assert.Equal(2, trie.Count);

        Assert.True(trie.Remove("cart"));
        Assert.Equal(0, trie.CountWithPrefix("car"));
        Assert.Equal(["cat"], trie.WithPrefix("c"));
    }

    [Fact]
    public void Trie_HandlesVeryLongWordsWithoutRecursion()
    {
        var word = new string('a', 50_000);
        var trie = Words((word, 1));

        Assert.Equal(word, Assert.Single(trie.WithPrefix("aaa")));
    }

    [Fact]
    public void UnionFind_TracksGroupsAsTheyMerge()
    {
        var friends = new UnionFind<string>();
        foreach (var person in new[] { "Ana", "Ben", "Cleo", "Dan", "Eli" }) friends.Add(person);

        friends.Union("Ana", "Ben");
        friends.Union("Cleo", "Dan");
        Assert.Equal(3, friends.Groups);
        Assert.False(friends.Connected("Ana", "Dan"));

        friends.Union("Ben", "Dan");
        Assert.True(friends.Connected("Ana", "Cleo"));
        Assert.Equal(2, friends.Groups);
        Assert.Equal(4, friends.SizeOfGroup("Dan"));
        Assert.Equal(1, friends.SizeOfGroup("Eli"));
    }

    [Fact]
    public void UnionFind_UnionOfConnectedItemsReturnsFalse()
    {
        var sets = new UnionFind<int>();

        Assert.True(sets.Union(1, 2));
        Assert.True(sets.Union(2, 3));
        Assert.False(sets.Union(1, 3)); // would close a cycle
    }

    [Fact]
    public void UnionFind_StaysFastOnALongChain()
    {
        var sets = new UnionFind<int>();
        for (var i = 1; i < 200_000; i++) sets.Union(i - 1, i);

        Assert.Equal(1, sets.Groups);
        Assert.True(sets.Connected(0, 199_999));
    }

    [Fact]
    public void UnionFind_UnknownItems()
    {
        var sets = new UnionFind<string>();

        Assert.False(sets.Connected("a", "b"));
        Assert.Throws<KeyNotFoundException>(() => sets.Find("a"));
    }

    [Fact]
    public void RingBuffer_KeepsTheLastNItems()
    {
        var recent = new RingBuffer<int>(3);
        foreach (var i in Enumerable.Range(1, 5)) recent.Add(i);

        Assert.Equal([3, 4, 5], recent);
        Assert.Equal(3, recent[0]);
        Assert.Equal(5, recent[2]);
        Assert.True(recent.IsFull);
    }

    [Fact]
    public void RingBuffer_ReportsWhatWasOverwritten()
    {
        var buffer = new RingBuffer<string>(2);

        Assert.False(buffer.Add("a", out _));
        Assert.False(buffer.Add("b", out _));
        Assert.True(buffer.Add("c", out var dropped));
        Assert.Equal("a", dropped);
    }

    [Fact]
    public void RingBuffer_RemoveOldestWrapsAround()
    {
        var buffer = new RingBuffer<int>(3);
        buffer.Add(1);
        buffer.Add(2);
        buffer.Add(3);
        buffer.Add(4); // overwrites 1

        Assert.Equal(2, buffer.RemoveOldest());
        buffer.Add(5);
        Assert.Equal([3, 4, 5], buffer);

        buffer.RemoveOldest();
        buffer.RemoveOldest();
        buffer.RemoveOldest();
        Assert.Empty(buffer);
        Assert.Throws<InvalidOperationException>(() => buffer.RemoveOldest());
    }

    [Fact]
    public void RingBuffer_MovingAverage()
    {
        var window = new RingBuffer<double>(3);
        List<double> averages = [];

        foreach (var reading in new double[] { 10, 20, 30, 40, 50 })
        {
            window.Add(reading);
            averages.Add(window.Average());
        }

        Assert.Equal([10, 15, 20, 30, 40], averages);
    }

    [Fact]
    public void RingBuffer_IndexOutOfRangeThrows()
    {
        var buffer = new RingBuffer<int>(4);
        buffer.Add(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => buffer[1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer[-1]);
    }

    [Fact]
    public void RingBuffer_ModifyingDuringEnumerationThrows()
    {
        var buffer = new RingBuffer<int>(4);
        buffer.Add(1);
        buffer.Add(2);

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (var item in buffer) buffer.Add(item);
        });
    }
}
