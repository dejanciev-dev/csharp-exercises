using System.Text;
using System.Text.RegularExpressions;

namespace CSharpExercises.Diffing;

public readonly record struct DiffLine(EditKind Kind, string Text);

/// <summary>
/// A block of changes with surrounding context. The numbers are exactly what the "@@ -start,count +start,count @@"
/// header shows: 1-based starts, except that an empty range starts at the line before it (0 for the top of the file).
/// </summary>
public sealed record Hunk(int OldStart, int OldCount, int NewStart, int NewCount, IReadOnlyList<DiffLine> Lines);

/// <summary>
/// The unified diff format of diff -u and git: changes grouped into hunks with a few lines of unchanged context, which
/// makes a diff readable and lets a patch be applied to a file that has changed elsewhere in the meantime.
/// </summary>
public static partial class UnifiedDiff
{
    public static IReadOnlyList<Hunk> Hunks(IReadOnlyList<string> oldLines, IReadOnlyList<string> newLines, int context = 3)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(context);
        var edits = MyersDiff.Diff(oldLines, newLines);
        var hunks = new List<Hunk>();

        var i = 0;
        while (true)
        {
            while (i < edits.Count && edits[i].Kind == EditKind.Equal) i++;
            if (i == edits.Count) break;

            // Extend the hunk over later changes while the unchanged gap between them is small enough that their
            // context would overlap; a longer gap starts a new hunk.
            var lastChange = i;
            var j = i;
            while (j < edits.Count)
            {
                if (edits[j].Kind != EditKind.Equal)
                {
                    lastChange = j++;
                    continue;
                }
                var gapEnd = j;
                while (gapEnd < edits.Count && edits[gapEnd].Kind == EditKind.Equal) gapEnd++;
                if (gapEnd == edits.Count || gapEnd - j > 2 * context) break;
                j = gapEnd;
            }

            var start = Math.Max(0, i - context);
            var end = Math.Min(edits.Count, lastChange + 1 + context);
            hunks.Add(ToHunk(edits, start, end));
            i = end;
        }
        return hunks;
    }

    public static string Create(IReadOnlyList<string> oldLines, IReadOnlyList<string> newLines,
        string oldName = "a", string newName = "b", int context = 3)
    {
        var hunks = Hunks(oldLines, newLines, context);
        if (hunks.Count == 0) return "";

        var text = new StringBuilder();
        text.Append("--- ").Append(oldName).Append('\n');
        text.Append("+++ ").Append(newName).Append('\n');
        foreach (var hunk in hunks)
        {
            text.Append($"@@ -{Range(hunk.OldStart, hunk.OldCount)} +{Range(hunk.NewStart, hunk.NewCount)} @@\n");
            foreach (var line in hunk.Lines)
            {
                text.Append(line.Kind switch { EditKind.Delete => '-', EditKind.Insert => '+', _ => ' ' });
                text.Append(line.Text).Append('\n');
            }
        }
        return text.ToString();
    }

    /// <summary>Reads the hunks back from a unified diff, checking each hunk has exactly the lines its header promises.</summary>
    public static IReadOnlyList<Hunk> Parse(string diff)
    {
        var lines = diff.Split('\n');
        if (lines[^1] == "") lines = lines[..^1];

        var hunks = new List<Hunk>();
        var i = 0;
        while (i < lines.Length)
        {
            var header = HunkHeader().Match(lines[i]);
            if (!header.Success)
            {
                // File headers ("---", "+++", "diff --git", ...) only matter before the first hunk.
                if (hunks.Count > 0) throw new FormatException($"Line {i + 1}: expected a hunk header but found '{lines[i]}'.");
                i++;
                continue;
            }

            var (oldStart, oldCount) = (int.Parse(header.Groups[1].Value), Count(header.Groups[2]));
            var (newStart, newCount) = (int.Parse(header.Groups[3].Value), Count(header.Groups[4]));
            var headerLine = i++;

            var body = new List<DiffLine>();
            int oldSeen = 0, newSeen = 0;
            while (oldSeen < oldCount || newSeen < newCount)
            {
                if (i == lines.Length) throw new FormatException($"Hunk at line {headerLine + 1} ends early.");
                var line = lines[i++];

                // Some editors strip the single space of an empty context line.
                var kind = line.Length == 0 ? EditKind.Equal : line[0] switch
                {
                    ' ' => EditKind.Equal,
                    '-' => EditKind.Delete,
                    '+' => EditKind.Insert,
                    _ => throw new FormatException($"Line {i}: unexpected '{line}' inside a hunk."),
                };
                if (kind != EditKind.Insert) oldSeen++;
                if (kind != EditKind.Delete) newSeen++;
                body.Add(new(kind, line.Length == 0 ? "" : line[1..]));
            }
            if (oldSeen != oldCount || newSeen != newCount)
                throw new FormatException($"Hunk at line {headerLine + 1} does not match its header counts.");

            hunks.Add(new Hunk(oldStart, oldCount, newStart, newCount, body));
        }
        return hunks;
    }

    private static Hunk ToHunk(IReadOnlyList<Edit<string>> edits, int start, int end)
    {
        var lines = new List<DiffLine>(end - start);
        int oldCount = 0, newCount = 0;
        for (var i = start; i < end; i++)
        {
            lines.Add(new(edits[i].Kind, edits[i].Value));
            if (edits[i].Kind != EditKind.Insert) oldCount++;
            if (edits[i].Kind != EditKind.Delete) newCount++;
        }

        var first = edits[start];
        return new Hunk(
            oldCount == 0 ? first.OldIndex : first.OldIndex + 1, oldCount,
            newCount == 0 ? first.NewIndex : first.NewIndex + 1, newCount,
            lines);
    }

    // A count of 1 is left out of the header: "@@ -3 +3,2 @@".
    private static string Range(int start, int count) => count == 1 ? $"{start}" : $"{start},{count}";

    private static int Count(Group group) => group.Success ? int.Parse(group.Value) : 1;

    [GeneratedRegex(@"^@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@")]
    private static partial Regex HunkHeader();
}
