namespace CSharpExercises.Codecs;

/// <summary>
/// Spelling numbers out in English, as on cheques: split into groups of three digits, spell each group, and attach
/// the scale word (thousand, million…). Also amounts in words with cents, and ordinals (first, twenty-second).
/// </summary>
public static class NumberToWords
{
    private static readonly string[] Ones =
    [
        "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
        "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen",
    ];

    private static readonly string[] Tens = ["", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety"];

    private static readonly string[] Scales = ["", "thousand", "million", "billion", "trillion", "quadrillion", "quintillion"];

    public static string Convert(long number)
    {
        if (number == 0) return Ones[0];
        if (number < 0) return "minus " + Convert(Math.Abs((decimal)number));
        return Convert((decimal)number);
    }

    private static string Convert(decimal number)
    {
        var groups = new List<string>();
        for (var scale = 0; number > 0; scale++, number = Math.Floor(number / 1000))
        {
            var group = (int)(number % 1000);
            if (group == 0) continue;
            groups.Insert(0, scale == 0 ? Hundreds(group) : $"{Hundreds(group)} {Scales[scale]}");
        }
        return string.Join(' ', groups);
    }

    /// <summary>"one hundred twenty-three dollars and 45/100", the way amounts are written on cheques.</summary>
    public static string Amount(decimal amount, string currency = "dollars")
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        var whole = (long)Math.Floor(amount);
        var cents = (int)Math.Round((amount - whole) * 100, MidpointRounding.AwayFromZero);
        if (cents == 100) (whole, cents) = (whole + 1, 0);
        return $"{Convert(whole)} {currency} and {cents:00}/100";
    }

    public static string Ordinal(long number)
    {
        var words = Convert(number);
        var lastSpace = Math.Max(words.LastIndexOf(' '), words.LastIndexOf('-'));
        var (head, last) = (words[..(lastSpace + 1)], words[(lastSpace + 1)..]);

        return head + last switch
        {
            "one" => "first",
            "two" => "second",
            "three" => "third",
            "five" => "fifth",
            "eight" => "eighth",
            "nine" => "ninth",
            "twelve" => "twelfth",
            _ when last.EndsWith('y') => last[..^1] + "ieth",
            _ => last + "th",
        };
    }

    private static string Hundreds(int n)
    {
        var parts = new List<string>();
        if (n >= 100)
        {
            parts.Add($"{Ones[n / 100]} hundred");
            n %= 100;
        }
        if (n >= 20) parts.Add(n % 10 == 0 ? Tens[n / 10] : $"{Tens[n / 10]}-{Ones[n % 10]}");
        else if (n > 0) parts.Add(Ones[n]);
        return string.Join(' ', parts);
    }
}
