using CSharpExercises.Testing;
using Microsoft.Extensions.Time.Testing;

namespace CSharpExercises.Tests.Testing;

/// <summary>
/// Testing time-dependent code with FakeTimeProvider: the clock only moves when the test calls Advance,
/// so there is no Thread.Sleep, the tests run instantly, and they never fail because a build machine was slow.
/// </summary>
public class TimeBasedTests
{
    private readonly FakeTimeProvider _time = new(SubscriptionBuilder.Now);

    [Fact]
    public void RateLimiter_AllowsBurstThenBlocksUntilWindowPasses()
    {
        var limiter = new SlidingWindowRateLimiter(maxRequests: 3, window: TimeSpan.FromMinutes(1), _time);

        Assert.True(limiter.TryAcquire());
        _time.Advance(TimeSpan.FromSeconds(20));
        Assert.True(limiter.TryAcquire());
        Assert.True(limiter.TryAcquire());
        Assert.False(limiter.TryAcquire());
        Assert.Equal(TimeSpan.FromSeconds(40), limiter.RetryAfter);

        _time.Advance(TimeSpan.FromSeconds(39));
        Assert.False(limiter.TryAcquire());

        _time.Advance(TimeSpan.FromSeconds(1)); // the first request is now a minute old
        Assert.True(limiter.TryAcquire());
        Assert.False(limiter.TryAcquire());
    }

    [Fact]
    public void RateLimiter_RetryAfterIsZeroWhenAllowed() =>
        Assert.Equal(TimeSpan.Zero, new SlidingWindowRateLimiter(1, TimeSpan.FromSeconds(1), _time).RetryAfter);

    [Fact]
    public void Debouncer_RunsOnlyAfterAQuietPeriod()
    {
        var searches = 0;
        using var debouncer = new Debouncer(TimeSpan.FromMilliseconds(300), () => searches++, _time);

        debouncer.Trigger();                              // user types "c"
        _time.Advance(TimeSpan.FromMilliseconds(200));
        debouncer.Trigger();                              // "ca": countdown restarts
        _time.Advance(TimeSpan.FromMilliseconds(299));
        Assert.Equal(0, searches);

        _time.Advance(TimeSpan.FromMilliseconds(1));      // 300 ms since the last keystroke
        Assert.Equal(1, searches);

        _time.Advance(TimeSpan.FromSeconds(10));          // nothing more typed: no more searches
        Assert.Equal(1, searches);
    }

    [Fact]
    public void Debouncer_CanFireAgainAfterNewInput()
    {
        var runs = 0;
        using var debouncer = new Debouncer(TimeSpan.FromSeconds(1), () => runs++, _time);

        debouncer.Trigger();
        _time.Advance(TimeSpan.FromSeconds(1));
        debouncer.Trigger();
        _time.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(2, runs);
    }

    [Fact]
    public void Debouncer_DisposeCancelsPendingRun()
    {
        var runs = 0;
        var debouncer = new Debouncer(TimeSpan.FromSeconds(1), () => runs++, _time);

        debouncer.Trigger();
        debouncer.Dispose();
        _time.Advance(TimeSpan.FromSeconds(5));

        Assert.Equal(0, runs);
        Assert.Throws<ObjectDisposedException>(debouncer.Trigger);
    }
}
