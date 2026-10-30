namespace CSharpExercises.Distributed;

/// <summary>
/// Lamport timestamps order events across machines without synchronised clocks: tick before every local event or
/// send, and on receipt jump past the sender's timestamp. If a caused b then time(a) &lt; time(b). The converse doesn't
/// hold, so ties are broken by node id to get one total order (used for distributed mutual exclusion).
/// </summary>
public sealed class LamportClock(string nodeId)
{
    public string NodeId { get; } = nodeId;
    public long Time { get; private set; }

    public long Tick() => ++Time;

    /// <summary>The timestamp to attach to an outgoing message.</summary>
    public long Send() => Tick();

    public long Receive(long messageTime)
    {
        Time = Math.Max(Time, messageTime) + 1;
        return Time;
    }

    /// <summary>A total order over (time, node) pairs that is consistent with causality.</summary>
    public static int Compare((long Time, string Node) a, (long Time, string Node) b) =>
        a.Time != b.Time ? a.Time.CompareTo(b.Time) : string.CompareOrdinal(a.Node, b.Node);
}
