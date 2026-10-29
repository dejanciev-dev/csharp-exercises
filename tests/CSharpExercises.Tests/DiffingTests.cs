using CSharpExercises.Diffing;

namespace CSharpExercises.Tests;

public class DiffingTests
{
    private static string[] Lines(string text) => text.Length == 0 ? [] : text.Split('\n');

    private static string Render(IEnumerable<Edit<char>> edits) =>
        string.Concat(edits.Select(e => e.Kind switch { EditKind.Delete => "-", EditKind.Insert => "+", _ => " " } + e.Value));

    [Fact]
    public void Diff_FindsTheShortestEditScript()
    {
        // The example from Myers' paper: 5 edits is the minimum.
        var edits = MyersDiff.Diff("ABCABBA".ToList(), "CBABAC".ToList());

        Assert.Equal(5, edits.Count(e => e.Kind != EditKind.Equal));
        Assert.Equal("ABCABBA", string.Concat(edits.Where(e => e.Kind != EditKind.Insert).Select(e => e.Value)));
        Assert.Equal("CBABAC", string.Concat(edits.Where(e => e.Kind != EditKind.Delete).Select(e => e.Value)));
    }

    [Fact]
    public void Diff_RecordsPositionsInBothSequences()
    {
        var edits = MyersDiff.Diff("abc".ToList(), "aXc".ToList());

        Assert.Equal(" a-b+X c", Render(edits));
        Assert.Equal([(0, 0), (1, 1), (2, 1), (2, 2)], edits.Select(e => (e.OldIndex, e.NewIndex)));
    }

    [Fact]
    public void Diff_HandlesEmptyAndIdenticalSequences()
    {
        Assert.Empty(MyersDiff.Diff<char>([], []));
        Assert.Equal("+a+b", Render(MyersDiff.Diff([], "ab".ToList())));
        Assert.Equal("-a-b", Render(MyersDiff.Diff("ab".ToList(), [])));
        Assert.Equal(" a b", Render(MyersDiff.Diff("ab".ToList(), "ab".ToList())));
    }

    [Fact]
    public void Diff_AcceptsAComparer()
    {
        var edits = MyersDiff.Diff(["Hello", "World"], ["hello", "world"], StringComparer.OrdinalIgnoreCase);

        Assert.All(edits, e => Assert.Equal(EditKind.Equal, e.Kind));
    }

    [Fact]
    public void UnifiedDiff_FormatsLikeDiffU()
    {
        var diff = UnifiedDiff.Create(Lines("a\nb\nc\nd"), Lines("a\nB\nc\nd\ne"), "old.txt", "new.txt");

        Assert.Equal("""
            --- old.txt
            +++ new.txt
            @@ -1,4 +1,5 @@
             a
            -b
            +B
             c
             d
            +e

            """.ReplaceLineEndings("\n"), diff);
    }

    [Fact]
    public void UnifiedDiff_IsEmptyWhenNothingChanged()
    {
        Assert.Equal("", UnifiedDiff.Create(Lines("a\nb"), Lines("a\nb")));
    }

    [Fact]
    public void UnifiedDiff_UsesLineZeroForAnEmptyRange()
    {
        var created = UnifiedDiff.Hunks([], Lines("x\ny")).Single();
        var deleted = UnifiedDiff.Hunks(Lines("x"), []).Single();

        Assert.Equal((0, 0, 1, 2), (created.OldStart, created.OldCount, created.NewStart, created.NewCount));
        Assert.Equal((1, 1, 0, 0), (deleted.OldStart, deleted.OldCount, deleted.NewStart, deleted.NewCount));
        Assert.Contains("@@ -0,0 +1,2 @@", UnifiedDiff.Create([], Lines("x\ny")));
        Assert.Contains("@@ -1 +0,0 @@", UnifiedDiff.Create(Lines("x"), []));
    }

    [Fact]
    public void UnifiedDiff_SplitsDistantChangesIntoHunks()
    {
        var oldLines = Enumerable.Range(1, 20).Select(i => $"line {i}").ToArray();
        var newLines = oldLines.ToArray();
        newLines[1] = "changed 2";
        newLines[17] = "changed 18";

        var hunks = UnifiedDiff.Hunks(oldLines, newLines, context: 2);

        Assert.Equal(2, hunks.Count);
        Assert.Equal((1, 4, 1, 4), (hunks[0].OldStart, hunks[0].OldCount, hunks[0].NewStart, hunks[0].NewCount));
        Assert.Equal((16, 5, 16, 5), (hunks[1].OldStart, hunks[1].OldCount, hunks[1].NewStart, hunks[1].NewCount));
    }

    [Fact]
    public void UnifiedDiff_MergesChangesWhoseContextWouldOverlap()
    {
        var oldLines = Enumerable.Range(1, 20).Select(i => $"line {i}").ToArray();
        var newLines = oldLines.ToArray();
        newLines[5] = "changed 6";
        newLines[10] = "changed 11"; // 4 unchanged lines between, with 2 lines of context on each side

        var hunk = Assert.Single(UnifiedDiff.Hunks(oldLines, newLines, context: 2));
        Assert.Equal((4, 10), (hunk.OldStart, hunk.OldCount));
    }

    [Fact]
    public void Parse_ReadsBackWhatCreateWrote()
    {
        var oldLines = Lines("one\ntwo\n\nthree\nfour");
        var newLines = Lines("one\n2\n\nthree\nfour\nfive");

        var hunks = UnifiedDiff.Parse(UnifiedDiff.Create(oldLines, newLines));

        Assert.Equal(UnifiedDiff.Hunks(oldLines, newLines).Single().Lines, hunks.Single().Lines);
    }

    [Theory]
    [InlineData("@@ -1,2 +1,2 @@\n a\n")]          // ends early
    [InlineData("@@ -1 +1 @@\n a\n?b\n")]            // garbage after the hunk
    [InlineData("@@ -1,2 +1,2 @@\n a\n*b\n")]         // unknown line prefix
    public void Parse_RejectsMalformedDiffs(string diff)
    {
        Assert.Throws<FormatException>(() => UnifiedDiff.Parse(diff));
    }

    [Fact]
    public void Patch_AppliesADiff()
    {
        var oldLines = Lines("a\nb\nc\nd");
        var newLines = Lines("a\nB\nc\nd\ne");

        var result = Patch.Apply(oldLines, UnifiedDiff.Parse(UnifiedDiff.Create(oldLines, newLines)));

        Assert.Equal(newLines, result.Lines);
        Assert.Equal([0], result.Offsets);
    }

    [Fact]
    public void Patch_FindsHunksThatMovedAndReportsTheOffset()
    {
        var oldLines = Enumerable.Range(1, 30).Select(i => $"line {i}").ToArray();
        var newLines = oldLines.Select(l => l is "line 10" or "line 25" ? l.ToUpperInvariant() : l).ToArray();
        var hunks = UnifiedDiff.Hunks(oldLines, newLines);

        // Meanwhile, someone added two lines at the top of the file.
        string[] edited = ["header", "", .. oldLines];
        var result = Patch.Apply(edited, hunks);

        Assert.Equal(["header", "", .. newLines], result.Lines);
        Assert.Equal([2, 2], result.Offsets);
    }

    [Fact]
    public void Patch_RefusesWhenTheContextIsGone()
    {
        var oldLines = Lines("a\nb\nc");
        var hunks = UnifiedDiff.Hunks(oldLines, Lines("a\nB\nc"));

        var ex = Assert.Throws<PatchConflictException>(() => Patch.Apply(Lines("a\nX\nc"), hunks));
        Assert.Equal(1, ex.HunkNumber);
    }

    [Fact]
    public void Patch_RoundTripsRandomEdits()
    {
        var random = new Random(42);
        for (var round = 0; round < 200; round++)
        {
            var oldLines = Enumerable.Range(0, random.Next(0, 30)).Select(_ => $"{random.Next(5)}").ToList();
            var newLines = oldLines.ToList();
            for (var change = random.Next(0, 6); change > 0; change--)
            {
                var at = random.Next(newLines.Count + 1);
                if (random.Next(2) == 0 || newLines.Count == 0) newLines.Insert(at, $"{random.Next(5)}");
                else newLines.RemoveAt(Math.Min(at, newLines.Count - 1));
            }

            var diff = UnifiedDiff.Create(oldLines, newLines, context: random.Next(0, 4));
            var patched = Patch.Apply(oldLines, UnifiedDiff.Parse(diff));

            Assert.Equal(newLines, patched.Lines);
        }
    }
}
