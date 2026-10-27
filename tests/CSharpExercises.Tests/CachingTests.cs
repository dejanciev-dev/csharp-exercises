using CSharpExercises.Caching;
using Microsoft.Extensions.Time.Testing;

namespace CSharpExercises.Tests;

public class CachingTests
{
    private sealed record User(int Id, string Name);

    [Fact]
    public void Ttl_AbsoluteExpiration()
    {
        var clock = new FakeTimeProvider();
        var cache = new TtlCache<string, int>(clock);
        cache.Set("rate", 42, TimeSpan.FromMinutes(5));

        clock.Advance(TimeSpan.FromMinutes(4));
        Assert.True(cache.TryGet("rate", out var value));
        Assert.Equal(42, value);

        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.False(cache.TryGet("rate", out _));
        Assert.Equal((1, 1), (cache.Hits, cache.Misses));
    }

    [Fact]
    public void Ttl_SlidingExpirationIsExtendedByReads()
    {
        var clock = new FakeTimeProvider();
        var cache = new TtlCache<string, string>(clock);
        cache.Set("session", "ana", absoluteExpiration: TimeSpan.FromHours(8), slidingExpiration: TimeSpan.FromMinutes(20));

        // Read every 15 minutes for 2 hours: never 20 minutes idle, so it stays.
        for (var i = 0; i < 8; i++)
        {
            clock.Advance(TimeSpan.FromMinutes(15));
            Assert.True(cache.TryGet("session", out _));
        }

        clock.Advance(TimeSpan.FromMinutes(20));
        Assert.False(cache.TryGet("session", out _));
    }

    [Fact]
    public void Ttl_AbsoluteLimitAppliesEvenToBusyEntries()
    {
        var clock = new FakeTimeProvider();
        var cache = new TtlCache<string, string>(clock);
        cache.Set("config", "v1", absoluteExpiration: TimeSpan.FromMinutes(30), slidingExpiration: TimeSpan.FromMinutes(10));

        for (var i = 0; i < 5; i++)
        {
            clock.Advance(TimeSpan.FromMinutes(5));
            Assert.True(cache.TryGet("config", out _));
        }

        clock.Advance(TimeSpan.FromMinutes(5)); // 30 minutes since it was stored
        Assert.False(cache.TryGet("config", out _));
    }

    [Fact]
    public void Ttl_CompactRemovesExpiredEntriesNobodyAskedFor()
    {
        var clock = new FakeTimeProvider();
        var cache = new TtlCache<int, int>(clock);
        for (var i = 0; i < 10; i++) cache.Set(i, i, TimeSpan.FromMinutes(i < 7 ? 1 : 10));

        clock.Advance(TimeSpan.FromMinutes(2));

        Assert.Equal(10, cache.Count); // expiry is lazy
        Assert.Equal(7, cache.Compact());
        Assert.Equal(3, cache.Count);
    }

    [Fact]
    public async Task SingleFlight_ConcurrentCallersShareOneLoad()
    {
        var flight = new SingleFlight<string, int>();
        var release = new TaskCompletionSource<int>();
        var loads = 0;

        Task<int> Load(string _)
        {
            Interlocked.Increment(ref loads);
            return release.Task;
        }

        var callers = Enumerable.Range(0, 50).Select(_ => flight.RunAsync("popular", Load)).ToList();
        Assert.Equal(1, flight.InFlightCount);

        release.SetResult(7);
        var results = await Task.WhenAll(callers);

        Assert.Equal(1, loads);
        Assert.All(results, r => Assert.Equal(7, r));
        Assert.Equal(0, flight.InFlightCount);
    }

    [Fact]
    public async Task SingleFlight_NothingIsKeptAfterTheLoadFinishes()
    {
        var flight = new SingleFlight<string, int>();
        var loads = 0;

        await flight.RunAsync("k", _ => Task.FromResult(Interlocked.Increment(ref loads)));
        await flight.RunAsync("k", _ => Task.FromResult(Interlocked.Increment(ref loads)));

        Assert.Equal(2, loads);
    }

    [Fact]
    public async Task SingleFlight_AFailureReachesEveryWaiterAndTheNextCallRetries()
    {
        var flight = new SingleFlight<string, int>();
        var release = new TaskCompletionSource<int>();

        var first = flight.RunAsync("k", _ => release.Task);
        var second = flight.RunAsync("k", _ => Task.FromResult(-1)); // joins the first load; its own loader never runs
        release.SetException(new TimeoutException("database busy"));

        await Assert.ThrowsAsync<TimeoutException>(() => first);
        await Assert.ThrowsAsync<TimeoutException>(() => second);
        Assert.Equal(5, await flight.RunAsync("k", _ => Task.FromResult(5)));
    }

    [Fact]
    public async Task SingleFlight_OneCallerCancellingDoesNotCancelTheOthers()
    {
        var flight = new SingleFlight<string, int>();
        var release = new TaskCompletionSource<int>();
        using var impatient = new CancellationTokenSource();

        var patientCall = flight.RunAsync("k", _ => release.Task);
        var impatientCall = flight.RunAsync("k", _ => release.Task, impatient.Token);

        await impatient.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => impatientCall);

        release.SetResult(1);
        Assert.Equal(1, await patientCall);
    }

    [Fact]
    public async Task CachedLookup_LoadsOnceAndThenServesFromTheCache()
    {
        var clock = new FakeTimeProvider();
        var lookup = new CachedLookup<int, User>((id, _) => Task.FromResult<User?>(new User(id, $"user {id}")), clock, TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(30));

        for (var i = 0; i < 10; i++) Assert.Equal("user 1", (await lookup.GetAsync(1))?.Name);

        Assert.Equal(1, lookup.Loads);
        Assert.Equal(9, lookup.CacheHits);

        clock.Advance(TimeSpan.FromMinutes(5));
        await lookup.GetAsync(1);
        Assert.Equal(2, lookup.Loads);
    }

    [Fact]
    public async Task CachedLookup_CachesNotFoundForAShorterTime()
    {
        var clock = new FakeTimeProvider();
        var lookup = new CachedLookup<int, User>((_, _) => Task.FromResult<User?>(null), clock, TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(30));

        Assert.Null(await lookup.GetAsync(404));
        Assert.Null(await lookup.GetAsync(404));
        Assert.Equal(1, lookup.Loads);

        clock.Advance(TimeSpan.FromSeconds(30));
        await lookup.GetAsync(404);
        Assert.Equal(2, lookup.Loads);
    }

    [Fact]
    public async Task CachedLookup_InvalidateLoadsTheNewValue()
    {
        var names = new Dictionary<int, string> { [1] = "Ana" };
        var lookup = new CachedLookup<int, User>((id, _) => Task.FromResult<User?>(new User(id, names[id])), new FakeTimeProvider(), TimeSpan.FromHours(1), TimeSpan.FromMinutes(1));

        Assert.Equal("Ana", (await lookup.GetAsync(1))?.Name);
        names[1] = "Ana Markovska";
        Assert.Equal("Ana", (await lookup.GetAsync(1))?.Name); // stale until invalidated

        lookup.Invalidate(1);
        Assert.Equal("Ana Markovska", (await lookup.GetAsync(1))?.Name);
    }

    [Fact]
    public async Task CachedLookup_AStampedeOfMissesCausesOneLoad()
    {
        var release = new TaskCompletionSource<User?>();
        var lookup = new CachedLookup<int, User>((_, _) => release.Task, new FakeTimeProvider(), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(30));

        var requests = Enumerable.Range(0, 100).Select(_ => lookup.GetAsync(1)).ToList();
        release.SetResult(new User(1, "Ana"));
        await Task.WhenAll(requests);

        Assert.Equal(1, lookup.Loads);
    }
}
