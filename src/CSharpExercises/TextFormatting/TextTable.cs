using System.Globalization;
using System.Text;

namespace CSharpExercises.TextFormatting;

public enum Align { Left, Right, Center }

/// <summary>
/// Rendering rows as an aligned plain-text table for console output: column widths from the widest cell, numbers
/// right-aligned by default, a header separator, and long cells truncated with an ellipsis when a maximum width is set.
/// </summary>
public sealed class TextTable(params string[] headers)
{
    private readonly List<string[]> _rows = [];
    private readonly Align?[] _align = new Align?[headers.Length];

    public int? MaxColumnWidth { get; init; }

    public TextTable AddRow(params object?[] cells)
    {
        if (cells.Length != headers.Length) throw new ArgumentException($"Expected {headers.Length} cells, got {cells.Length}.", nameof(cells));
        _rows.Add(cells.Select(c => Convert.ToString(c, CultureInfo.InvariantCulture) ?? "").ToArray());
        return this;
    }

    public TextTable AlignColumn(int column, Align align)
    {
        _align[column] = align;
        return this;
    }

    public string Render()
    {
        var all = _rows.Prepend(headers).Select(r => r.Select(Fit).ToArray()).ToList();
        var widths = Enumerable.Range(0, headers.Length).Select(c => all.Max(r => r[c].Length)).ToArray();
        var alignment = Enumerable.Range(0, headers.Length)
            .Select(c => _align[c] ?? (_rows.Count > 0 && _rows.All(r => double.TryParse(r[c], NumberStyles.Any, CultureInfo.InvariantCulture, out _)) ? Align.Right : Align.Left))
            .ToArray();

        var sb = new StringBuilder();
        for (var r = 0; r < all.Count; r++)
        {
            sb.AppendJoin(" | ", all[r].Select((cell, c) => Pad(cell, widths[c], alignment[c]))).Append('\n');
            if (r == 0) sb.AppendJoin("-+-", widths.Select(w => new string('-', w))).Append('\n');
        }
        return sb.ToString();
    }

    private string Fit(string cell) =>
        MaxColumnWidth is { } max && cell.Length > max ? cell[..(max - 1)] + "…" : cell;

    private static string Pad(string cell, int width, Align align) => align switch
    {
        Align.Right => cell.PadLeft(width),
        Align.Center => cell.PadLeft((width + cell.Length) / 2).PadRight(width),
        _ => cell.PadRight(width),
    };
}
