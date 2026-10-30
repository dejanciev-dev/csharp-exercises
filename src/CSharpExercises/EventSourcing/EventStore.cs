namespace CSharpExercises.EventSourcing;

public sealed record StoredEvent(string StreamId, long Version, long Position, object Data, DateTimeOffset RecordedAt);

public sealed class ConcurrencyException(string streamId, long expected, long actual)
    : Exception($"Stream '{streamId}' is at version {actual}, but the writer expected {expected}.")
{
    public long Expected { get; } = expected;
    public long Actual { get; } = actual;
}

/// <summary>
/// An append-only event store. State is never overwritten: every change is an event appended to its stream, and the
/// current state is whatever the events add up to. Writers say which version they read (optimistic concurrency), so
/// two people editing the same thing can't silently overwrite each other. A global position orders all events for
/// projections that read everything.
/// </summary>
public sealed class InMemoryEventStore(TimeProvider? time = null)
{
    public const long NoStream = 0;

    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private readonly List<StoredEvent> _all = [];
    private readonly Dictionary<string, List<StoredEvent>> _streams = new();
    private readonly Lock _lock = new();

    public event Action<StoredEvent>? Appended;

    /// <returns>The stream's new version.</returns>
    public long Append(string streamId, long expectedVersion, IEnumerable<object> events)
    {
        List<StoredEvent> written;
        lock (_lock)
        {
            var stream = _streams.GetValueOrDefault(streamId) ?? [];
            var current = stream.Count;
            if (current != expectedVersion) throw new ConcurrencyException(streamId, expectedVersion, current);

            written = events.Select((e, i) => new StoredEvent(streamId, current + i + 1, _all.Count + i + 1, e, _time.GetUtcNow())).ToList();
            stream.AddRange(written);
            _streams[streamId] = stream;
            _all.AddRange(written);
        }
        foreach (var e in written) Appended?.Invoke(e); // notify outside the lock
        return expectedVersion + written.Count;
    }

    public IReadOnlyList<StoredEvent> ReadStream(string streamId, long afterVersion = 0)
    {
        lock (_lock) return (_streams.GetValueOrDefault(streamId) ?? []).Where(e => e.Version > afterVersion).ToList();
    }

    public IReadOnlyList<StoredEvent> ReadAll(long afterPosition = 0)
    {
        lock (_lock) return _all.Skip((int)afterPosition).ToList();
    }

    public long Version(string streamId)
    {
        lock (_lock) return _streams.GetValueOrDefault(streamId)?.Count ?? NoStream;
    }
}
