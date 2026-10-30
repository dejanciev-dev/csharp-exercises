using System.Security.Cryptography;
using System.Text;

namespace CSharpExercises.Ciphers;

/// <summary>
/// How to actually encrypt data in .NET: AES-GCM, an authenticated cipher that also detects tampering, with a fresh
/// random 12-byte nonce per message (reusing a nonce with the same key is catastrophic). The output is
/// nonce ‖ tag ‖ ciphertext so it's self-contained. Associated data (a user id, a version) is authenticated but not
/// encrypted, binding the ciphertext to its context so it can't be replayed elsewhere.
/// </summary>
public sealed class AuthenticatedEncryption(byte[] key)
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public static byte[] NewKey() => RandomNumberGenerator.GetBytes(32);

    public byte[] Encrypt(string plaintext, string associatedData = "")
    {
        var data = Encoding.UTF8.GetBytes(plaintext);
        var output = new byte[NonceSize + TagSize + data.Length];
        var nonce = output.AsSpan(0, NonceSize);
        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, data, output.AsSpan(NonceSize + TagSize), output.AsSpan(NonceSize, TagSize), Encoding.UTF8.GetBytes(associatedData));
        return output;
    }

    /// <summary>Throws <see cref="AuthenticationTagMismatchException"/> if anything was changed: never returns forged plaintext.</summary>
    public string Decrypt(byte[] message, string associatedData = "")
    {
        if (message.Length < NonceSize + TagSize) throw new CryptographicException("Message too short.");
        var plaintext = new byte[message.Length - NonceSize - TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(message.AsSpan(0, NonceSize), message.AsSpan(NonceSize + TagSize), message.AsSpan(NonceSize, TagSize), plaintext, Encoding.UTF8.GetBytes(associatedData));
        return Encoding.UTF8.GetString(plaintext);
    }

    /// <summary>A key from a password: slow and salted, never the password bytes themselves.</summary>
    public static byte[] KeyFromPassword(string password, byte[] salt, int iterations = 600_000) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, 32);
}
