using System.Globalization;
using System.Text;

namespace CSharpExercises.DataProcessing;

/// <summary>
/// A pivot table, as in Excel: group rows by one key down the side and another across the top, aggregate the values in
/// each cell, and add totals. Missing combinations stay empty rather than zero, so "no sales" and "sales of 0" differ.
/// </summary>
public sealed class PivotTable<TRow>
{
    private readonly Dictionary<(string Row, string Column), decimal> _cells = new();

    public PivotTable(IEnumerable<TRow> data, Func<TRow, string> rowKey, Func<TRow, string> columnKey, Func<TRow, decimal> value,
        Func<IEnumerable<decimal>, decimal>? aggregate = null)
    {
        aggregate ??= values => values.Sum();
        var groups = data.GroupBy(d => (rowKey(d), columnKey(d)), value).ToList();
        foreach (var g in groups) _cells[g.Key] = aggregate(g);

        RowKeys = groups.Select(g => g.Key.Item1).Distinct().Order(StringComparer.Ordinal).ToList();
        ColumnKeys = groups.Select(g => g.Key.Item2).Distinct().Order(StringComparer.Ordinal).ToList();
    }

    public IReadOnlyList<string> RowKeys { get; }
    public IReadOnlyList<string> ColumnKeys { get; }

    public decimal? this[string row, string column] => _cells.TryGetValue((row, column), out var v) ? v : null;

    public decimal RowTotal(string row) => ColumnKeys.Sum(c => this[row, c] ?? 0);
    public decimal ColumnTotal(string column) => RowKeys.Sum(r => this[r, column] ?? 0);
    public decimal GrandTotal => _cells.Values.Sum();

    /// <summary>A fixed-width text rendering with a total row and column.</summary>
    public string Render()
    {
        static string N(decimal? v) => v?.ToString("0.##", CultureInfo.InvariantCulture) ?? "-";
        var header = new[] { "" }.Concat(ColumnKeys).Append("Total").ToList();
        var rows = RowKeys.Select(r => new[] { r }.Concat(ColumnKeys.Select(c => N(this[r, c]))).Append(N(RowTotal(r))).ToList()).ToList();
        rows.Add(["Total", .. ColumnKeys.Select(c => N(ColumnTotal(c))), N(GrandTotal)]);

        var widths = header.Select((h, i) => Math.Max(h.Length, rows.Max(r => r[i].Length))).ToArray();
        var sb = new StringBuilder();
        foreach (var line in rows.Prepend(header))
            sb.AppendLine(string.Join(" | ", line.Select((cell, i) => i == 0 ? cell.PadRight(widths[i]) : cell.PadLeft(widths[i]))).TrimEnd());
        return sb.ToString();
    }
}
