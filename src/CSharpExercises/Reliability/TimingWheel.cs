namespace CSharpExercises.Reliability;

/// <summary>
/// A hashed timing wheel schedules huge numbers of timeouts (one per connection or request) with O(1) add and
/// cancel. Time is split into ticks; a timeout goes into slot (now + delay) mod size, with a count of full
/// rotations still to wait. Each tick only looks at one slot. Netty and Kafka use this for their timers.
/// </summary>
public sealed class TimingWheel(int slots, TimeSpan tickDuration)
{
    public sealed class Timeout
    {
        internal Timeout(Action callback, long rounds, int slot) => (Callback, Rounds, Slot) = (callback, rounds, slot);
        internal Action Callback { get; }
        internal long Rounds { get; set; }
        internal int Slot { get; }
        public bool Cancelled { get; internal set; }
        public bool Fired { get; internal set; }
    }

    private readonly LinkedList<Timeout>[] _wheel = Enumerable.Range(0, slots).Select(_ => new LinkedList<Timeout>()).ToArray();
    private long _tick;

    public long CurrentTick => _tick;

    public int Pending => _wheel.Sum(s => s.Count);

    public Timeout Schedule(TimeSpan delay, Action callback)
    {
        var ticks = Math.Max(1, (long)Math.Ceiling(delay / tickDuration)); // never fire in the current tick
        var target = _tick + ticks;
        var timeout = new Timeout(callback, (ticks - 1) / slots, (int)(target % slots));
        _wheel[timeout.Slot].AddLast(timeout);
        return timeout;
    }

    public void Cancel(Timeout timeout)
    {
        if (timeout.Fired || timeout.Cancelled) return;
        timeout.Cancelled = true;
        _wheel[timeout.Slot].Remove(timeout);
    }

    /// <summary>Advances one tick and fires what is due. A real implementation calls this from a timer thread.</summary>
    public int Advance()
    {
        _tick++;
        var slot = _wheel[_tick % slots];
        var fired = 0;
        for (var node = slot.First; node is not null;)
        {
            var next = node.Next;
            var timeout = node.Value;
            if (timeout.Rounds > 0)
            {
                timeout.Rounds--;
            }
            else
            {
                slot.Remove(node);
                timeout.Fired = true;
                timeout.Callback();
                fired++;
            }
            node = next;
        }
        return fired;
    }

    public void AdvanceBy(TimeSpan elapsed)
    {
        for (var i = 0L; i < (long)(elapsed / tickDuration); i++) Advance();
    }
}
