using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CSharpExercises.Security;

/// <summary>
/// A JSON Web Token signed with HMAC-SHA256 (HS256), built by hand: base64url(header) . base64url(payload) .
/// base64url(signature). The payload is only encoded, not encrypted, so anyone can read it; the signature proves the
/// server issued it unmodified. Verification checks the algorithm explicitly (the infamous "alg: none" attack),
/// compares signatures in constant time, and enforces expiry with a little clock-skew leeway.
/// </summary>
public sealed class SignedToken(byte[] key, TimeProvider? time = null)
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    public string Create(IReadOnlyDictionary<string, object> claims, TimeSpan lifetime)
    {
        var header = new JsonObject { ["alg"] = "HS256", ["typ"] = "JWT" };
        var payload = JsonSerializer.SerializeToNode(claims)!.AsObject();
        payload["exp"] = _time.GetUtcNow().Add(lifetime).ToUnixTimeSeconds();

        var unsigned = $"{Base64Url(Encoding.UTF8.GetBytes(header.ToJsonString()))}.{Base64Url(Encoding.UTF8.GetBytes(payload.ToJsonString()))}";
        return $"{unsigned}.{Base64Url(Sign(unsigned))}";
    }

    /// <summary>Returns the claims, or throws <see cref="SecurityTokenException"/> explaining what's wrong.</summary>
    public JsonObject Validate(string token, TimeSpan? clockSkew = null)
    {
        var parts = token.Split('.');
        if (parts.Length != 3) throw new SecurityTokenException("A JWT has three parts.");

        var header = JsonNode.Parse(FromBase64Url(parts[0]))?.AsObject() ?? throw new SecurityTokenException("Bad header.");
        if ((string?)header["alg"] != "HS256") throw new SecurityTokenException($"Algorithm '{header["alg"]}' is not accepted.");

        var expected = Sign($"{parts[0]}.{parts[1]}");
        if (!CryptographicOperations.FixedTimeEquals(expected, FromBase64Url(parts[2])))
            throw new SecurityTokenException("Invalid signature.");

        var payload = JsonNode.Parse(FromBase64Url(parts[1]))!.AsObject();
        var expires = DateTimeOffset.FromUnixTimeSeconds((long)payload["exp"]!);
        if (_time.GetUtcNow() > expires + (clockSkew ?? TimeSpan.FromSeconds(30))) throw new SecurityTokenException("Token expired.");
        return payload;
    }

    private byte[] Sign(string data) => HMACSHA256.HashData(key, Encoding.ASCII.GetBytes(data));

    public static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static byte[] FromBase64Url(string text)
    {
        var s = text.Replace('-', '+').Replace('_', '/');
        try
        {
            return Convert.FromBase64String(s + new string('=', (4 - s.Length % 4) % 4));
        }
        catch (FormatException)
        {
            throw new SecurityTokenException("Malformed base64url.");
        }
    }
}

public sealed class SecurityTokenException(string message) : Exception(message);
