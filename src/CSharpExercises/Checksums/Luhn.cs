namespace CSharpExercises.Checksums;

/// <summary>
/// The Luhn (mod 10) check digit on every payment card number: from the right, double every second digit, subtract 9
/// from results over 9, and the total must be a multiple of 10. It catches every single-digit typo and most swaps of
/// neighbouring digits (except 09 ↔ 90), and is no protection at all against deliberate forgery.
/// </summary>
public static class Luhn
{
    public static bool IsValid(string number)
    {
        var digits = number.Where(c => c is not (' ' or '-')).ToArray();
        if (digits.Length < 2 || digits.Any(c => !char.IsAsciiDigit(c))) return false;
        return Sum(digits, doubleRightmost: false) % 10 == 0;
    }

    /// <summary>The digit to append so that the result is valid.</summary>
    public static int CheckDigit(string payload)
    {
        if (payload.Length == 0 || payload.Any(c => !char.IsAsciiDigit(c))) throw new FormatException("Digits only.");
        return (10 - Sum(payload.ToCharArray(), doubleRightmost: true) % 10) % 10;
    }

    /// <summary>Card brand from the leading digits (IIN ranges), checked before sending anything to a payment gateway.</summary>
    public static string? CardBrand(string number)
    {
        var digits = new string(number.Where(char.IsAsciiDigit).ToArray());
        if (!IsValid(digits)) return null;
        return digits switch
        {
            ['4', ..] when digits.Length is 13 or 16 or 19 => "Visa",
            ['5', >= '1' and <= '5', ..] when digits.Length == 16 => "Mastercard",
            ['2', ..] when digits.Length == 16 && int.Parse(digits[..4]) is >= 2221 and <= 2720 => "Mastercard",
            ['3', '4' or '7', ..] when digits.Length == 15 => "American Express",
            _ => "Unknown",
        };
    }

    private static int Sum(char[] digits, bool doubleRightmost)
    {
        var sum = 0;
        var doubleIt = doubleRightmost;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var d = digits[i] - '0';
            if (doubleIt && (d *= 2) > 9) d -= 9;
            sum += d;
            doubleIt = !doubleIt;
        }
        return sum;
    }
}
