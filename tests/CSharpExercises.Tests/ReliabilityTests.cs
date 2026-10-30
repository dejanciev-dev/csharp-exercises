using CSharpExercises.Reliability;
using Microsoft.Extensions.Time.Testing;

namespace CSharpExercises.Tests;

public class ReliabilityTests
{
    [Fact]
    public void TokenBucket_AllowsBurstsThenRefills()
    {
        var time = new FakeTimeProvider();
        var bucket = new TokenBucket(capacity: 5, tokensPerSecond: 2, time);

        Assert.Equal(5, Enumerable.Range(0, 10).Count(_ => bucket.TryTake()));
        Assert.Equal(TimeSpan.FromSeconds(0.5), bucket.TimeUntilAvailable());

        time.Advance(TimeSpan.FromSeconds(1));
        Assert.True(bucket.TryTake(2));
        Assert.False(bucket.TryTake());

        time.Advance(TimeSpan.FromHours(1));
        Assert.Equal(5, bucket.Available); // capped at capacity
    }

    [Fact]
    public async Task CircuitBreaker_OpensAfterFailuresAndRecovers()
    {
        var time = new FakeTimeProvider();
        var breaker = new CircuitBreaker(failureThreshold: 3, TimeSpan.FromSeconds(30), time);
        var states = new List<CircuitState>();
        breaker.StateChanged += states.Add;
        var calls = 0;

        Task<int> Failing(CancellationToken _) { calls++; throw new HttpRequestException("down"); }
        Task<int> Working(CancellationToken _) { calls++; return Task.FromResult(42); }

        for (var i = 0; i < 3; i++) await Assert.ThrowsAsync<HttpRequestException>(() => breaker.ExecuteAsync(Failing));
        Assert.Equal(CircuitState.Open, breaker.State);

        var open = await Assert.ThrowsAsync<CircuitOpenException>(() => breaker.ExecuteAsync(Working));
        Assert.Equal(TimeSpan.FromSeconds(30), open.RetryAfter);
        Assert.Equal(3, calls); // failed fast, the dependency wasn't called

        time.Advance(TimeSpan.FromSeconds(30));
        Assert.Equal(CircuitState.HalfOpen, breaker.State);
        Assert.Equal(42, await breaker.ExecuteAsync(Working));
        Assert.Equal([CircuitState.Open, CircuitState.HalfOpen, CircuitState.Closed], states);
    }

    [Fact]
    public async Task CircuitBreaker_FailedTrialReopens()
    {
        var time = new FakeTimeProvider();
        var breaker = new CircuitBreaker(1, TimeSpan.FromSeconds(10), time);
        Task<int> Failing(CancellationToken _) => throw new TimeoutException();

        await Assert.ThrowsAsync<TimeoutException>(() => breaker.ExecuteAsync(Failing));
        time.Advance(TimeSpan.FromSeconds(10));
        await Assert.ThrowsAsync<TimeoutException>(() => breaker.ExecuteAsync(Failing));

        Assert.Equal(CircuitState.Open, breaker.State);
    }

    [Fact]
    public async Task Bulkhead_RejectsBeyondConcurrencyPlusQueue()
    {
        var bulkhead = new Bulkhead(maxConcurrency: 2, maxQueue: 1);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<int> Slow(CancellationToken _) { await gate.Task; return 1; }

        var accepted = Enumerable.Range(0, 3).Select(_ => bulkhead.ExecuteAsync(Slow)).ToArray();
        await Assert.ThrowsAsync<BulkheadRejectedException>(() => bulkhead.ExecuteAsync(Slow));

        Assert.Equal(2, bulkhead.Running);
        Assert.Equal(1, bulkhead.Rejected);

        gate.SetResult();
        var results = await Task.WhenAll(accepted);
        Assert.Equal([1, 1, 1], results);
        Assert.Equal(1, await bulkhead.ExecuteAsync(Slow)); // room again
    }

    [Fact]
    public void WeightedRoundRobin_SpreadsTheHeavyServer()
    {
        var balancer = new WeightedRoundRobin<string>([("a", 5), ("b", 1), ("c", 1)]);

        var picks = string.Concat(Enumerable.Range(0, 7).Select(_ => balancer.Next()));

        Assert.Equal("aabacaa", picks);
    }

    [Fact]
    public void WeightedRoundRobin_SkipsUnhealthyServers()
    {
        var balancer = new WeightedRoundRobin<string>([("a", 1), ("b", 1)]);
        balancer.SetHealthy("a", false);

        Assert.All(Enumerable.Range(0, 5), _ => Assert.Equal("b", balancer.Next()));

        balancer.SetHealthy("b", false);
        Assert.Throws<InvalidOperationException>(() => balancer.Next());
    }

    [Fact]
    public void TwoChoices_BalancesFarBetterThanRandom()
    {
        var twoChoices = new TwoChoicesBalancer(100, new Random(1));
        var random = new TwoChoicesBalancer(100, new Random(1));
        for (var i = 0; i < 10_000; i++)
        {
            twoChoices.Assign();
            random.AssignRandom();
        }

        // Average load is 100. Random placement overshoots by tens; two choices stays within a few.
        Assert.True(twoChoices.Load.Max() <= 103);
        Assert.True(random.Load.Max() >= 115);
    }

    [Fact]
    public void TimingWheel_FiresAtTheRightTick()
    {
        var wheel = new TimingWheel(slots: 8, TimeSpan.FromMilliseconds(100));
        var fired = new List<string>();

        wheel.Schedule(TimeSpan.FromMilliseconds(250), () => fired.Add("short"));
        wheel.Schedule(TimeSpan.FromMilliseconds(1_500), () => fired.Add("long")); // 15 ticks: almost two rotations
        var cancelled = wheel.Schedule(TimeSpan.FromMilliseconds(500), () => fired.Add("cancelled"));
        wheel.Cancel(cancelled);

        wheel.AdvanceBy(TimeSpan.FromMilliseconds(200));
        Assert.Empty(fired);
        wheel.Advance();
        Assert.Equal(["short"], fired);

        wheel.AdvanceBy(TimeSpan.FromMilliseconds(1_100));
        Assert.Equal(["short"], fired);
        wheel.Advance();
        Assert.Equal(["short", "long"], fired);
        Assert.Equal(0, wheel.Pending);
        Assert.True(cancelled.Cancelled);
    }

    [Theory]
    [InlineData("*/15 * * * *", "2026-10-30 10:07", "2026-10-30 10:15")]
    [InlineData("0 9 * * 1-5", "2026-10-30 10:00", "2026-11-02 09:00")]   // Friday after 9 → Monday
    [InlineData("30 2 1 * *", "2026-10-30 10:00", "2026-11-01 02:30")]
    [InlineData("0 0 29 2 *", "2026-03-01 00:00", "2028-02-29 00:00")]    // next leap day
    [InlineData("0 0 13 * 5", "2026-10-30 10:00", "2026-11-06 00:00")]    // the either/or rule: a Friday
    [InlineData("0 12 * * 7", "2026-10-30 10:00", "2026-11-01 12:00")]    // 7 is Sunday
    public void Cron_FindsTheNextRun(string expression, string after, string expected)
    {
        var next = CronSchedule.Parse(expression).Next(DateTime.Parse(after));
        Assert.Equal(DateTime.Parse(expected), next);
    }

    [Fact]
    public void Cron_ListsOccurrencesAndRejectsNonsense()
    {
        var times = CronSchedule.Parse("0 8,12,17 * * *").Occurrences(new DateTime(2026, 1, 1)).Take(4);
        Assert.Equal([8, 12, 17, 8], times.Select(t => t.Hour));

        Assert.Throws<FormatException>(() => CronSchedule.Parse("* * *"));
        Assert.Throws<FormatException>(() => CronSchedule.Parse("60 * * * *"));
        Assert.Throws<FormatException>(() => CronSchedule.Parse("*/0 * * * *"));
        Assert.Null(CronSchedule.Parse("0 0 31 2 *").Next(new DateTime(2026, 1, 1))); // February 31st never comes
    }
}
