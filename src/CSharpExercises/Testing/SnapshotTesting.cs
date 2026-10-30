using System.Runtime.CompilerServices;

namespace CSharpExercises.Testing;

public sealed class SnapshotMismatchException(string message) : Exception(message);

/// <summary>
/// Snapshot (approval) testing: instead of hand-writing the expected output of something big (a report, a rendered
/// page, serialized JSON), store an approved copy next to the test. The first run writes ".received" and fails until a
/// human approves it by renaming it to ".approved"; later runs fail with a line diff when the output changes, writing the
/// new ".received" file for review. Line endings are normalised so the files work on every OS.
/// </summary>
public sealed class SnapshotTesting(string directory)
{
    public void Verify(string actual, [CallerMemberName] string name = "")
    {
        Directory.CreateDirectory(directory);
        var approvedPath = Path.Combine(directory, $"{name}.approved.txt");
        var receivedPath = Path.Combine(directory, $"{name}.received.txt");
        actual = Normalise(actual);

        if (!File.Exists(approvedPath))
        {
            File.WriteAllText(receivedPath, actual);
            throw new SnapshotMismatchException($"No approved snapshot for '{name}'. Review {receivedPath} and rename it to .approved.txt.");
        }

        var approved = Normalise(File.ReadAllText(approvedPath));
        if (approved == actual)
        {
            File.Delete(receivedPath); // clean up after a fixed failure
            return;
        }

        File.WriteAllText(receivedPath, actual);
        throw new SnapshotMismatchException($"Snapshot '{name}' changed:\n{Diff(approved, actual)}");
    }

    public void Approve(string name)
    {
        var received = Path.Combine(directory, $"{name}.received.txt");
        File.Move(received, Path.Combine(directory, $"{name}.approved.txt"), overwrite: true);
    }

    /// <summary>A minimal line diff: "-" lines only in the approved copy, "+" lines only in the new output.</summary>
    public static string Diff(string expected, string actual)
    {
        var (a, b) = (expected.Split('\n'), actual.Split('\n'));
        var lines = new List<string>();
        for (var i = 0; i < Math.Max(a.Length, b.Length); i++)
        {
            var (left, right) = (i < a.Length ? a[i] : null, i < b.Length ? b[i] : null);
            if (left == right) continue;
            if (left is not null) lines.Add($"line {i + 1}: - {left}");
            if (right is not null) lines.Add($"line {i + 1}: + {right}");
        }
        return string.Join('\n', lines);
    }

    private static string Normalise(string text) => text.Replace("\r\n", "\n").TrimEnd('\n') + "\n";
}
