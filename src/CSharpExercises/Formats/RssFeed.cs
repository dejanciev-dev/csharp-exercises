using System.Globalization;
using System.Xml.Linq;

namespace CSharpExercises.Formats;

public sealed record FeedItem(string Title, string Link, DateTimeOffset Published, string? Summary = null);

/// <summary>
/// Building and reading RSS 2.0 and Atom feeds with LINQ to XML: functional construction (the XML tree is one expression
/// mirroring the document), namespaces for Atom, RFC 822 dates for RSS, and XML escaping handled by the library rather
/// than string concatenation. The reader accepts both formats and normalises them to the same items.
/// </summary>
public static class RssFeed
{
    private static readonly XNamespace Atom = "http://www.w3.org/2005/Atom";

    public static string ToRss(string title, string link, IEnumerable<FeedItem> items) =>
        new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("rss", new XAttribute("version", "2.0"),
                new XElement("channel",
                    new XElement("title", title),
                    new XElement("link", link),
                    items.OrderByDescending(i => i.Published).Select(i =>
                        new XElement("item",
                            new XElement("title", i.Title),
                            new XElement("link", i.Link),
                            new XElement("guid", i.Link),
                            new XElement("pubDate", i.Published.ToUniversalTime().ToString("r", CultureInfo.InvariantCulture)),
                            i.Summary is null ? null : new XElement("description", i.Summary))))))
        .ToString();

    public static string ToAtom(string title, string id, IEnumerable<FeedItem> items)
    {
        var list = items.OrderByDescending(i => i.Published).ToList();
        return new XDocument(
            new XElement(Atom + "feed",
                new XElement(Atom + "title", title),
                new XElement(Atom + "id", id),
                new XElement(Atom + "updated", (list.Count > 0 ? list[0].Published : DateTimeOffset.UnixEpoch).ToString("o", CultureInfo.InvariantCulture)),
                list.Select(i =>
                    new XElement(Atom + "entry",
                        new XElement(Atom + "title", i.Title),
                        new XElement(Atom + "link", new XAttribute("href", i.Link)),
                        new XElement(Atom + "id", i.Link),
                        new XElement(Atom + "updated", i.Published.ToString("o", CultureInfo.InvariantCulture)),
                        i.Summary is null ? null : new XElement(Atom + "summary", i.Summary)))))
            .ToString();
    }

    /// <summary>Reads either format, detected from the root element.</summary>
    public static IReadOnlyList<FeedItem> Parse(string xml)
    {
        var root = XDocument.Parse(xml).Root ?? throw new FormatException("Empty document.");
        if (root.Name == "rss")
            return root.Element("channel")!.Elements("item").Select(i => new FeedItem(
                (string?)i.Element("title") ?? "",
                (string?)i.Element("link") ?? "",
                DateTimeOffset.Parse((string?)i.Element("pubDate") ?? "", CultureInfo.InvariantCulture),
                (string?)i.Element("description"))).ToList();

        if (root.Name == Atom + "feed")
            return root.Elements(Atom + "entry").Select(e => new FeedItem(
                (string?)e.Element(Atom + "title") ?? "",
                (string?)e.Element(Atom + "link")?.Attribute("href") ?? "",
                DateTimeOffset.Parse((string?)e.Element(Atom + "updated") ?? "", CultureInfo.InvariantCulture),
                (string?)e.Element(Atom + "summary"))).ToList();

        throw new FormatException($"Unknown feed format <{root.Name.LocalName}>.");
    }
}
