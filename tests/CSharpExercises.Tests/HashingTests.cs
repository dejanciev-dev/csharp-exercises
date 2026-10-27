using CSharpExercises.Hashing;

namespace CSharpExercises.Tests;

public class HashingTests
{
    /// <summary>The worst possible hash: every key collides, so every lookup walks the probe chain.</summary>
    private sealed class EverythingCollides : IEqualityComparer<string>
    {
        public bool Equals(string? x, string? y) => x == y;
        public int GetHashCode(string obj) => 0;
    }

    [Theory]
    [InlineData("", 0xcbf29ce484222325)]
    [InlineData("a", 0xaf63dc4c8601ec8c)]
    [InlineData("foobar", 0x85944171f73967e8)]
    public void Fnv1a_MatchesTheReferenceTestVectors(string input, ulong expected)
    {
        Assert.Equal(expected, StableHash.Fnv1a64(input));
    }

    [Fact]
    public void Mix_SpreadsSimilarInputsApart()
    {
        // FNV of "node-1" and "node-2" differ only a little; after mixing, about half of the 64 bits differ.
        var raw = StableHash.Fnv1a64("node-1") ^ StableHash.Fnv1a64("node-2");
        var mixed = StableHash.Of("node-1") ^ StableHash.Of("node-2");

        Assert.True(System.Numerics.BitOperations.PopCount(raw) < 16);
        Assert.InRange(System.Numerics.BitOperations.PopCount(mixed), 20, 44);
    }

    [Fact]
    public void Map_BasicOperations()
    {
        var map = new OpenAddressingMap<string, int>();

        Assert.True(map.TryAdd("one", 1));
        Assert.False(map.TryAdd("one", 100));
        map["two"] = 2;
        map["two"] = 22;

        Assert.Equal(1, map["one"]);
        Assert.Equal(22, map["two"]);
        Assert.Equal(2, map.Count);
        Assert.Throws<KeyNotFoundException>(() => map["three"]);

        Assert.True(map.Remove("one"));
        Assert.False(map.Remove("one"));
        Assert.False(map.ContainsKey("one"));
        Assert.Equal([new KeyValuePair<string, int>("two", 22)], map);
    }

    [Fact]
    public void Map_RemovingFromTheMiddleOfAProbeChainKeepsLaterKeysReachable()
    {
        var map = new OpenAddressingMap<string, int>(comparer: new EverythingCollides());
        map["a"] = 1;
        map["b"] = 2;
        map["c"] = 3; // all three share one slot, so they sit next to each other: a, b, c

        map.Remove("b");

        Assert.Equal(3, map["c"]); // had b's slot been emptied, the search would stop there and miss c
        map["d"] = 4;             // reuses b's tombstone
        Assert.Equal([1, 4, 3], map.Select(p => p.Value));
    }

    [Fact]
    public void Map_GrowsAsItFills()
    {
        var map = new OpenAddressingMap<int, int>(capacity: 2);
        for (var i = 0; i < 1_000; i++) map[i] = i * i;

        Assert.Equal(1_000, map.Count);
        Assert.Equal(2048, map.Capacity); // 1,000 / 0.7 needs more than 1,024 slots
        Assert.Equal(998_001, map[999]);
    }

    [Fact]
    public void Map_TombstonesDoNotMakeItGrowForever()
    {
        var map = new OpenAddressingMap<int, string>();

        // Always at most one live entry, but 100,000 different keys pass through.
        for (var i = 0; i < 100_000; i++)
        {
            map[i] = "x";
            map.Remove(i);
        }

        Assert.Equal(0, map.Count);
        Assert.Equal(8, map.Capacity);
    }

    [Fact]
    public void Map_BehavesLikeDictionaryUnderRandomOperations()
    {
        var random = new Random(2026);
        var map = new OpenAddressingMap<int, int>();
        var reference = new Dictionary<int, int>();

        for (var step = 0; step < 20_000; step++)
        {
            var key = random.Next(500);
            switch (random.Next(3))
            {
                case 0:
                    map[key] = step;
                    reference[key] = step;
                    break;
                case 1:
                    Assert.Equal(reference.Remove(key), map.Remove(key));
                    break;
                default:
                    Assert.Equal(reference.TryGetValue(key, out var expected), map.TryGetValue(key, out var actual));
                    Assert.Equal(expected, actual);
                    break;
            }
        }

        Assert.Equal(reference.OrderBy(p => p.Key), map.OrderBy(p => p.Key));
    }

    [Fact]
    public void Bloom_NeverGivesAFalseNegative()
    {
        var filter = new BloomFilter(expectedItems: 10_000, falsePositiveRate: 0.01);
        var added = Enumerable.Range(0, 10_000).Select(i => $"user-{i}@example.com").ToList();
        added.ForEach(filter.Add);

        Assert.All(added, item => Assert.True(filter.MightContain(item)));
    }

    [Fact]
    public void Bloom_FalsePositiveRateIsCloseToTheTarget()
    {
        var filter = new BloomFilter(expectedItems: 10_000, falsePositiveRate: 0.01);
        for (var i = 0; i < 10_000; i++) filter.Add($"user-{i}@example.com");

        var falsePositives = Enumerable.Range(0, 20_000).Count(i => filter.MightContain($"visitor-{i}@example.org"));
        var rate = falsePositives / 20_000.0;

        Assert.InRange(rate, 0.002, 0.02);
        Assert.Equal(0.01, filter.ExpectedFalsePositiveRate(10_000), precision: 3);
    }

    [Fact]
    public void Bloom_IsSmall()
    {
        var filter = new BloomFilter(expectedItems: 1_000_000, falsePositiveRate: 0.01);

        // About 9.6 bits per item and 7 hash functions, whatever the size of the items themselves.
        Assert.Equal(7, filter.HashCount);
        Assert.InRange(filter.SizeInBytes, 1_150_000, 1_250_000);
    }

    [Fact]
    public void Ring_SpreadsKeysEvenly()
    {
        var ring = new ConsistentHashRing<string>();
        foreach (var server in new[] { "cache-a", "cache-b", "cache-c", "cache-d" }) ring.Add(server);

        var shares = Enumerable.Range(0, 40_000).CountBy(i => ring.GetNode($"session:{i}")).ToDictionary();

        Assert.Equal(4, shares.Count);
        Assert.All(shares.Values, count => Assert.InRange(count / 40_000.0, 0.18, 0.32));
    }

    [Fact]
    public void Ring_AddingAServerMovesFewKeysAndOnlyToIt()
    {
        string[] servers = ["cache-a", "cache-b", "cache-c", "cache-d"];
        var keys = Enumerable.Range(0, 40_000).Select(i => $"session:{i}").ToList();

        var ring = new ConsistentHashRing<string>();
        foreach (var server in servers) ring.Add(server);
        var before = keys.ToDictionary(k => k, ring.GetNode);

        ring.Add("cache-e");
        var moved = keys.Where(k => ring.GetNode(k) != before[k]).ToList();

        Assert.InRange(moved.Count / (double)keys.Count, 0.12, 0.28);
        Assert.All(moved, k => Assert.Equal("cache-e", ring.GetNode(k)));

        // The same change with hash % serverCount moves most keys.
        var movedWithModulo = keys.Count(k => StableHash.Of(k) % 4 != StableHash.Of(k) % 5);
        Assert.True(movedWithModulo / (double)keys.Count > 0.7);
    }

    [Fact]
    public void Ring_RemovingAServerOnlyMovesItsKeys()
    {
        var ring = new ConsistentHashRing<string>();
        foreach (var server in new[] { "a", "b", "c" }) ring.Add(server);
        var keys = Enumerable.Range(0, 5_000).Select(i => $"k{i}").ToList();
        var before = keys.ToDictionary(k => k, ring.GetNode);

        ring.Remove("b");

        Assert.All(keys.Where(k => before[k] != "b"), k => Assert.Equal(before[k], ring.GetNode(k)));
        Assert.DoesNotContain("b", keys.Select(ring.GetNode));
    }

    [Fact]
    public void Ring_IsTheSameInEveryProcess()
    {
        var first = new ConsistentHashRing<string>();
        var second = new ConsistentHashRing<string>();
        foreach (var server in new[] { "x", "y", "z" }) first.Add(server);
        foreach (var server in new[] { "z", "x", "y" }) second.Add(server); // order of adding does not matter

        Assert.All(Enumerable.Range(0, 1_000), i => Assert.Equal(first.GetNode($"k{i}"), second.GetNode($"k{i}")));
    }

    [Fact]
    public void Ring_WithoutServersThrows()
    {
        Assert.Throws<InvalidOperationException>(() => new ConsistentHashRing<string>().GetNode("key"));
    }
}
