namespace CSharpExercises.Codecs;

/// <summary>
/// Roman numerals. Encoding is greedy over a table that includes the subtractive pairs (CM, XC, IV…). Parsing adds
/// each symbol, or subtracts it when a larger one follows, then re-encodes the result to reject non-canonical forms
/// such as "IIII" or "VX".
/// </summary>
public static class RomanNumerals
{
    private static readonly (int Value, string Symbol)[] Table =
    [
        (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"), (100, "C"), (90, "XC"),
        (50, "L"), (40, "XL"), (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I"),
    ];

    public static string ToRoman(int number)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(number, 3999);

        var sb = new System.Text.StringBuilder();
        foreach (var (value, symbol) in Table)
        {
            while (number >= value)
            {
                sb.Append(symbol);
                number -= value;
            }
        }
        return sb.ToString();
    }

    public static bool TryParse(string roman, out int number)
    {
        number = 0;
        if (string.IsNullOrEmpty(roman)) return false;

        var total = 0;
        for (var i = 0; i < roman.Length; i++)
        {
            var value = ValueOf(roman[i]);
            if (value == 0) return false;
            var next = i + 1 < roman.Length ? ValueOf(roman[i + 1]) : 0;
            total += value < next ? -value : value;
        }

        if (total is < 1 or > 3999 || ToRoman(total) != roman) return false;
        number = total;
        return true;
    }

    private static int ValueOf(char c) => c switch
    {
        'I' => 1, 'V' => 5, 'X' => 10, 'L' => 50, 'C' => 100, 'D' => 500, 'M' => 1000, _ => 0,
    };
}
