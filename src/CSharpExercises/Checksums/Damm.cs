namespace CSharpExercises.Checksums;

/// <summary>
/// The Damm algorithm: a check digit from a 10×10 table (a totally anti-symmetric quasigroup) that, unlike Luhn,
/// detects every single-digit error and every adjacent swap, with no letters needed and no special cases. Each digit
/// moves through the table; a valid number ends in state 0.
/// </summary>
public static class Damm
{
    private static readonly byte[,] Table =
    {
        { 0, 3, 1, 7, 5, 9, 8, 6, 4, 2 },
        { 7, 0, 9, 2, 1, 5, 4, 8, 6, 3 },
        { 4, 2, 0, 6, 8, 7, 1, 3, 5, 9 },
        { 1, 7, 5, 0, 9, 8, 3, 4, 2, 6 },
        { 6, 1, 2, 3, 0, 4, 5, 9, 7, 8 },
        { 3, 6, 7, 4, 2, 0, 9, 5, 8, 1 },
        { 5, 8, 6, 9, 7, 2, 0, 1, 3, 4 },
        { 8, 9, 4, 5, 3, 6, 2, 0, 1, 7 },
        { 9, 4, 3, 8, 6, 1, 7, 2, 0, 5 },
        { 2, 5, 8, 1, 4, 3, 6, 7, 9, 0 },
    };

    public static int CheckDigit(string digits)
    {
        var state = 0;
        foreach (var c in digits)
        {
            if (!char.IsAsciiDigit(c)) throw new FormatException($"'{c}' is not a digit.");
            state = Table[state, c - '0'];
        }
        return state;
    }

    public static bool IsValid(string digitsWithCheck) =>
        digitsWithCheck.Length > 0 && digitsWithCheck.All(char.IsAsciiDigit) && CheckDigit(digitsWithCheck) == 0;

    public static string Append(string digits) => digits + CheckDigit(digits);
}
