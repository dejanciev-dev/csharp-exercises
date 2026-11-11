namespace CSharpExercises.Music;

/// <summary>
/// Naming a chord from the notes being played. Reduce the notes to pitch classes, then try each one as the root and
/// see whether the intervals above it match a known shape. The order of tries matters: C E G# is an augmented chord
/// on any of its three notes, and C D G is both Csus2 and Gsus4, so the bass note gets the first try. When the root
/// isn't in the bass the chord is an inversion and gets a slash: E G C is C/E.
/// </summary>
public static class Chords
{
    private static readonly (string Suffix, int[] Intervals)[] Shapes =
    [
        ("", [0, 4, 7]),
        ("m", [0, 3, 7]),
        ("dim", [0, 3, 6]),
        ("aug", [0, 4, 8]),
        ("sus2", [0, 2, 7]),
        ("sus4", [0, 5, 7]),
        ("7", [0, 4, 7, 10]),
        ("maj7", [0, 4, 7, 11]),
        ("m7", [0, 3, 7, 10]),
        ("m7b5", [0, 3, 6, 10]),
        ("dim7", [0, 3, 6, 9]),
    ];

    /// <summary>The chord name for the given MIDI notes, or null if the notes don't form a known shape.</summary>
    public static string? Name(IReadOnlyList<int> midiNotes)
    {
        if (midiNotes.Count == 0) return null;
        var bass = ((midiNotes.Min() % 12) + 12) % 12;
        var classes = midiNotes.Select(n => ((n % 12) + 12) % 12).ToHashSet();
        var candidates = classes.OrderBy(pc => pc == bass ? 0 : 1).ThenBy(pc => pc);

        foreach (var root in candidates)
        {
            var intervals = classes.Select(pc => (pc - root + 12) % 12).ToHashSet();
            foreach (var (suffix, shape) in Shapes)
            {
                if (!intervals.SetEquals(shape)) continue;
                var name = Pitch.NameOf(root) + suffix;
                return root == bass ? name : $"{name}/{Pitch.NameOf(bass)}";
            }
        }
        return null;
    }

    /// <summary>Convenience overload taking notes like "E3", "G3", "C4".</summary>
    public static string? Name(params string[] notes) => Name(notes.Select(Pitch.ToMidi).ToArray());

    /// <summary>The MIDI notes of a chord in root position, built from a root note and a suffix such as "m7".</summary>
    public static IReadOnlyList<int> Build(int rootMidi, string suffix)
    {
        foreach (var (s, shape) in Shapes)
            if (s == suffix) return shape.Select(i => rootMidi + i).ToArray();
        throw new ArgumentException($"Unknown chord suffix '{suffix}'.", nameof(suffix));
    }
}
