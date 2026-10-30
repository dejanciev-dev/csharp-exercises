namespace CSharpExercises.TextAlgorithms;

/// <summary>
/// Soundex encodes names by how they sound in English, so "Robert" and "Rupert" both become R163: keep the first
/// letter, map consonants to digits by mouth position, drop vowels, merge repeats, pad to four characters.
/// Census records and genealogy searches use it to match misspelled surnames.
/// </summary>
public static class Soundex
{
    public static string Encode(string name)
    {
        var letters = name.Where(char.IsAsciiLetter).Select(char.ToUpperInvariant).ToArray();
        if (letters.Length == 0) return "";

        var code = new System.Text.StringBuilder().Append(letters[0]);
        var previous = Digit(letters[0]);

        foreach (var letter in letters.Skip(1))
        {
            var digit = Digit(letter);
            if (digit != '0' && digit != previous) code.Append(digit);
            if (code.Length == 4) break;

            // H and W don't separate equal codes ("Ashcraft" is A261), but vowels do.
            if (letter is not ('H' or 'W')) previous = digit;
        }

        return code.ToString().PadRight(4, '0');
    }

    public static bool SoundAlike(string a, string b) => Encode(a) == Encode(b);

    private static char Digit(char letter) => letter switch
    {
        'B' or 'F' or 'P' or 'V' => '1',
        'C' or 'G' or 'J' or 'K' or 'Q' or 'S' or 'X' or 'Z' => '2',
        'D' or 'T' => '3',
        'L' => '4',
        'M' or 'N' => '5',
        'R' => '6',
        _ => '0',
    };
}
