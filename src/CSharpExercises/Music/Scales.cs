namespace CSharpExercises.Music;

public enum Mode { Ionian, Dorian, Phrygian, Lydian, Mixolydian, Aeolian, Locrian }

/// <summary>
/// The seven modes are rotations of the major scale's step pattern W W H W W W H (Aeolian is the natural minor).
/// Spelling a scale correctly means using each letter exactly once: F major has B flat, not A sharp, and C sharp
/// major has E sharp, not F. So we walk the letters and pick whatever accidental lands each one on the right pitch.
/// </summary>
public static class Scales
{
    private static readonly int[] MajorSteps = [2, 2, 1, 2, 2, 2, 1];

    /// <summary>Semitone steps of the mode: the major pattern rotated to start on the mode's degree.</summary>
    public static IReadOnlyList<int> Steps(Mode mode) =>
        Enumerable.Range(0, 7).Select(i => MajorSteps[(i + (int)mode) % 7]).ToArray();

    /// <summary>The scale's notes as MIDI numbers, starting at <paramref name="rootMidi"/>.</summary>
    public static IReadOnlyList<int> Notes(int rootMidi, Mode mode)
    {
        var notes = new List<int> { rootMidi };
        foreach (var step in Steps(mode).Take(6)) notes.Add(notes[^1] + step);
        return notes;
    }

    /// <summary>The scale's note names with one letter per degree, e.g. F Ionian → F G A Bb C D E.</summary>
    public static IReadOnlyList<string> Spell(string root, Mode mode)
    {
        var (rootLetter, _, _) = Pitch.ParseName(root);
        var target = Pitch.PitchClass(root);
        var steps = Steps(mode);
        var names = new List<string>();
        for (var degree = 0; degree < 7; degree++)
        {
            var letter = (rootLetter + degree) % 7;
            var accidental = ((target - Pitch.LetterOffsets[letter]) % 12 + 12) % 12;
            if (accidental > 6) accidental -= 12;
            names.Add(Pitch.Letters[letter] + new string(accidental > 0 ? '#' : 'b', Math.Abs(accidental)));
            target = (target + steps[degree]) % 12;
        }
        return names;
    }
}
