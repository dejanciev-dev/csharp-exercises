using System.Collections.Immutable;

namespace CSharpExercises.FunctionalProgramming;

/// <summary>
/// A list zipper: an immutable list with a cursor, stored as the items before it (nearest first), the focus, and the
/// items after it. Moving and editing at the cursor are O(1) and every older version stays valid, which suits text
/// editors, carousels and undoable navigation.
/// </summary>
public sealed record Zipper<T>(ImmutableStack<T> Before, T Focus, ImmutableStack<T> After)
{
    public static Zipper<T> FromList(IEnumerable<T> items)
    {
        using var e = items.GetEnumerator();
        if (!e.MoveNext()) throw new ArgumentException("A zipper needs at least one item.", nameof(items));
        var first = e.Current;

        var rest = new List<T>();
        while (e.MoveNext()) rest.Add(e.Current);
        return new Zipper<T>(ImmutableStack<T>.Empty, first, ImmutableStack.CreateRange(Enumerable.Reverse(rest)));
    }

    public int Index => Before.Count();

    public bool CanMoveLeft => !Before.IsEmpty;
    public bool CanMoveRight => !After.IsEmpty;

    public Zipper<T> Left() =>
        CanMoveLeft ? new(Before.Pop(), Before.Peek(), After.Push(Focus)) : throw new InvalidOperationException("Already at the start.");

    public Zipper<T> Right() =>
        CanMoveRight ? new(Before.Push(Focus), After.Peek(), After.Pop()) : throw new InvalidOperationException("Already at the end.");

    public Zipper<T> Replace(T value) => this with { Focus = value };

    public Zipper<T> InsertAfter(T value) => new(Before.Push(Focus), value, After);

    /// <summary>Removes the focus; the next item takes its place, or the previous one at the end.</summary>
    public Zipper<T>? Remove() =>
        CanMoveRight ? new(Before, After.Peek(), After.Pop())
        : CanMoveLeft ? new(Before.Pop(), Before.Peek(), After)
        : null;

    public IReadOnlyList<T> ToList() => [.. Before.Reverse(), Focus, .. After];
}
