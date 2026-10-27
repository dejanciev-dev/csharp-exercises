using CSharpExercises.Immutable;

namespace CSharpExercises.Tests;

public class ImmutableTests
{
    [Fact]
    public void Stack_OldVersionsStayValid()
    {
        var empty = PersistentStack<int>.Empty;
        var one = empty.Push(1);
        var two = one.Push(2);
        var other = one.Push(99);

        Assert.Empty(empty);
        Assert.Equal([1], one);
        Assert.Equal([2, 1], two);
        Assert.Equal([99, 1], other);
    }

    [Fact]
    public void Stack_VersionsShareTheirTails()
    {
        var shared = PersistentStack<string>.Empty.Push("a").Push("b");

        var left = shared.Push("left");
        var right = shared.Push("right");

        // Both new stacks point at the very same object below their top: nothing was copied.
        Assert.Same(shared, left.Pop());
        Assert.Same(left.Pop(), right.Pop());
    }

    [Fact]
    public void Stack_EmptyOperationsThrow()
    {
        Assert.Throws<InvalidOperationException>(() => PersistentStack<int>.Empty.Peek());
        Assert.Throws<InvalidOperationException>(() => PersistentStack<int>.Empty.Pop());
    }

    [Fact]
    public void Queue_IsFirstInFirstOut()
    {
        var queue = PersistentQueue<int>.Empty.Enqueue(1).Enqueue(2).Enqueue(3);

        Assert.Equal(1, queue.Peek());
        Assert.Equal([2, 3], queue.Dequeue());
        Assert.Equal([1, 2, 3], queue); // the original is untouched
        Assert.Equal(3, queue.Count);
    }

    [Fact]
    public void Queue_MatchesTheMutableQueueUnderRandomOperations()
    {
        var random = new Random(11);
        var persistent = PersistentQueue<int>.Empty;
        var reference = new Queue<int>();

        for (var step = 0; step < 5_000; step++)
        {
            if (reference.Count > 0 && random.Next(3) == 0)
            {
                Assert.Equal(reference.Dequeue(), persistent.Peek());
                persistent = persistent.Dequeue();
            }
            else
            {
                reference.Enqueue(step);
                persistent = persistent.Enqueue(step);
            }

            Assert.Equal(reference.Count, persistent.Count);
        }

        Assert.Equal(reference, persistent);
    }

    [Fact]
    public void Document_EditsReturnNewVersions()
    {
        var original = TextDocument.From("one\ntwo\nthree");

        var edited = original.Replace(1, "TWO").Insert(0, "zero").Delete(3);

        Assert.Equal("zero\none\nTWO", edited.ToString());
        Assert.Equal("one\ntwo\nthree", original.ToString());
    }

    [Fact]
    public void Editor_UndoAndRedo()
    {
        var editor = new Editor();
        editor.Apply(d => d.Append("Hello"));
        editor.Apply(d => d.Append("world"));
        editor.Apply(d => d.Replace(1, "there"));

        Assert.True(editor.Undo());
        Assert.Equal("Hello\nworld", editor.Current.ToString());
        Assert.True(editor.Undo());
        Assert.Equal("Hello", editor.Current.ToString());

        Assert.True(editor.Redo());
        Assert.True(editor.Redo());
        Assert.False(editor.Redo());
        Assert.Equal("Hello\nthere", editor.Current.ToString());
    }

    [Fact]
    public void Editor_ANewEditClearsRedo()
    {
        var editor = new Editor();
        editor.Apply(d => d.Append("a"));
        editor.Apply(d => d.Append("b"));
        editor.Undo();

        editor.Apply(d => d.Append("c"));

        Assert.False(editor.CanRedo);
        Assert.Equal("a\nc", editor.Current.ToString());
    }

    [Fact]
    public void Editor_NoOpEditsAddNoUndoStep()
    {
        var editor = new Editor();
        editor.Apply(d => d.Append("same"));

        editor.Apply(d => d.Replace(0, "same"));

        Assert.Equal(1, editor.HistoryCount);
    }

    [Fact]
    public void Editor_KeepsALimitedHistory()
    {
        var editor = new Editor(maxHistory: 3);
        for (var i = 0; i < 10; i++) editor.Apply(d => d.Append($"line {i}"));

        var undone = 0;
        while (editor.Undo()) undone++;

        Assert.Equal(3, undone);
        Assert.Equal(7, editor.Current.Lines.Count);
        Assert.False(editor.CanUndo);
    }

    [Fact]
    public void Playlist_AnInterfaceCanBeCastBackAndModified()
    {
        var playlist = new Playlist();
        playlist.Add("Song A");

        ((List<string>)playlist.SongsAsInterface).Clear(); // nothing stops this

        Assert.Empty(playlist.SongsAsInterface);
    }

    [Fact]
    public void Playlist_AReadOnlyViewRefusesChangesButFollowsTheSource()
    {
        var playlist = new Playlist();
        playlist.Add("Song A");
        var view = playlist.SongsAsView;

        Assert.Throws<NotSupportedException>(() => ((IList<string>)view).Add("Song B"));

        playlist.Add("Song B");
        Assert.Equal(["Song A", "Song B"], view); // what we held has changed

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (var _ in view) playlist.Add("Song C");
        });
    }

    [Fact]
    public void Playlist_ASnapshotNeverChanges()
    {
        var playlist = new Playlist();
        playlist.Add("Song A");
        var snapshot = playlist.SongsAsSnapshot;

        playlist.Add("Song B");

        // Compare as a sequence of strings. Assert.Equal(["Song A"], snapshot) would build a second ImmutableArray, and
        // ImmutableArray.Equals compares the underlying array references, not the items: two equal-looking arrays differ.
        Assert.Equal<string>(["Song A"], snapshot);
    }

    [Fact]
    public void ImmutableArray_EqualsComparesReferencesNotContents()
    {
        System.Collections.Immutable.ImmutableArray<int> a = [1, 2, 3];
        System.Collections.Immutable.ImmutableArray<int> b = [1, 2, 3];

        Assert.False(a.Equals(b));
        Assert.True(a.SequenceEqual(b));
    }

    [Theory]
    [InlineData("MK", "North Macedonia")]
    [InlineData(" de ", "Germany")]
    [InlineData("xx", null)]
    public void CountryCodes_LooksUpCaseInsensitively(string code, string? name)
    {
        Assert.Equal(name, CountryCodes.NameOf(code));
    }
}
