using System.Globalization;
using System.Text;

namespace CSharpExercises.Patterns;

public sealed record SalesRow(string Region, int Units, decimal Revenue);

/// <summary>
/// Template method: the base class fixes the steps of an algorithm (header, rows, totals, footer) and the order they
/// run in; subclasses fill in how each step looks. Shared rules, like sorting and computing totals, can't be
/// forgotten by a new format because they live in the template.
/// </summary>
public abstract class ReportExporter
{
    public string Export(string title, IEnumerable<SalesRow> rows)
    {
        var sorted = rows.OrderByDescending(r => r.Revenue).ToList();
        var sb = new StringBuilder();
        WriteHeader(sb, title);
        foreach (var row in sorted) WriteRow(sb, row);
        if (IncludeTotals) WriteTotals(sb, sorted.Sum(r => r.Units), sorted.Sum(r => r.Revenue));
        WriteFooter(sb);
        return sb.ToString();
    }

    protected abstract void WriteHeader(StringBuilder sb, string title);
    protected abstract void WriteRow(StringBuilder sb, SalesRow row);
    protected abstract void WriteTotals(StringBuilder sb, int units, decimal revenue);

    /// <summary>Hooks: optional steps with a default that subclasses may override.</summary>
    protected virtual void WriteFooter(StringBuilder sb) { }
    protected virtual bool IncludeTotals => true;

    protected static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
}

public sealed class CsvReportExporter : ReportExporter
{
    protected override void WriteHeader(StringBuilder sb, string title) => sb.Append("region,units,revenue\n");
    protected override void WriteRow(StringBuilder sb, SalesRow row) => sb.Append($"{row.Region},{row.Units},{Money(row.Revenue)}\n");
    protected override void WriteTotals(StringBuilder sb, int units, decimal revenue) { }
    protected override bool IncludeTotals => false; // totals would break tools that import the file
}

public sealed class MarkdownReportExporter : ReportExporter
{
    protected override void WriteHeader(StringBuilder sb, string title) =>
        sb.Append($"## {title}\n\n| Region | Units | Revenue |\n|---|---:|---:|\n");
    protected override void WriteRow(StringBuilder sb, SalesRow row) => sb.Append($"| {row.Region} | {row.Units} | {Money(row.Revenue)} |\n");
    protected override void WriteTotals(StringBuilder sb, int units, decimal revenue) => sb.Append($"| **Total** | **{units}** | **{Money(revenue)}** |\n");
    protected override void WriteFooter(StringBuilder sb) => sb.Append("\n_Sorted by revenue._\n");
}
