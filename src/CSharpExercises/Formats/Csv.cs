using System.Text;

namespace CSharpExercises.Formats;

/// <summary>
/// CSV as specified by RFC 4180, which "split on commas" gets wrong: fields can be quoted, quoted fields can contain
/// commas, newlines and doubled quotes (""). A small state machine reads one character at a time.
/// </summary>
public static class Csv
{
    public static IEnumerable<string[]> Parse(TextReader reader, char separator = ',')
    {
        var record = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var fieldStarted = false; // distinguishes an empty last line from a line with one empty field
        var line = 1;

        int c;
        while ((c = reader.Read()) != -1)
        {
            var ch = (char)c;
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (reader.Peek() == '"') field.Append((char)reader.Read()); // "" is an escaped quote
                    else inQuotes = false;
                }
                else
                {
                    if (ch == '\n') line++;
                    field.Append(ch);
                }
                continue;
            }

            if (ch == '"')
            {
                if (field.Length > 0) throw new FormatException($"Unexpected quote inside an unquoted field on line {line}.");
                inQuotes = fieldStarted = true;
            }
            else if (ch == separator)
            {
                record.Add(field.ToString());
                field.Clear();
                fieldStarted = true;
            }
            else if (ch is '\r' or '\n')
            {
                if (ch == '\r' && reader.Peek() == '\n') reader.Read();
                if (fieldStarted || field.Length > 0 || record.Count > 0)
                {
                    record.Add(field.ToString());
                    yield return [.. record];
                }
                record.Clear();
                field.Clear();
                fieldStarted = false;
                line++;
            }
            else
            {
                field.Append(ch);
                fieldStarted = true;
            }
        }

        if (inQuotes) throw new FormatException($"Unterminated quoted field at end of input (line {line}).");
        if (fieldStarted || field.Length > 0 || record.Count > 0)
        {
            record.Add(field.ToString());
            yield return [.. record];
        }
    }

    public static IEnumerable<string[]> Parse(string text, char separator = ',') => Parse(new StringReader(text), separator);

    /// <summary>Quotes a field only when it has to: when it contains the separator, a quote or a line break.</summary>
    public static string FormatRecord(IEnumerable<string> fields, char separator = ',') =>
        string.Join(separator, fields.Select(f =>
            f.IndexOfAny([separator, '"', '\r', '\n']) >= 0 ? $"\"{f.Replace("\"", "\"\"")}\"" : f));

    /// <summary>Uses the first row as headers and maps each following row to a dictionary.</summary>
    public static IEnumerable<IReadOnlyDictionary<string, string>> ParseWithHeaders(string text)
    {
        using var rows = Parse(text).GetEnumerator();
        if (!rows.MoveNext()) yield break;
        var headers = rows.Current;

        var number = 1;
        while (rows.MoveNext())
        {
            number++;
            if (rows.Current.Length != headers.Length)
                throw new FormatException($"Row {number} has {rows.Current.Length} fields but the header has {headers.Length}.");
            yield return headers.Zip(rows.Current).ToDictionary(p => p.First, p => p.Second);
        }
    }
}
