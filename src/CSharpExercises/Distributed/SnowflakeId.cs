namespace CSharpExercises.Distributed;

/// <summary>
/// Twitter's Snowflake: 64-bit ids generated on many machines without coordination, roughly sorted by time.
/// 41 bits of milliseconds since a custom epoch (69 years), 10 bits of worker id (1024 workers) and 12 bits of
/// sequence (4096 ids per millisecond per worker). If the sequence runs out, wait for the next millisecond; if the
/// clock goes backwards, refuse rather than risk duplicates.
/// </summary>
public sealed class SnowflakeId
{
    public const int WorkerBits = 10;
    public const int SequenceBits = 12;
    private const long MaxSequence = (1L << SequenceBits) - 1;

    private readonly TimeProvider _time;
    private readonly DateTimeOffset _epoch;
    private readonly long _workerId;
    private readonly Lock _lock = new();
    private long _lastMillis = -1;
    private long _sequence;

    public SnowflakeId(int workerId, DateTimeOffset epoch, TimeProvider? time = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(workerId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(workerId, 1 << WorkerBits);
        _workerId = workerId;
        _epoch = epoch;
        _time = time ?? TimeProvider.System;
    }

    public long Next()
    {
        lock (_lock)
        {
            var millis = Now();
            if (millis < _lastMillis) throw new InvalidOperationException($"Clock moved backwards by {_lastMillis - millis} ms.");

            if (millis == _lastMillis)
            {
                _sequence = (_sequence + 1) & MaxSequence;
                if (_sequence == 0)
                {
                    // 4096 ids this millisecond already: spin until the clock moves on.
                    while ((millis = Now()) <= _lastMillis) Thread.SpinWait(10);
                }
            }
            else
            {
                _sequence = 0;
            }

            _lastMillis = millis;
            return millis << (WorkerBits + SequenceBits) | _workerId << SequenceBits | _sequence;
        }
    }

    public (DateTimeOffset Time, int Worker, int Sequence) Decode(long id) => (
        _epoch.AddMilliseconds(id >> (WorkerBits + SequenceBits)),
        (int)(id >> SequenceBits & ((1 << WorkerBits) - 1)),
        (int)(id & MaxSequence));

    private long Now() => (long)(_time.GetUtcNow() - _epoch).TotalMilliseconds;
}
