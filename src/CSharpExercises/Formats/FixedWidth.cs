using System.Globalization;
using System.Text;

namespace CSharpExercises.Formats;

public sealed record FieldSpec(string Name, int Start, int Length, char Pad = ' ', bool RightAlign = false);

/// <summary>
/// Fixed-width records, still everywhere in banking, payroll and mainframe exports: each field lives at a fixed column
/// range, numbers are right-aligned and zero-padded, text is left-aligned and space-padded, and implied decimals
/// ("000012345" means 123.45) are common. Layouts are validated so fields can't overlap.
/// </summary>
public sealed class FixedWidth
{
    private readonly IReadOnlyList<FieldSpec> _fields;

    public FixedWidth(IEnumerable<FieldSpec> fields)
    {
        _fields = fields.OrderBy(f => f.Start).ToList();
        for (var i = 1; i < _fields.Count; i++)
            if (_fields[i].Start < _fields[i - 1].Start + _fields[i - 1].Length)
                throw new ArgumentException($"Fields '{_fields[i - 1].Name}' and '{_fields[i].Name}' overlap.", nameof(fields));
        RecordLength = _fields.Count == 0 ? 0 : _fields.Max(f => f.Start + f.Length);
    }

    public int RecordLength { get; }

    public IReadOnlyDictionary<string, string> Parse(string line)
    {
        if (line.Length < RecordLength) throw new FormatException($"Record is {line.Length} characters, expected {RecordLength}.");
        return _fields.ToDictionary(f => f.Name, f =>
        {
            var raw = line.Substring(f.Start, f.Length);
            if (!f.RightAlign) return raw.TrimEnd(f.Pad);
            var trimmed = raw.TrimStart(f.Pad);
            return trimmed.Length == 0 && f.Pad == '0' ? "0" : trimmed; // all zeros is the number 0, not empty
        });
    }

    public string Format(IReadOnlyDictionary<string, string> values)
    {
        var line = new StringBuilder(new string(' ', RecordLength));
        foreach (var f in _fields)
        {
            var value = values.GetValueOrDefault(f.Name, "");
            if (value.Length > f.Length) throw new ArgumentException($"'{value}' is too long for {f.Name} ({f.Length} characters).");
            var padded = f.RightAlign ? value.PadLeft(f.Length, f.Pad) : value.PadRight(f.Length, f.Pad);
            line.Remove(f.Start, f.Length).Insert(f.Start, padded);
        }
        return line.ToString();
    }

    /// <summary>"000012345" with 2 implied decimals is 123.45; a trailing or leading sign is allowed.</summary>
    public static decimal ParseImpliedDecimal(string digits, int decimals)
    {
        var negative = digits.EndsWith('-') || digits.StartsWith('-');
        var clean = digits.Trim('-', '+', ' ');
        if (!long.TryParse(clean, NumberStyles.None, CultureInfo.InvariantCulture, out var value)) throw new FormatException($"'{digits}' is not a number.");
        var result = value / (decimal)Math.Pow(10, decimals);
        return negative ? -result : result;
    }

    public static string FormatImpliedDecimal(decimal amount, int decimals, int width)
    {
        var scaled = (long)Math.Round(Math.Abs(amount) * (decimal)Math.Pow(10, decimals), MidpointRounding.AwayFromZero);
        var text = scaled.ToString(CultureInfo.InvariantCulture).PadLeft(width - (amount < 0 ? 1 : 0), '0');
        return amount < 0 ? text + "-" : text;
    }
}
