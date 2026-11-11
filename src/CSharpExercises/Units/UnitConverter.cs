namespace CSharpExercises.Units;

/// <summary>
/// Converts compound units like "km/h", "kg*m/s^2" or "cm^3" by reducing each to a factor in SI base units plus a
/// dimension. Conversion is then value × fromFactor ÷ toFactor, allowed only when the dimensions match. SI prefixes
/// apply to a unit before its power, so cm^3 is (0.01 m)³ = 1e-6 m³. Exact names win over prefixes, so "min" is
/// minutes rather than milli-inches and "mm" is millimetres.
/// </summary>
public static class UnitConverter
{
    private readonly record struct Unit(double Factor, Dimension Dimension, bool Prefixable = false);

    private static readonly Dimension Force = Dimension.M * Dimension.L / Dimension.T.Pow(2);

    private static readonly Dictionary<string, Unit> Units = new()
    {
        ["m"] = new(1, Dimension.L, true),
        ["g"] = new(0.001, Dimension.M, true),
        ["s"] = new(1, Dimension.T, true),
        ["min"] = new(60, Dimension.T),
        ["h"] = new(3600, Dimension.T),
        ["in"] = new(0.0254, Dimension.L),
        ["ft"] = new(0.3048, Dimension.L),
        ["mi"] = new(1609.344, Dimension.L),
        ["lb"] = new(0.45359237, Dimension.M),
        ["L"] = new(0.001, Dimension.L.Pow(3), true),
        ["N"] = new(1, Force, true),
        ["J"] = new(1, Force * Dimension.L, true),
        ["W"] = new(1, Force * Dimension.L / Dimension.T, true),
    };

    private static readonly Dictionary<char, double> Prefixes = new()
    {
        ['G'] = 1e9, ['M'] = 1e6, ['k'] = 1e3, ['c'] = 1e-2, ['m'] = 1e-3, ['u'] = 1e-6, ['µ'] = 1e-6,
    };

    public static double Convert(double value, string from, string to)
    {
        var (fromFactor, fromDimension) = Parse(from);
        var (toFactor, toDimension) = Parse(to);
        if (fromDimension != toDimension)
            throw new InvalidOperationException($"Cannot convert {from} ({fromDimension}) to {to} ({toDimension}).");
        return value * fromFactor / toFactor;
    }

    /// <summary>
    /// Parses terms joined by '*' or '/', each optionally raised to an integer power. A '/' divides by the next term
    /// only, so "m/s/s" is m/s².
    /// </summary>
    public static (double Factor, Dimension Dimension) Parse(string expression)
    {
        var factor = 1.0;
        var dimension = Dimension.None;
        var sign = 1;
        var start = 0;
        for (var i = 0; i <= expression.Length; i++)
        {
            if (i < expression.Length && expression[i] is not ('*' or '/')) continue;
            var (termFactor, termDimension) = ParseTerm(expression[start..i].Trim());
            factor *= Math.Pow(termFactor, sign);
            dimension *= termDimension.Pow(sign);
            if (i < expression.Length) sign = expression[i] == '/' ? -1 : 1;
            start = i + 1;
        }
        return (factor, dimension);
    }

    private static (double, Dimension) ParseTerm(string term)
    {
        var power = 1;
        var caret = term.IndexOf('^');
        if (caret >= 0)
        {
            if (!int.TryParse(term[(caret + 1)..], out power)) throw new FormatException($"Bad power in '{term}'.");
            term = term[..caret];
        }
        var (factor, dimension) = Lookup(term);
        return (Math.Pow(factor, power), dimension.Pow(power));
    }

    private static (double, Dimension) Lookup(string name)
    {
        if (Units.TryGetValue(name, out var unit)) return (unit.Factor, unit.Dimension);
        if (name.Length > 1 && Prefixes.TryGetValue(name[0], out var prefix)
            && Units.TryGetValue(name[1..], out var baseUnit) && baseUnit.Prefixable)
            return (prefix * baseUnit.Factor, baseUnit.Dimension);
        throw new FormatException($"Unknown unit '{name}'.");
    }
}
