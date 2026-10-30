namespace CSharpExercises.Reliability;

/// <summary>
/// Token bucket rate limiting: tokens drip in at a steady rate up to a maximum, and each request spends some.
/// Unlike a fixed window it allows short bursts (up to the capacity) while holding the long-run rate. Tokens are
/// refilled lazily from the elapsed time on each call, so there's no background timer.
/// </summary>
public sealed class TokenBucket
{
    private readonly TimeProvider _time;
    private readonly double _capacity;
    private readonly double _tokensPerSecond;
    private readonly Lock _lock = new();
    private double _tokens;
    private long _lastRefill;

    public TokenBucket(double capacity, double tokensPerSecond, TimeProvider? time = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tokensPerSecond);
        _time = time ?? TimeProvider.System;
        _capacity = capacity;
        _tokensPerSecond = tokensPerSecond;
        _tokens = capacity;
        _lastRefill = _time.GetTimestamp();
    }

    public double Available
    {
        get
        {
            lock (_lock)
            {
                Refill();
                return _tokens;
            }
        }
    }

    public bool TryTake(double tokens = 1)
    {
        lock (_lock)
        {
            Refill();
            if (_tokens < tokens) return false;
            _tokens -= tokens;
            return true;
        }
    }

    /// <summary>How long until <paramref name="tokens"/> are available: what to put in a Retry-After header.</summary>
    public TimeSpan TimeUntilAvailable(double tokens = 1)
    {
        if (tokens > _capacity) throw new ArgumentOutOfRangeException(nameof(tokens), "More than the bucket can ever hold.");
        lock (_lock)
        {
            Refill();
            return _tokens >= tokens ? TimeSpan.Zero : TimeSpan.FromSeconds((tokens - _tokens) / _tokensPerSecond);
        }
    }

    private void Refill()
    {
        var now = _time.GetTimestamp();
        var elapsed = _time.GetElapsedTime(_lastRefill, now);
        _tokens = Math.Min(_capacity, _tokens + elapsed.TotalSeconds * _tokensPerSecond);
        _lastRefill = now;
    }
}
