using System.Text;
using System.Text.Json;

namespace CSharpExercises.Web;

public sealed record Page<T>(IReadOnlyList<T> Items, string? NextCursor);

/// <summary>
/// Paging through a list that changes while you read it. Offset paging ("skip 40, take 20") gets slow on deep pages and
/// duplicates or skips rows when items are inserted. Cursor (keyset) paging remembers the sort key of the last item and
/// asks for items after it: stable under inserts and fast with an index. The cursor is opaque to clients (base64url
/// JSON) so its format can change, and it carries the sort key plus a unique tiebreaker.
/// </summary>
public static class Pagination
{
    public static IReadOnlyList<T> Offset<T>(IEnumerable<T> ordered, int page, int pageSize) =>
        ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

    /// <summary>Items ordered by (key, id), returning those strictly after the cursor's position.</summary>
    public static Page<T> Cursor<T>(IEnumerable<T> items, Func<T, long> key, Func<T, long> id, string? cursor, int pageSize)
    {
        var after = cursor is null ? null : Decode(cursor);
        var page = items
            .OrderBy(key).ThenBy(id)
            .Where(item => after is null || key(item) > after.Value.Key || (key(item) == after.Value.Key && id(item) > after.Value.Id))
            .Take(pageSize + 1) // one extra to know whether there's a next page
            .ToList();

        var hasMore = page.Count > pageSize;
        if (hasMore) page.RemoveAt(page.Count - 1);
        return new Page<T>(page, hasMore ? Encode(key(page[^1]), id(page[^1])) : null);
    }

    public static string Encode(long key, long id) =>
        Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new { k = key, i = id })).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static (long Key, long Id)? Decode(string cursor)
    {
        try
        {
            var b64 = cursor.Replace('-', '+').Replace('_', '/');
            var json = JsonDocument.Parse(Convert.FromBase64String(b64 + new string('=', (4 - b64.Length % 4) % 4)));
            return (json.RootElement.GetProperty("k").GetInt64(), json.RootElement.GetProperty("i").GetInt64());
        }
        catch (Exception e) when (e is FormatException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new ArgumentException("Invalid cursor.", nameof(cursor), e); // a 400 for the client, not a 500
        }
    }

    /// <summary>An RFC 8288 Link header for the next page.</summary>
    public static string? LinkHeader(string baseUrl, string? nextCursor) =>
        nextCursor is null ? null : $"<{baseUrl}?cursor={Uri.EscapeDataString(nextCursor)}>; rel=\"next\"";
}
