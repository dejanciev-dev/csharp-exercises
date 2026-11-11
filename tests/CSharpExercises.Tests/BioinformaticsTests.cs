using CSharpExercises.Bioinformatics;

namespace CSharpExercises.Tests;

public class BioinformaticsTests
{
    [Fact]
    public void Dna_ComplementsAndCounts()
    {
        Assert.Equal("ACCGGGTTTT", Dna.ReverseComplement("AAAACCCGGT"));
        Assert.Equal("AAAACCCGGT", Dna.ReverseComplement(Dna.ReverseComplement("AAAACCCGGT")));
        Assert.Equal(0.5, Dna.GcContent("AGCTGCCAAT"), 9);
        Assert.Equal(0.75, Dna.GcContent("GGCA"), 9);
        Assert.Equal("GAUGGAACUUGACUACGUAAAUU", Dna.Transcribe("GATGGAACTTGACTACGTAAATT"));
        Assert.Throws<FormatException>(() => Dna.ReverseComplement("ACGX"));
    }

    [Fact]
    public void Dna_TranslatesCodons()
    {
        Assert.Equal("MAMAPRTEINSTRING*", Dna.Translate("AUGGCCAUGGCGCCCAGAACUGAGAUCAAUAGUACCCGUAUUAACGGGUGA"));
        Assert.Equal("MAMAPRTEINSTRING", Dna.Translate("AUGGCCAUGGCGCCCAGAACUGAGAUCAAUAGUACCCGUAUUAACGGGUGA", stopAtStop: true));
        Assert.Equal("MW", Dna.Translate("ATGTGGTT")); // trailing partial codon ignored
    }

    [Fact]
    public void Dna_FindsOpenReadingFramesOnBothStrands()
    {
        var proteins = Dna.OpenReadingFrames(
            "AGCCATGTAGCTAACTCAGGTTACATGGGGATGACCCCGCGACTTGGATTAGAGTCTCTTTTGGAATAAGCCTGAATGATCCGAGTAGCATCTCAG");

        Assert.Equal(new HashSet<string> { "MLLGSFRLIPKETLIQVAGSSPCNLS", "M", "MGMTPRLGLESLLE", "MTPRLGLESLLE" }, proteins);
    }

    [Fact]
    public void Global_AlignsEverything()
    {
        var alignment = SequenceAlignment.Global("GATTACA", "GCATGCU");

        Assert.Equal(0, alignment.Score);
        Assert.Equal(alignment.Top.Length, alignment.Bottom.Length);
        Assert.Equal("GATTACA", alignment.Top.Replace("-", ""));
        Assert.Equal("GCATGCU", alignment.Bottom.Replace("-", ""));
        Assert.Equal(alignment.Score, SequenceAlignment.Score(alignment.Top, alignment.Bottom, 1, -1, -1));
    }

    [Fact]
    public void Global_IdenticalSequencesNeedNoGaps()
    {
        var alignment = SequenceAlignment.Global("ACGTACGT", "ACGTACGT");

        Assert.Equal(new Alignment("ACGTACGT", "ACGTACGT", 8), alignment);
        Assert.Equal(new Alignment("ACGT", "AC-T", 2), SequenceAlignment.Global("ACGT", "ACT"));
    }

    [Fact]
    public void Local_FindsTheBestMatchingStretch()
    {
        var alignment = SequenceAlignment.Local("TGTTACGG", "GGTTGACTA");

        Assert.Equal(new Alignment("GTT-AC", "GTTGAC", 13), alignment);
        Assert.Equal(new Alignment("", "", 0), SequenceAlignment.Local("AAAA", "CCCC"));
    }
}
