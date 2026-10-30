using CSharpExercises.Collections;

namespace CSharpExercises.Tests;

public class MoreCollectionsTests
{
    [Fact]
    public void Deque_PushesAndPopsAtBothEnds()
    {
        var deque = new Deque<int>(capacity: 2);
        deque.PushBack(2);
        deque.PushBack(3);
        deque.PushFront(1); // wraps, then grows
        deque.PushFront(0);
        deque.PushBack(4);

        Assert.Equal([0, 1, 2, 3, 4], deque);
        Assert.Equal(2, deque[2]);
        Assert.Equal(0, deque.PopFront());
        Assert.Equal(4, deque.PopBack());
        Assert.Equal((1, 3), (deque.PeekFront(), deque.PeekBack()));
        Assert.Throws<InvalidOperationException>(() => new Deque<int>().PopBack());
    }

    [Fact]
    public void Deque_MatchesLinkedListUnderRandomOperations()
    {
        var random = new Random(1);
        var deque = new Deque<int>(1);
        var reference = new LinkedList<int>();

        for (var i = 0; i < 5_000; i++)
        {
            switch (reference.Count == 0 ? random.Next(2) : random.Next(4))
            {
                case 0: deque.PushFront(i); reference.AddFirst(i); break;
                case 1: deque.PushBack(i); reference.AddLast(i); break;
                case 2: Assert.Equal(reference.First!.Value, deque.PopFront()); reference.RemoveFirst(); break;
                case 3: Assert.Equal(reference.Last!.Value, deque.PopBack()); reference.RemoveLast(); break;
            }
        }
        Assert.Equal(reference, deque);
    }

    [Theory]
    [InlineData("A man, a plan, a canal: Panama", true)]
    [InlineData("race a car", false)]
    public void Deque_ChecksPalindromes(string text, bool expected) => Assert.Equal(expected, Deque<char>.IsPalindrome(text));

    [Fact]
    public void SkipList_StaysSortedAndLogarithmic()
    {
        var list = new SkipList<int>(random: new Random(2));
        var values = Enumerable.Range(0, 10_000).Select(i => i * 7 % 10_007).ToArray();
        foreach (var v in values) Assert.True(list.Add(v));

        Assert.False(list.Add(values[0]));
        Assert.Equal(values.Order(), list);
        Assert.InRange(list.Levels, 10, 25); // about log2(10,000) ≈ 13
        Assert.True(list.Contains(700));
        Assert.Equal([14, 21, 28], list.Range(10, 30).Where(v => v % 7 == 0));

        Assert.True(list.Remove(700));
        Assert.False(list.Contains(700));
        Assert.False(list.Remove(700));
        Assert.Equal(9_999, list.Count);
    }

    [Fact]
    public void Lfu_EvictsTheLeastFrequentlyUsed()
    {
        var cache = new LfuCache<string, int>(2);
        cache.Put("a", 1);
        cache.Put("b", 2);
        cache.TryGet("a", out _);
        cache.TryGet("a", out _);

        cache.Put("c", 3); // b was used least

        Assert.False(cache.TryGet("b", out _));
        Assert.True(cache.TryGet("a", out var a) && a == 1);
        Assert.Equal(4, cache.FrequencyOf("a"));
    }

    [Fact]
    public void Lfu_BreaksTiesByRecency()
    {
        var cache = new LfuCache<int, string>(3);
        cache.Put(1, "one");
        cache.Put(2, "two");
        cache.Put(3, "three");
        cache.Put(1, "uno"); // update counts as a use
        cache.TryGet(2, out _);

        cache.Put(4, "four"); // 3 is the only key used once

        Assert.False(cache.TryGet(3, out _));
        Assert.True(cache.TryGet(1, out var one));
        Assert.Equal("uno", one);
        Assert.Equal(3, cache.Count);
    }

    [Fact]
    public void BiMap_LooksUpBothWays()
    {
        var codes = new BiMap<string, int>(StringComparer.OrdinalIgnoreCase);
        codes.Add("DE", 49);
        codes.Add("FR", 33);

        Assert.Equal(49, codes["de"]);
        Assert.Equal("FR", codes.KeyOf(33));
        Assert.Throws<ArgumentException>(() => codes.Add("XX", 49));

        codes.Set("UK", 33); // takes 33 away from FR
        Assert.False(codes.TryGetValue("FR", out _));
        Assert.Equal("UK", codes.KeyOf(33));
        Assert.Equal("DE", codes.Inverse()[49]);
        Assert.Equal(2, codes.Count);
    }

    [Fact]
    public void IntervalSet_MergesAndSplits()
    {
        var set = new IntervalSet();
        set.Add(10, 20);
        set.Add(30, 40);
        set.Add(18, 32);   // bridges both
        set.Add(50, 60);
        set.Add(60, 70);   // touching ranges merge

        Assert.Equal([(10L, 40L), (50L, 70L)], set.Ranges);

        set.Remove(15, 25);
        Assert.Equal([(10L, 15L), (25L, 40L), (50L, 70L)], set.Ranges);
        Assert.Equal(40, set.TotalLength);

        Assert.True(set.Contains(10));
        Assert.False(set.Contains(15));
        Assert.True(set.Contains(39));
        Assert.False(set.Contains(40));
    }

    [Fact]
    public void IntervalSet_FindsFreeSlots()
    {
        var busy = new IntervalSet();
        busy.Add(9 * 60, 10 * 60);
        busy.Add(12 * 60, 13 * 60);

        var free = busy.Gaps(8 * 60, 17 * 60).Select(g => $"{g.Start / 60}-{g.End / 60}");

        Assert.Equal(["8-9", "10-12", "13-17"], free);
    }

    [Fact]
    public void Avl_StaysBalancedOnSortedInput()
    {
        var tree = new AvlTree<int>();
        for (var i = 0; i < 1_000; i++) tree.Add(i);

        Assert.Equal(1_000, tree.Count);
        Assert.True(tree.Height <= 11); // 1.44 · log2(1000) ≈ 14 worst case; a sorted insert gives a perfect tree
        Assert.True(tree.IsBalanced());
        Assert.Equal(Enumerable.Range(0, 1_000), tree.InOrder());
    }

    [Fact]
    public void Avl_RemovesAndAnswersOrderQueries()
    {
        var random = new Random(3);
        var tree = new AvlTree<int>();
        var reference = new SortedSet<int>();
        for (var i = 0; i < 3_000; i++)
        {
            var v = random.Next(1_000);
            if (random.Next(3) == 0) Assert.Equal(reference.Remove(v), tree.Remove(v));
            else Assert.Equal(reference.Add(v), tree.Add(v));
        }

        Assert.True(tree.IsBalanced());
        Assert.Equal(reference, tree.InOrder());
        Assert.Equal(reference.ElementAt(10), tree.ElementAt(10));
        Assert.Equal(reference.Count(x => x < 500), tree.Rank(500));
        Assert.Equal(reference.Contains(123), tree.Contains(123));
    }
}
