using System.Buffers.Binary;
using System.Security.Cryptography;

namespace CSharpExercises.Security;

/// <summary>
/// Time-based one-time passwords (RFC 6238), the six-digit codes in authenticator apps. Both sides share a secret; the
/// code is HMAC(secret, number of 30-second steps since 1970), cut down to 6 digits by "dynamic truncation" (RFC 4226).
/// Verification accepts a step either side for clock drift, and remembers the last used step so a code can't be replayed.
/// </summary>
public sealed class Totp(byte[] secret, TimeProvider? time = null, int digits = 6, int stepSeconds = 30)
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private long _lastUsedStep = -1;

    public string Generate() => Generate(CurrentStep());

    public string Generate(long step)
    {
        Span<byte> counter = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counter, step);
        var hash = HMACSHA1.HashData(secret, counter);

        // Dynamic truncation: the low 4 bits of the last byte pick where to read 31 bits from.
        var offset = hash[^1] & 0x0F;
        var binary = BinaryPrimitives.ReadInt32BigEndian(hash.AsSpan(offset)) & 0x7FFFFFFF;
        return (binary % (int)Math.Pow(10, digits)).ToString().PadLeft(digits, '0');
    }

    public bool Verify(string code, int allowedDrift = 1)
    {
        var now = CurrentStep();
        for (var step = now - allowedDrift; step <= now + allowedDrift; step++)
        {
            if (step <= _lastUsedStep) continue; // already used: a replayed code
            if (!CryptographicOperations.FixedTimeEquals(System.Text.Encoding.ASCII.GetBytes(Generate(step)), System.Text.Encoding.ASCII.GetBytes(code))) continue;
            _lastUsedStep = step;
            return true;
        }
        return false;
    }

    /// <summary>The otpauth:// URI that authenticator apps read from a QR code.</summary>
    public string ProvisioningUri(string issuer, string account) =>
        $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(account)}?secret={Base32NoPadding(secret)}&issuer={Uri.EscapeDataString(issuer)}&digits={digits}&period={stepSeconds}";

    private long CurrentStep() => _time.GetUtcNow().ToUnixTimeSeconds() / stepSeconds;

    /// <summary>RFC 4648 base32 (A–Z, 2–7), the format authenticator apps expect secrets in.</summary>
    public static string Base32NoPadding(byte[] data)
    {
        const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var sb = new System.Text.StringBuilder();
        int buffer = 0, bits = 0;
        foreach (var b in data)
        {
            buffer = buffer << 8 | b;
            bits += 8;
            while (bits >= 5) sb.Append(Alphabet[buffer >> (bits -= 5) & 31]);
        }
        if (bits > 0) sb.Append(Alphabet[buffer << (5 - bits) & 31]);
        return sb.ToString();
    }
}
