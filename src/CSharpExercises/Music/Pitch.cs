namespace CSharpExercises.Music;

/// <summary>
/// Scientific pitch notation to MIDI numbers and back. Middle C is C4 = 60 and A4 = 69 = 440 Hz; every semitone
/// multiplies the frequency by the twelfth root of 2, so an octave doubles it. Spellings are not unique:
/// C#4, Db4 and even B##3 are the same key, and B#3 belongs to octave 3 even though it sounds like C4.
/// </summary>
public static class Pitch
{
    private static readonly string[] SharpNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];

    /// <summary>Semitones above C of each natural letter, indexed from C.</summary>
    internal static readonly int[] LetterOffsets = [0, 2, 4, 5, 7, 9, 11];

    internal const string Letters = "CDEFGAB";

    /// <summary>Parses a letter with any number of sharps or flats, e.g. "F", "Bb", "C##"; returns the letter index and accidental.</summary>
    internal static (int Letter, int Accidental, int Length) ParseName(string text)
    {
        if (text.Length == 0) throw new FormatException("Empty note name.");
        var letter = Letters.IndexOf(char.ToUpperInvariant(text[0]));
        if (letter < 0) throw new FormatException($"'{text[0]}' is not a note letter.");
        var i = 1;
        var accidental = 0;
        for (; i < text.Length && text[i] is '#' or 'b'; i++) accidental += text[i] == '#' ? 1 : -1;
        return (letter, accidental, i);
    }

    /// <summary>Pitch class 0–11 of a note name without an octave.</summary>
    public static int PitchClass(string name)
    {
        var (letter, accidental, length) = ParseName(name);
        if (length != name.Length) throw new FormatException($"Unexpected '{name[length..]}' in '{name}'.");
        return ((LetterOffsets[letter] + accidental) % 12 + 12) % 12;
    }

    public static int ToMidi(string note)
    {
        var (letter, accidental, length) = ParseName(note);
        if (!int.TryParse(note.AsSpan(length), out var octave)) throw new FormatException($"Missing octave in '{note}'.");
        return (octave + 1) * 12 + LetterOffsets[letter] + accidental;
    }

    public static string FromMidi(int midi)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(midi);
        return SharpNames[midi % 12] + (midi / 12 - 1);
    }

    public static string NameOf(int pitchClass) => SharpNames[(pitchClass % 12 + 12) % 12];

    public static double Frequency(int midi, double a4 = 440) => a4 * Math.Pow(2, (midi - 69) / 12.0);
}
