using CSharpExercises.Collections;

namespace CSharpExercises.Tests;

public class ExtraCollectionsTests
{
    [Fact]
    public void Rope_EditsMatchStringOperations()
    {
        var random = new Random(1);
        var text = string.Concat(Enumerable.Range(0, 2000).Select(i => (char)('a' + i % 26)));
        var rope = Rope.From(text);

        for (var i = 0; i < 300; i++)
        {
            var position = random.Next(text.Length + 1);
            if (random.Next(2) == 0)
            {
                var insert = $"<{i}>";
                text = text.Insert(position, insert);
                rope = rope.Insert(position, insert);
            }
            else
            {
                var count = random.Next(Math.Min(20, text.Length - position) + 1);
                text = text.Remove(position, count);
                rope = rope.Delete(position, count);
            }
        }

        Assert.Equal(text, rope.ToString());
        Assert.Equal(text.Length, rope.Length);
        Assert.Equal(text[1234], rope[1234]);
        Assert.Equal(text.Substring(500, 40), rope.Substring(500, 40));
        Assert.True(rope.Rebalance().Depth <= rope.Depth);
    }

    [Fact]
    public void Rope_OldVersionsSurviveEdits()
    {
        var v1 = Rope.From("Hello world");
        var v2 = v1.Insert(5, ",");
        var v3 = v2.Delete(0, 7).Insert(0, "Goodbye ");

        Assert.Equal("Hello world", v1.ToString());
        Assert.Equal("Hello, world", v2.ToString());
        Assert.Equal("Goodbye world", v3.ToString());
        Assert.Equal("", Rope.Empty.ToString());
        Assert.Throws<ArgumentOutOfRangeException>(() => v1.Split(100));
    }

    [Fact]
    public void OrderedMap_KeepsInsertionOrderThroughRemovals()
    {
        var map = new InsertionOrderedMap<string, int> { ["c"] = 3, ["a"] = 1, ["b"] = 2 };
        map.Remove("a");
        map["d"] = 4;
        map["c"] = 30; // update keeps position
        map["a"] = 10; // re-adding goes to the end

        Assert.Equal(["c", "b", "d", "a"], map.Keys);
        Assert.Equal(30, map["c"]);
        Assert.Throws<KeyNotFoundException>(() => map["zzz"]);
    }

    [Fact]
    public void OrderedMap_BecomesAnLruCache()
    {
        var cache = new InsertionOrderedMap<string, string>(moveToEndOnAccess: true) { ["x"] = "1", ["y"] = "2", ["z"] = "3" };

        cache.TryGetValue("x", out _); // x is now the most recently used
        var evicted = cache.RemoveFirst();

        Assert.Equal("y", evicted.Key);
        Assert.Equal(["z", "x"], cache.Keys);
    }

    [Fact]
    public void IndexedQueue_ChangesPriorities()
    {
        var queue = new IndexedPriorityQueue<string, int>();
        queue.Set("a", 5);
        queue.Set("b", 3);
        queue.Set("c", 8);
        queue.Set("c", 1); // decrease
        queue.Set("b", 9); // increase
        queue.Remove("a");
        queue.Set("d", 4);

        Assert.Equal(3, queue.Count);
        Assert.Equal(["c", "d", "b"], Enumerable.Range(0, 3).Select(_ => queue.Dequeue().Item));
        Assert.Throws<InvalidOperationException>(() => queue.Dequeue());
    }

    [Fact]
    public void IndexedQueue_MatchesASortedReferenceUnderRandomUpdates()
    {
        var random = new Random(2);
        var queue = new IndexedPriorityQueue<int, int>();
        var reference = new Dictionary<int, int>();
        for (var i = 0; i < 2000; i++)
        {
            var item = random.Next(100);
            var priority = random.Next(1000);
            queue.Set(item, priority);
            reference[item] = priority;
        }

        var drained = new List<int>();
        while (queue.Count > 0) drained.Add(queue.Dequeue().Priority);

        Assert.Equal(reference.Values.Order(), drained);
    }

    [Fact]
    public void IndexedQueue_RunsDijkstra()
    {
        var graph = new Dictionary<string, (string, int)[]>
        {
            ["A"] = [("B", 4), ("C", 1)],
            ["C"] = [("B", 2), ("D", 7)],
            ["B"] = [("D", 1)],
        };

        Assert.Equal(new Dictionary<string, int> { ["A"] = 0, ["C"] = 1, ["B"] = 3, ["D"] = 4 }, IndexedPriorityQueue<string, int>.ShortestDistances(graph, "A"));
    }

    [Fact]
    public void Bag_CountsAndCombines()
    {
        var words = new Bag<string>("the cat and the hat and the bat".Split(' '));

        Assert.Equal(3, words["the"]);
        Assert.Equal(0, words["dog"]);
        Assert.Equal(8, words.Total);
        Assert.Equal([("the", 3), ("and", 2)], words.MostCommon(2));

        var a = new Bag<char>("aabbbc");
        var b = new Bag<char>("abbbbd");
        Assert.Equal("aabbbbcd", string.Concat(a.Union(b).Expand().Order()));
        Assert.Equal("abbb", string.Concat(a.Intersect(b).Expand().Order()));
        Assert.Equal("ac", string.Concat(a.Except(b).Expand().Order()));
        Assert.Equal(12, a.Sum(b).Total);
    }

    [Fact]
    public void Bag_ChecksARansomNote()
    {
        var magazine = new Bag<char>("give me one grand today night".Replace(" ", ""));

        Assert.True(magazine.Contains(new Bag<char>("give one grand today".Replace(" ", ""))));
        Assert.False(magazine.Contains(new Bag<char>("give two grand tomorrow".Replace(" ", ""))));
        Assert.Equal(1, magazine.Remove('g', 1));
        Assert.Equal(2, magazine.Remove('g', 5)); // only two left
    }
}
