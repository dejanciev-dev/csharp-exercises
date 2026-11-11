using System.Numerics;
using CSharpExercises.Numerics;

namespace CSharpExercises.Chemistry;

/// <summary>
/// Balancing a chemical equation is linear algebra: one column per species, one row per element, reactants counted
/// positive and products negative, and we want the whole-number vector that the matrix sends to zero. Row-reducing
/// with exact fractions (floating point would give 0.6666…) leaves one free column when the answer is unique; set
/// it to 1, read off the rest, and scale by the denominators' LCM. No free column means the equation is impossible;
/// several mean it's really two reactions mixed together.
/// </summary>
public static class EquationBalancer
{
    public static IReadOnlyList<long> Coefficients(string equation)
    {
        var sides = equation.Split("->");
        if (sides.Length != 2) throw new FormatException("Expected 'reactants -> products'.");
        var reactants = Species(sides[0]);
        var products = Species(sides[1]);
        var species = reactants.Concat(products).Select(ChemicalFormula.Parse).ToList();
        var elements = species.SelectMany(s => s.Keys).Distinct().ToList();

        var matrix = elements
            .Select(e => species.Select((s, j) => (Rational)(s.GetValueOrDefault(e) * (j < reactants.Count ? 1 : -1))).ToArray())
            .ToArray();

        var pivots = RowReduce(matrix);
        var free = Enumerable.Range(0, species.Count).Where(c => !pivots.Contains(c)).ToList();
        if (free.Count == 0) throw new InvalidOperationException("The equation can't be balanced.");
        if (free.Count > 1) throw new InvalidOperationException("The equation has no unique balance; it combines independent reactions.");

        var solution = new Rational[species.Count];
        solution[free[0]] = Rational.One;
        for (var row = 0; row < pivots.Count; row++) solution[pivots[row]] = -matrix[row][free[0]];

        var lcm = solution.Aggregate(BigInteger.One, (acc, r) => acc * r.Denominator / BigInteger.GreatestCommonDivisor(acc, r.Denominator));
        var whole = solution.Select(r => r.Numerator * (lcm / r.Denominator)).ToList();
        var gcd = whole.Aggregate(BigInteger.Zero, BigInteger.GreatestCommonDivisor);
        var result = whole.Select(w => (long)(w / gcd)).ToList();
        if (result.Any(c => c <= 0)) throw new InvalidOperationException("The equation can't be balanced with positive amounts.");
        return result;
    }

    /// <summary>The equation with coefficients written in, e.g. "2H2 + O2 -> 2H2O".</summary>
    public static string Balance(string equation)
    {
        var coefficients = Coefficients(equation); // validates the shape too
        var sides = equation.Split("->");
        var reactants = Species(sides[0]);
        var products = Species(sides[1]);
        string Write(IEnumerable<string> side, int offset) =>
            string.Join(" + ", side.Select((s, i) => (coefficients[offset + i] == 1 ? "" : coefficients[offset + i].ToString()) + s));
        return $"{Write(reactants, 0)} -> {Write(products, reactants.Count)}";
    }

    private static List<string> Species(string side) =>
        side.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    /// <summary>Gauss–Jordan elimination to reduced row echelon form in place; returns the pivot column of each row.</summary>
    private static List<int> RowReduce(Rational[][] m)
    {
        var pivots = new List<int>();
        var columns = m.Length == 0 ? 0 : m[0].Length;
        var row = 0;
        for (var col = 0; col < columns && row < m.Length; col++)
        {
            var pivot = Array.FindIndex(m, row, r => r[col] != Rational.Zero);
            if (pivot < 0) continue;
            (m[row], m[pivot]) = (m[pivot], m[row]);

            var scale = m[row][col];
            for (var c = 0; c < columns; c++) m[row][c] /= scale;
            for (var r = 0; r < m.Length; r++)
            {
                if (r == row || m[r][col] == Rational.Zero) continue;
                var factor = m[r][col];
                for (var c = 0; c < columns; c++) m[r][c] -= factor * m[row][c];
            }
            pivots.Add(col);
            row++;
        }
        return pivots;
    }
}
