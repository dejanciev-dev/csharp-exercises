using System.Net;
using System.Text;

namespace CSharpExercises.Patterns;

/// <summary>
/// Visitor: new operations over a fixed set of node types without changing the nodes. Each node's Accept calls the
/// visitor method for its own type (double dispatch), and the compiler makes every visitor handle every node type.
/// Modern C# often uses a switch expression over records instead; the visitor shines when the operations are many and
/// written by other people.
/// </summary>
public interface IDocumentVisitor<out T>
{
    T VisitHeading(Heading heading);
    T VisitParagraph(Paragraph paragraph);
    T VisitBulletList(BulletList list);
    T VisitSection(Section section);
}

public abstract record DocumentNode
{
    public abstract T Accept<T>(IDocumentVisitor<T> visitor);
}

public sealed record Heading(int Level, string Text) : DocumentNode
{
    public override T Accept<T>(IDocumentVisitor<T> visitor) => visitor.VisitHeading(this);
}

public sealed record Paragraph(string Text) : DocumentNode
{
    public override T Accept<T>(IDocumentVisitor<T> visitor) => visitor.VisitParagraph(this);
}

public sealed record BulletList(IReadOnlyList<string> Items) : DocumentNode
{
    public override T Accept<T>(IDocumentVisitor<T> visitor) => visitor.VisitBulletList(this);
}

public sealed record Section(IReadOnlyList<DocumentNode> Children) : DocumentNode
{
    public override T Accept<T>(IDocumentVisitor<T> visitor) => visitor.VisitSection(this);
}

public sealed class HtmlRenderer : IDocumentVisitor<string>
{
    public string VisitHeading(Heading heading) => $"<h{heading.Level}>{WebUtility.HtmlEncode(heading.Text)}</h{heading.Level}>";
    public string VisitParagraph(Paragraph paragraph) => $"<p>{WebUtility.HtmlEncode(paragraph.Text)}</p>";
    public string VisitBulletList(BulletList list) =>
        $"<ul>{string.Concat(list.Items.Select(i => $"<li>{WebUtility.HtmlEncode(i)}</li>"))}</ul>";
    public string VisitSection(Section section) => $"<section>{string.Concat(section.Children.Select(c => c.Accept(this)))}</section>";
}

public sealed class MarkdownRenderer : IDocumentVisitor<string>
{
    public string VisitHeading(Heading heading) => $"{new string('#', heading.Level)} {heading.Text}\n";
    public string VisitParagraph(Paragraph paragraph) => $"{paragraph.Text}\n";
    public string VisitBulletList(BulletList list) => string.Concat(list.Items.Select(i => $"- {i}\n"));
    public string VisitSection(Section section) => string.Join("\n", section.Children.Select(c => c.Accept(this)));
}

public sealed class WordCounter : IDocumentVisitor<int>
{
    public int VisitHeading(Heading heading) => Count(heading.Text);
    public int VisitParagraph(Paragraph paragraph) => Count(paragraph.Text);
    public int VisitBulletList(BulletList list) => list.Items.Sum(Count);
    public int VisitSection(Section section) => section.Children.Sum(c => c.Accept(this));

    private static int Count(string text) => text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
}

/// <summary>Builds a table of contents, indenting by heading level.</summary>
public sealed class TableOfContents : IDocumentVisitor<StringBuilder>
{
    private readonly StringBuilder _toc = new();
    public StringBuilder VisitHeading(Heading heading) => _toc.Append(' ', 2 * (heading.Level - 1)).Append(heading.Text).Append('\n');
    public StringBuilder VisitParagraph(Paragraph paragraph) => _toc;
    public StringBuilder VisitBulletList(BulletList list) => _toc;
    public StringBuilder VisitSection(Section section)
    {
        foreach (var child in section.Children) child.Accept(this);
        return _toc;
    }
}
