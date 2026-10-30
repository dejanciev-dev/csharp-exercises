using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace CSharpExercises.TextFormatting;

/// <summary>
/// A small Markdown-to-HTML converter: headings, paragraphs, bullet and numbered lists, fenced code blocks, and inline
/// **bold**, *italic*, `code` and [links](url). Block structure is found line by line first, then inline formatting
/// is applied to the escaped text, so user input can't inject HTML. Code spans are protected from further formatting.
/// </summary>
public static partial class MiniMarkdown
{
    public static string ToHtml(string markdown)
    {
        var html = new StringBuilder();
        var paragraph = new List<string>();
        string? openList = null;
        var inCode = false;

        void CloseParagraph()
        {
            if (paragraph.Count > 0) html.Append($"<p>{Inline(string.Join(' ', paragraph))}</p>\n");
            paragraph.Clear();
        }

        void CloseList()
        {
            if (openList is not null) html.Append($"</{openList}>\n");
            openList = null;
        }

        foreach (var raw in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.TrimEnd();
            if (line.StartsWith("```"))
            {
                CloseParagraph();
                CloseList();
                html.Append(inCode ? "</code></pre>\n" : "<pre><code>");
                inCode = !inCode;
                continue;
            }
            if (inCode)
            {
                html.Append(WebUtility.HtmlEncode(raw)).Append('\n');
                continue;
            }

            if (line.Length == 0)
            {
                CloseParagraph();
                CloseList();
            }
            else if (HeadingPattern().Match(line) is { Success: true } h)
            {
                CloseParagraph();
                CloseList();
                var level = h.Groups[1].Length;
                html.Append($"<h{level}>{Inline(h.Groups[2].Value)}</h{level}>\n");
            }
            else if (ListPattern().Match(line) is { Success: true } item)
            {
                CloseParagraph();
                var kind = char.IsDigit(item.Groups[1].Value[0]) ? "ol" : "ul";
                if (openList != kind)
                {
                    CloseList();
                    html.Append($"<{kind}>\n");
                    openList = kind;
                }
                html.Append($"<li>{Inline(item.Groups[2].Value)}</li>\n");
            }
            else
            {
                CloseList();
                paragraph.Add(line.Trim());
            }
        }

        if (inCode) html.Append("</code></pre>\n"); // an unclosed fence runs to the end
        CloseParagraph();
        CloseList();
        return html.ToString();
    }

    private static string Inline(string text)
    {
        var codeSpans = new List<string>();
        var escaped = WebUtility.HtmlEncode(text);

        // Pull code spans out first so their contents aren't formatted, then put them back at the end.
        escaped = CodePattern().Replace(escaped, m =>
        {
            codeSpans.Add(m.Groups[1].Value);
            return $"\u0000{codeSpans.Count - 1}\u0000";
        });
        escaped = BoldPattern().Replace(escaped, "<strong>$1</strong>");
        escaped = ItalicPattern().Replace(escaped, "<em>$1</em>");
        escaped = LinkPattern().Replace(escaped, m =>
        {
            var url = WebUtility.HtmlDecode(m.Groups[2].Value);
            return url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)
                ? m.Groups[1].Value
                : $"<a href=\"{WebUtility.HtmlEncode(url)}\">{m.Groups[1].Value}</a>";
        });
        return PlaceholderPattern().Replace(escaped, m => $"<code>{codeSpans[int.Parse(m.Groups[1].Value)]}</code>");
    }

    [GeneratedRegex(@"^(#{1,6})\s+(.*)$")] private static partial Regex HeadingPattern();
    [GeneratedRegex(@"^\s*([-*]|\d+\.)\s+(.*)$")] private static partial Regex ListPattern();
    [GeneratedRegex(@"`([^`]+)`")] private static partial Regex CodePattern();
    [GeneratedRegex(@"\*\*(.+?)\*\*")] private static partial Regex BoldPattern();
    [GeneratedRegex(@"\*(.+?)\*")] private static partial Regex ItalicPattern();
    [GeneratedRegex(@"\[([^\]]+)\]\(((?:[^()\s]|\([^()\s]*\))+)\)")] private static partial Regex LinkPattern(); // allows one level of (parentheses)
    [GeneratedRegex("\u0000(\\d+)\u0000")] private static partial Regex PlaceholderPattern();
}
