using CSharpExercises.Music;

namespace CSharpExercises.Tests;

public class MusicTests
{
    [Theory]
    [InlineData("C4", 60)]
    [InlineData("A4", 69)]
    [InlineData("C#4", 61)]
    [InlineData("Db4", 61)]
    [InlineData("B#3", 60)]
    [InlineData("Cb4", 59)]
    [InlineData("C-1", 0)]
    public void Pitch_ParsesScientificNotation(string note, int midi)
    {
        Assert.Equal(midi, Pitch.ToMidi(note));
    }

    [Fact]
    public void Pitch_RoundTripsAndComputesFrequencies()
    {
        Assert.Equal("F#2", Pitch.FromMidi(Pitch.ToMidi("Gb2")));
        Assert.Equal(440, Pitch.Frequency(69), 9);
        Assert.Equal(880, Pitch.Frequency(81), 9);
        Assert.Equal(261.6256, Pitch.Frequency(60), 4);
        Assert.Throws<FormatException>(() => Pitch.ToMidi("H4"));
        Assert.Throws<FormatException>(() => Pitch.ToMidi("C"));
    }

    [Fact]
    public void Scales_ModesAreRotationsOfTheMajorScale()
    {
        Assert.Equal([2, 2, 1, 2, 2, 2, 1], Scales.Steps(Mode.Ionian));
        Assert.Equal([2, 1, 2, 2, 1, 2, 2], Scales.Steps(Mode.Aeolian));
        Assert.Equal([60, 62, 64, 65, 67, 69, 71], Scales.Notes(60, Mode.Ionian));
        Assert.Equal(Scales.Notes(57, Mode.Aeolian).Select(n => n % 12).Order(), Scales.Notes(60, Mode.Ionian).Select(n => n % 12).Order());
    }

    [Fact]
    public void Scales_SpellUsingEachLetterOnce()
    {
        Assert.Equal(["F", "G", "A", "Bb", "C", "D", "E"], Scales.Spell("F", Mode.Ionian));
        Assert.Equal(["C#", "D#", "E#", "F#", "G#", "A#", "B#"], Scales.Spell("C#", Mode.Ionian));
        Assert.Equal(["D", "E", "F", "G", "A", "B", "C"], Scales.Spell("D", Mode.Dorian));
        Assert.Equal(["Eb", "F", "Gb", "Ab", "Bb", "Cb", "Db"], Scales.Spell("Eb", Mode.Aeolian));
    }

    [Theory]
    [InlineData("C", "C4", "E4", "G4")]
    [InlineData("Am", "A3", "C4", "E4")]
    [InlineData("C/E", "E3", "G3", "C4")]
    [InlineData("G7", "G2", "B2", "D3", "F3")]
    [InlineData("Dm7", "D3", "F3", "A3", "C4")]
    [InlineData("Bdim", "B3", "D4", "F4")]
    [InlineData("Caug", "C4", "E4", "G#4")]
    [InlineData("Csus2", "C4", "D4", "G4")]
    [InlineData("C", "C3", "G3", "C4", "E4", "G4")]
    public void Chords_NamesTheNotes(string expected, params string[] notes)
    {
        Assert.Equal(expected, Chords.Name(notes));
    }

    [Fact]
    public void Chords_BuildAndNameAgree()
    {
        foreach (var suffix in new[] { "", "m", "7", "maj7", "m7", "m7b5" })
            Assert.Equal("D" + suffix, Chords.Name(Chords.Build(62, suffix)));

        Assert.Null(Chords.Name("C4", "C#4", "D4"));
    }
}
