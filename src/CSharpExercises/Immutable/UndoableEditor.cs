using System.Collections.Immutable;

namespace CSharpExercises.Immutable;

/// <summary>
/// An immutable document. <see cref="ImmutableList{T}"/> is a balanced tree, so changing one line creates
/// O(log n) new nodes and shares the rest with the previous version: keeping hundreds of versions for undo costs
/// little more than keeping one.
/// </summary>
public sealed record TextDocument(ImmutableList<string> Lines)
{
    public static readonly TextDocument Empty = new(ImmutableList<string>.Empty);

    public static TextDocument From(string text) => new([.. text.Split('\n')]);

    public TextDocument Insert(int line, string text) => this with { Lines = Lines.Insert(line, text) };

    public TextDocument Append(string text) => this with { Lines = Lines.Add(text) };

    /// <summary>Returns the same instance when nothing changes, so the editor can tell a no-op from an edit.</summary>
    public TextDocument Replace(int line, string text) => Lines[line] == text ? this : this with { Lines = Lines.SetItem(line, text) };

    public TextDocument Delete(int line) => this with { Lines = Lines.RemoveAt(line) };

    public override string ToString() => string.Join('\n', Lines);
}

/// <summary>
/// Undo and redo by keeping snapshots: because documents are immutable, a snapshot is just a reference, and no edit
/// can ever change a version stored in the history. The alternative, recording an inverse command for every edit,
/// needs an "undo" written for each operation, and one bug in any of them corrupts the history.
/// </summary>
public sealed class Editor(int maxHistory = 100)
{
    private readonly LinkedList<TextDocument> _undo = new();
    private readonly Stack<TextDocument> _redo = new();

    public TextDocument Current { get; private set; } = TextDocument.Empty;

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public int HistoryCount => _undo.Count;

    /// <summary>Applies an edit. A new edit clears the redo history, as in every editor.</summary>
    public void Apply(Func<TextDocument, TextDocument> edit)
    {
        var next = edit(Current);
        if (ReferenceEquals(next, Current)) return; // nothing changed: do not add an undo step

        _undo.AddLast(Current);
        if (_undo.Count > maxHistory) _undo.RemoveFirst(); // forget the oldest step
        _redo.Clear();
        Current = next;
    }

    public bool Undo()
    {
        if (_undo.Last is not { } previous) return false;

        _undo.RemoveLast();
        _redo.Push(Current);
        Current = previous.Value;
        return true;
    }

    public bool Redo()
    {
        if (!_redo.TryPop(out var next)) return false;

        _undo.AddLast(Current);
        Current = next;
        return true;
    }
}
