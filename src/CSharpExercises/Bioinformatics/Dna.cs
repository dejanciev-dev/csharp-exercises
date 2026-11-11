using System.Text;

namespace CSharpExercises.Bioinformatics;

/// <summary>
/// DNA basics. The two strands run in opposite directions and pair A–T and C–G, so the other strand is the reverse
/// complement. Proteins are read three bases (one codon) at a time. The whole genetic code fits in a 64-character
/// string: order the bases T, C, A, G and a codon's index is 16·first + 4·second + third. An open reading frame runs
/// from a start codon (ATG, methionine) to the first stop, and can sit in any of three offsets on either strand.
/// </summary>
public static class Dna
{
    private const string Bases = "TCAG";
    private const string GeneticCode = "FFLLSSSSYY**CC*WLLLLPPPPHHQQRRRRIIIMTTTTNNKKSSRRVVVVAAAADDEEGGGG";

    public static string ReverseComplement(string dna)
    {
        var result = new StringBuilder(dna.Length);
        for (var i = dna.Length - 1; i >= 0; i--)
            result.Append(dna[i] switch
            {
                'A' => 'T', 'T' => 'A', 'C' => 'G', 'G' => 'C',
                var other => throw new FormatException($"'{other}' is not a DNA base."),
            });
        return result.ToString();
    }

    public static double GcContent(string dna) =>
        dna.Length == 0 ? 0 : (double)dna.Count(b => b is 'G' or 'C') / dna.Length;

    public static string Transcribe(string dna) => dna.Replace('T', 'U');

    /// <summary>Translates DNA or RNA codon by codon; stops are '*', a trailing partial codon is ignored.</summary>
    public static string Translate(string sequence, bool stopAtStop = false)
    {
        var protein = new StringBuilder(sequence.Length / 3);
        for (var i = 0; i + 3 <= sequence.Length; i += 3)
        {
            var aminoAcid = Codon(sequence.AsSpan(i, 3));
            if (aminoAcid == '*' && stopAtStop) break;
            protein.Append(aminoAcid);
        }
        return protein.ToString();
    }

    private static char Codon(ReadOnlySpan<char> codon)
    {
        var index = 0;
        foreach (var b in codon)
        {
            var value = Bases.IndexOf(b == 'U' ? 'T' : b);
            if (value < 0) throw new FormatException($"'{b}' is not a base.");
            index = index * 4 + value;
        }
        return GeneticCode[index];
    }

    /// <summary>Every distinct protein that starts at an ATG and ends at a stop codon, on both strands and all frames.</summary>
    public static IReadOnlySet<string> OpenReadingFrames(string dna)
    {
        var proteins = new HashSet<string>();
        foreach (var strand in new[] { dna, ReverseComplement(dna) })
            for (var start = 0; start + 3 <= strand.Length; start++)
            {
                if (!strand.AsSpan(start, 3).SequenceEqual("ATG")) continue;
                var protein = new StringBuilder();
                for (var i = start; i + 3 <= strand.Length; i += 3)
                {
                    var aminoAcid = Codon(strand.AsSpan(i, 3));
                    if (aminoAcid == '*')
                    {
                        proteins.Add(protein.ToString());
                        break;
                    }
                    protein.Append(aminoAcid);
                } // no stop before the end: not an open reading frame
            }
        return proteins;
    }
}
