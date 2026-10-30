namespace CSharpExercises.Diagnostics;

/// <summary>
/// Stopping log floods: when the same message repeats (a retry loop, a broken dependency), write it once and then a
/// single "last message repeated N times" line, like syslog does, instead of thousands of identical lines that bury
/// everything else and cost money to store. Repeats are counted within a time window, using <see cref="TimeProvider"/>.
/// </summary>
public sealed class LogDeduplicator(Action<string> sink, TimeSpan window, TimeProvider? time = null)
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private readonly Lock _lock = new();
    private string? _last;
    private int _repeats;
    private DateTimeOffset _firstSeen;

    public void Write(string message)
    {
        lock (_lock)
        {
            var now = _time.GetUtcNow();
            if (message == _last && now - _firstSeen < window)
            {
                _repeats++;
                return; // swallowed for now, counted
            }
            FlushSummary();
            sink(message);
            (_last, _firstSeen) = (message, now);
        }
    }

    /// <summary>Writes the pending summary, e.g. on shutdown or from a periodic timer.</summary>
    public void Flush()
    {
        lock (_lock)
        {
            FlushSummary();
            _last = null;
        }
    }

    private void FlushSummary()
    {
        if (_repeats > 0) sink($"last message repeated {_repeats} time{(_repeats == 1 ? "" : "s")}");
        _repeats = 0;
    }
}
