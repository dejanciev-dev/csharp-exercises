namespace CSharpExercises.Reliability;

public enum CircuitState { Closed, Open, HalfOpen }

public sealed class CircuitOpenException(TimeSpan retryAfter)
    : Exception($"The circuit is open; retry after {retryAfter.TotalSeconds:0.#} s.")
{
    public TimeSpan RetryAfter { get; } = retryAfter;
}

/// <summary>
/// A circuit breaker stops calling a dependency that keeps failing, so callers fail fast instead of piling up
/// behind timeouts, and the dependency gets room to recover.
/// Closed: calls go through; after N consecutive failures it opens.
/// Open: calls fail immediately until the break duration passes.
/// Half-open: one trial call is let through; success closes the circuit, failure opens it again.
/// </summary>
public sealed class CircuitBreaker(int failureThreshold, TimeSpan breakDuration, TimeProvider? time = null)
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private readonly Lock _lock = new();
    private int _failures;
    private DateTimeOffset _openedAt;
    private bool _trialInFlight;
    private CircuitState _state = CircuitState.Closed;

    public CircuitState State
    {
        get
        {
            lock (_lock) return CurrentState();
        }
    }

    public event Action<CircuitState>? StateChanged;

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            switch (CurrentState())
            {
                case CircuitState.Open:
                    throw new CircuitOpenException(_openedAt + breakDuration - _time.GetUtcNow());
                case CircuitState.HalfOpen when _trialInFlight:
                    throw new CircuitOpenException(TimeSpan.Zero); // someone else is already testing the waters
                case CircuitState.HalfOpen:
                    _trialInFlight = true;
                    break;
            }
        }

        try
        {
            var result = await action(cancellationToken);
            lock (_lock)
            {
                _failures = 0;
                _trialInFlight = false;
                Transition(CircuitState.Closed);
            }
            return result;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested) // our own cancellation isn't their failure
        {
            lock (_lock)
            {
                _trialInFlight = false;
                if (_state == CircuitState.HalfOpen || ++_failures >= failureThreshold)
                {
                    _openedAt = _time.GetUtcNow();
                    Transition(CircuitState.Open);
                }
            }
            throw;
        }
    }

    private CircuitState CurrentState()
    {
        if (_state == CircuitState.Open && _time.GetUtcNow() >= _openedAt + breakDuration) Transition(CircuitState.HalfOpen);
        return _state;
    }

    private void Transition(CircuitState state)
    {
        if (_state == state) return;
        _state = state;
        if (state == CircuitState.Closed) _failures = 0;
        StateChanged?.Invoke(state);
    }
}
