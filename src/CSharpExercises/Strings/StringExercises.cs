using System.Text;

namespace CSharpExercises.Strings;

/// <summary>
/// Common string-manipulation interview exercises.
/// </summary>
public static class StringExercises
{
    public static string Reverse(string input)
    {
        var chars = input.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    /// <summary>Ignores case and any non-alphanumeric characters.</summary>
    public static bool IsPalindrome(string input)
    {
        var cleaned = input.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray();
        for (int i = 0, j = cleaned.Length - 1; i < j; i++, j--)
        {
            if (cleaned[i] != cleaned[j]) return false;
        }

        return true;
    }

    public static bool AreAnagrams(string first, string second)
    {
        static string Normalize(string s) =>
            new(s.Where(char.IsLetter).Select(char.ToLowerInvariant).Order().ToArray());

        return Normalize(first) == Normalize(second);
    }

    public static int CountVowels(string input) =>
        input.Count(c => "aeiouAEIOU".Contains(c));

    /// <summary>Run-length encoding: "aaabcc" becomes "a3b1c2".</summary>
    public static string Compress(string input)
    {
        if (input.Length == 0) return input;

        var sb = new StringBuilder();
        var count = 1;
        for (var i = 1; i <= input.Length; i++)
        {
            if (i < input.Length && input[i] == input[i - 1])
            {
                count++;
                continue;
            }

            sb.Append(input[i - 1]).Append(count);
            count = 1;
        }

        return sb.ToString();
    }

    private static readonly char[] WordSeparators = [' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':'];

    public static Dictionary<string, int> WordFrequency(string text) =>
        text.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries)
            .GroupBy(w => w.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.Count());
}
