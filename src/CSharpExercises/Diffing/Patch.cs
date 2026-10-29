namespace CSharpExercises.Diffing;

/// <param name="Lines">The patched file.</param>
/// <param name="Offsets">Per hunk, how many lines away from its header position it applied (0 when the file was unchanged).</param>
public sealed record PatchResult(IReadOnlyList<string> Lines, IReadOnlyList<int> Offsets);

public sealed class PatchConflictException(int hunkNumber, string message) : Exception(message)
{
    public int HunkNumber { get; } = hunkNumber;
}

/// <summary>
/// Applies unified diff hunks like the patch tool: a hunk's context and deleted lines must be found in the file, but
/// not necessarily at the line its header says. If lines were added or removed above it since the diff was made, the
/// nearest matching position wins and the offset is reported, as in "Hunk #2 succeeded at 14 (offset 2 lines)".
/// </summary>
public static class Patch
{
    public static PatchResult Apply(IReadOnlyList<string> original, IReadOnlyList<Hunk> hunks)
    {
        var result = new List<string>(original.Count);
        var offsets = new List<int>(hunks.Count);
        var cursor = 0;  // lines of the original consumed so far
        var drift = 0;   // the previous hunk's offset: later hunks have most likely moved by the same amount

        for (var h = 0; h < hunks.Count; h++)
        {
            var hunk = hunks[h];
            var expected = hunk.OldCount == 0 ? hunk.OldStart : hunk.OldStart - 1;
            var before = hunk.Lines.Where(l => l.Kind != EditKind.Insert).Select(l => l.Text).ToArray();

            var at = FindNearest(original, before, expected + drift, minimum: cursor);
            if (at < 0)
            {
                throw new PatchConflictException(h + 1,
                    $"Hunk #{h + 1} does not apply: its lines around line {hunk.OldStart} were not found in the file.");
            }

            drift = at - expected;
            offsets.Add(drift);

            for (var i = cursor; i < at; i++) result.Add(original[i]);
            result.AddRange(hunk.Lines.Where(l => l.Kind != EditKind.Delete).Select(l => l.Text));
            cursor = at + before.Length;
        }

        for (var i = cursor; i < original.Count; i++) result.Add(original[i]);
        return new PatchResult(result, offsets);
    }

    /// <summary>
    /// The position closest to <paramref name="expected"/> where <paramref name="lines"/> appear, searching outwards in
    /// both directions, or -1. Positions before <paramref name="minimum"/> belong to earlier hunks and are skipped.
    /// </summary>
    private static int FindNearest(IReadOnlyList<string> file, string[] lines, int expected, int minimum)
    {
        var last = file.Count - lines.Length;
        for (var distance = 0; expected - distance >= minimum || expected + distance <= last; distance++)
        {
            foreach (var candidate in (int[])[expected - distance, expected + distance])
            {
                if (candidate >= minimum && candidate <= last && MatchesAt(file, lines, candidate)) return candidate;
            }
        }
        return -1;
    }

    private static bool MatchesAt(IReadOnlyList<string> file, string[] lines, int position)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            if (file[position + i] != lines[i]) return false;
        }
        return true;
    }
}
