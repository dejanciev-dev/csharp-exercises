using System.Security.Cryptography;
using System.Text;

namespace CSharpExercises.Web;

public sealed record HttpResult(int Status, string? Body = null, string? ETag = null);

/// <summary>
/// ETags make HTTP caching and concurrency safe. On GET, a client sends If-None-Match with the ETag it has; if nothing
/// changed the server answers 304 Not Modified with no body. On PUT, If-Match makes the update conditional, so two
/// editors can't overwrite each other: the loser gets 412 Precondition Failed (optimistic concurrency over HTTP).
/// The ETag here is a hash of the content, so it changes exactly when the content does.
/// </summary>
public sealed class ConditionalResource
{
    private readonly Dictionary<string, string> _documents = new();

    public static string ETagFor(string content) => $"\"{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)))[..16]}\"";

    public HttpResult Get(string id, string? ifNoneMatch = null)
    {
        if (!_documents.TryGetValue(id, out var content)) return new HttpResult(404);
        var etag = ETagFor(content);
        if (ifNoneMatch is not null && (ifNoneMatch == "*" || ifNoneMatch.Split(',').Select(e => e.Trim()).Contains(etag)))
            return new HttpResult(304, ETag: etag);
        return new HttpResult(200, content, etag);
    }

    /// <param name="ifMatch">The ETag the client last saw; "*" means "only if it exists"; null means unconditional.</param>
    public HttpResult Put(string id, string content, string? ifMatch = null, bool requireIfMatch = false)
    {
        var exists = _documents.TryGetValue(id, out var current);
        if (ifMatch is null && requireIfMatch && exists) return new HttpResult(428); // Precondition Required
        if (ifMatch is not null && (!exists || (ifMatch != "*" && ifMatch != ETagFor(current!)))) return new HttpResult(412);

        _documents[id] = content;
        return new HttpResult(exists ? 200 : 201, ETag: ETagFor(content));
    }

    public HttpResult Delete(string id, string? ifMatch = null)
    {
        if (!_documents.TryGetValue(id, out var current)) return new HttpResult(404);
        if (ifMatch is not null && ifMatch != ETagFor(current)) return new HttpResult(412);
        _documents.Remove(id);
        return new HttpResult(204);
    }
}
