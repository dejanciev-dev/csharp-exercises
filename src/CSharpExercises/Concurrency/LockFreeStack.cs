namespace CSharpExercises.Concurrency;

/// <summary>
/// Treiber's lock-free stack. Instead of locking, each operation reads the head, prepares the change, and publishes it
/// with one atomic compare-and-swap; if another thread changed the head in between, the swap fails and it tries again.
/// No thread ever waits for another to release a lock, so a thread paused by the OS cannot block the rest.
/// </summary>
/// <remarks>
/// In languages with manual memory management this algorithm suffers from the "ABA problem": a node is popped, freed,
/// reused, and pushed back, so a stale compare-and-swap succeeds when it should not. In .NET the garbage collector
/// cannot reuse a node while any thread still holds a reference to it, which rules that out.
/// </remarks>
public sealed class LockFreeStack<T>
{
    private sealed class Node(T value)
    {
        public readonly T Value = value;
        public Node? Next; // only written before the node is published
    }

    private Node? _head;

    public bool IsEmpty => Volatile.Read(ref _head) is null;

    public void Push(T value)
    {
        var node = new Node(value);
        var spinner = new SpinWait();
        while (true)
        {
            var head = Volatile.Read(ref _head);
            node.Next = head;
            if (Interlocked.CompareExchange(ref _head, node, head) == head) return;
            spinner.SpinOnce(); // back off a little under contention instead of hammering the same cache line
        }
    }

    public bool TryPop(out T value)
    {
        var spinner = new SpinWait();
        while (true)
        {
            var head = Volatile.Read(ref _head);
            if (head is null)
            {
                value = default!;
                return false;
            }

            if (Interlocked.CompareExchange(ref _head, head.Next, head) == head)
            {
                value = head.Value;
                return true;
            }
            spinner.SpinOnce();
        }
    }
}
