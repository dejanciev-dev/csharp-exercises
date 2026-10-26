using System.Collections;

namespace CSharpExercises.Collections;

/// <summary>
/// A singly linked list built from scratch, with the classic in-place reverse and fast/slow pointer exercises.
/// </summary>
public class SinglyLinkedList<T> : IEnumerable<T>
{
    private sealed class Node(T value)
    {
        public T Value { get; } = value;
        public Node? Next { get; set; }
    }

    private Node? _head;
    private Node? _tail;

    public int Count { get; private set; }

    public void AddFirst(T value)
    {
        _head = new Node(value) { Next = _head };
        _tail ??= _head;
        Count++;
    }

    public void AddLast(T value)
    {
        var node = new Node(value);
        if (_tail is null)
        {
            _head = node;
        }
        else
        {
            _tail.Next = node;
        }

        _tail = node;
        Count++;
    }

    /// <summary>Removes the first occurrence of <paramref name="value"/>.</summary>
    public bool Remove(T value)
    {
        Node? previous = null;
        for (var current = _head; current is not null; previous = current, current = current.Next)
        {
            if (!EqualityComparer<T>.Default.Equals(current.Value, value)) continue;

            if (previous is null) _head = current.Next;
            else previous.Next = current.Next;

            if (current == _tail) _tail = previous;
            Count--;
            return true;
        }

        return false;
    }

    public bool Contains(T value) => Enumerable.Contains(this, value);

    /// <summary>Reverses the list in place by flipping every Next pointer: O(n) time, O(1) extra space.</summary>
    public void Reverse()
    {
        _tail = _head;
        Node? previous = null;
        var current = _head;
        while (current is not null)
        {
            var next = current.Next;
            current.Next = previous;
            previous = current;
            current = next;
        }

        _head = previous;
    }

    /// <summary>
    /// Finds the middle element in one pass: the fast pointer moves two steps for every step of the slow one.
    /// For an even count, returns the second of the two middle elements.
    /// </summary>
    public T Middle()
    {
        if (_head is null) throw new InvalidOperationException("List is empty.");

        Node slow = _head;
        var fast = _head;
        while (fast?.Next is not null)
        {
            slow = slow.Next!;
            fast = fast.Next.Next;
        }

        return slow.Value;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (var current = _head; current is not null; current = current.Next)
        {
            yield return current.Value;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
