namespace CSharpExercises.Simulations;

/// <summary>
/// Wolfram's elementary cellular automata: a row of cells where each new cell depends only on itself and its two
/// neighbours. The 8 possible neighbourhoods map to the 8 bits of the rule number, so rule 90 draws a Sierpiński
/// triangle, rule 30 looks random (Mathematica used it as a random generator) and rule 110 is Turing complete.
/// </summary>
public static class ElementaryAutomaton
{
    public static bool[] Step(bool[] cells, byte rule, bool wrap = false)
    {
        var next = new bool[cells.Length];
        for (var i = 0; i < cells.Length; i++)
        {
            var left = i > 0 ? cells[i - 1] : wrap && cells[^1];
            var right = i < cells.Length - 1 ? cells[i + 1] : wrap && cells[0];
            var pattern = (left ? 4 : 0) | (cells[i] ? 2 : 0) | (right ? 1 : 0);
            next[i] = (rule >> pattern & 1) == 1;
        }
        return next;
    }

    /// <summary>Starts from one live cell in the middle and draws each generation as a line of '#' and ' '.</summary>
    public static IReadOnlyList<string> Draw(byte rule, int generations)
    {
        var width = 2 * generations + 1;
        var cells = new bool[width];
        cells[generations] = true;

        var lines = new List<string>();
        for (var g = 0; g <= generations; g++)
        {
            lines.Add(new string(cells.Select(c => c ? '#' : ' ').ToArray()).TrimEnd());
            cells = Step(cells, rule);
        }
        return lines;
    }

    /// <summary>The centre column of rule 30 as a bit stream.</summary>
    public static IEnumerable<bool> Rule30Bits(int count)
    {
        var cells = new bool[2 * count + 1];
        cells[count] = true;
        for (var i = 0; i < count; i++)
        {
            yield return cells[count];
            cells = Step(cells, 30);
        }
    }
}
