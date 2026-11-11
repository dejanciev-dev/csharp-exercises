namespace CSharpExercises.Chemistry;

/// <summary>
/// Parses chemical formulas into element counts with a small recursive-descent parser. An element is a capital letter
/// plus an optional lowercase one (so "Co" is cobalt but "CO" is carbon monoxide), groups in parentheses or brackets
/// multiply everything inside ("Ca(OH)2", "K4[Fe(CN)6]"), and a hydrate adds parts joined by '·' or '*', each with an
/// optional leading count ("CuSO4·5H2O").
/// </summary>
public static class ChemicalFormula
{
    private static readonly Dictionary<string, double> AtomicMasses = new()
    {
        ["H"] = 1.008, ["He"] = 4.0026, ["C"] = 12.011, ["N"] = 14.007, ["O"] = 15.999, ["F"] = 18.998,
        ["Na"] = 22.990, ["Mg"] = 24.305, ["Al"] = 26.982, ["Si"] = 28.085, ["P"] = 30.974, ["S"] = 32.06,
        ["Cl"] = 35.45, ["K"] = 39.098, ["Ca"] = 40.078, ["Mn"] = 54.938, ["Fe"] = 55.845, ["Co"] = 58.933,
        ["Cu"] = 63.546, ["Zn"] = 65.38, ["Br"] = 79.904, ["Ag"] = 107.87, ["I"] = 126.90, ["Au"] = 196.97,
    };

    public static IReadOnlyDictionary<string, int> Parse(string formula)
    {
        var total = new Dictionary<string, int>();
        foreach (var part in formula.Split('·', '*'))
        {
            var text = part.Trim();
            var position = 0;
            var multiplier = ReadCount(text, ref position, defaultCount: 1);
            var counts = ParseGroup(text, ref position);
            if (position != text.Length) throw new FormatException($"Unexpected '{text[position]}' in '{formula}'.");
            Add(total, counts, multiplier);
        }
        if (total.Count == 0) throw new FormatException("Empty formula.");
        return total;
    }

    private static Dictionary<string, int> ParseGroup(string text, ref int position)
    {
        var counts = new Dictionary<string, int>();
        while (position < text.Length)
        {
            var c = text[position];
            if (c is '(' or '[')
            {
                var close = c == '(' ? ')' : ']';
                position++;
                var inner = ParseGroup(text, ref position);
                if (position >= text.Length || text[position] != close) throw new FormatException($"Missing '{close}' in '{text}'.");
                position++;
                Add(counts, inner, ReadCount(text, ref position, 1));
            }
            else if (char.IsUpper(c))
            {
                var start = position++;
                if (position < text.Length && char.IsLower(text[position])) position++;
                var element = text[start..position];
                if (!AtomicMasses.ContainsKey(element)) throw new FormatException($"Unknown element '{element}'.");
                counts[element] = counts.GetValueOrDefault(element) + ReadCount(text, ref position, 1);
            }
            else break; // a closing bracket or junk: let the caller decide
        }
        return counts;
    }

    private static int ReadCount(string text, ref int position, int defaultCount)
    {
        var start = position;
        while (position < text.Length && char.IsAsciiDigit(text[position])) position++;
        return position == start ? defaultCount : int.Parse(text.AsSpan(start, position - start));
    }

    private static void Add(Dictionary<string, int> into, IReadOnlyDictionary<string, int> counts, int multiplier)
    {
        foreach (var (element, count) in counts) into[element] = into.GetValueOrDefault(element) + count * multiplier;
    }

    /// <summary>Grams per mole: the sum of the atomic masses.</summary>
    public static double MolarMass(string formula) =>
        Parse(formula).Sum(e => AtomicMasses[e.Key] * e.Value);

    /// <summary>Each element's share of the mass, e.g. water is about 88.8% oxygen.</summary>
    public static IReadOnlyDictionary<string, double> MassFractions(string formula)
    {
        var mass = MolarMass(formula);
        return Parse(formula).ToDictionary(e => e.Key, e => AtomicMasses[e.Key] * e.Value / mass);
    }
}
