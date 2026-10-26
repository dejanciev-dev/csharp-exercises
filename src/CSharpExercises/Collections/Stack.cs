namespace CSharpExercises.Collections;

/// <summary>
/// A generic stack implemented from scratch on top of a growable array.
/// </summary>
public class Stack<T>
{
    private T[] _items = new T[4];

    public int Count { get; private set; }

    public bool IsEmpty => Count == 0;

    public void Push(T item)
    {
        if (Count == _items.Length) Array.Resize(ref _items, _items.Length * 2);
        _items[Count++] = item;
    }

    public T Pop()
    {
        var item = Peek();
        _items[--Count] = default!;
        return item;
    }

    public T Peek() =>
        IsEmpty ? throw new InvalidOperationException("Stack is empty.") : _items[Count - 1];
}
