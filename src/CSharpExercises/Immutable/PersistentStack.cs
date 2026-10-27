using System.Collections;

namespace CSharpExercises.Immutable;

/// <summary>
/// An immutable stack where every "change" returns a new stack and the old one stays valid. The new stack shares all
/// of the old one's nodes (structural sharing), so <see cref="Push"/> and <see cref="Pop"/> are O(1) and copy nothing.
/// It is a singly linked list, the "cons list" of functional languages.
/// </summary>
public sealed class PersistentStack<T> : IEnumerable<T>
{
    public static readonly PersistentStack<T> Empty = new(default!, null, 0);

    private readonly T _head;
    private readonly PersistentStack<T>? _tail;

    private PersistentStack(T head, PersistentStack<T>? tail, int count)
    {
        _head = head;
        _tail = tail;
        Count = count;
    }

    public int Count { get; }
    public bool IsEmpty => Count == 0;

    public T Peek() => !IsEmpty ? _head : throw new InvalidOperationException("The stack is empty.");

    /// <summary>A new stack with <paramref name="value"/> on top; this stack becomes its tail, unchanged.</summary>
    public PersistentStack<T> Push(T value) => new(value, this, Count + 1);

    /// <summary>The stack below the top: an existing object, so nothing is allocated.</summary>
    public PersistentStack<T> Pop() => !IsEmpty ? _tail! : throw new InvalidOperationException("The stack is empty.");

    public PersistentStack<T> Reverse()
    {
        var reversed = Empty;
        for (var node = this; !node.IsEmpty; node = node._tail!) reversed = reversed.Push(node._head);
        return reversed;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (var node = this; !node.IsEmpty; node = node._tail!) yield return node._head;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// An immutable FIFO queue made of two persistent stacks: items are pushed onto the back stack and popped from the
/// front stack, and when the front runs out the back is reversed to become the new front. Each item is moved at most
/// once, so enqueue and dequeue are O(1) amortised.
/// </summary>
/// <remarks>
/// The amortised bound assumes each version is used once. Dequeuing the same old version over and over can repeat an
/// expensive reversal each time; Okasaki's "banker's queue" fixes that with lazy evaluation.
/// </remarks>
public sealed class PersistentQueue<T> : IEnumerable<T>
{
    public static readonly PersistentQueue<T> Empty = new(PersistentStack<T>.Empty, PersistentStack<T>.Empty);

    private readonly PersistentStack<T> _front;
    private readonly PersistentStack<T> _back;

    private PersistentQueue(PersistentStack<T> front, PersistentStack<T> back)
    {
        // Invariant: the front is only empty when the whole queue is, so Peek never has to reverse.
        (_front, _back) = front.IsEmpty ? (back.Reverse(), PersistentStack<T>.Empty) : (front, back);
    }

    public int Count => _front.Count + _back.Count;
    public bool IsEmpty => _front.IsEmpty;

    public T Peek() => !IsEmpty ? _front.Peek() : throw new InvalidOperationException("The queue is empty.");

    public PersistentQueue<T> Enqueue(T value) => new(_front, _back.Push(value));

    public PersistentQueue<T> Dequeue() =>
        !IsEmpty ? new(_front.Pop(), _back) : throw new InvalidOperationException("The queue is empty.");

    public IEnumerator<T> GetEnumerator() => _front.Concat(_back.Reverse()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
