using System.Numerics;
using System.Security.Cryptography;

namespace CSharpExercises.Ciphers;

/// <summary>
/// Diffie–Hellman key agreement: two parties who have never met agree on a shared secret over a public channel. Each
/// picks a private number, publishes g^private mod p, and raises the other's public value to its own private power;
/// both arrive at g^(ab) mod p, which an eavesdropper can't compute without solving a discrete logarithm. Shown with a
/// toy-sized group; real systems use ECDH (<see cref="ECDiffieHellman"/>) and derive keys with a KDF.
/// </summary>
public sealed class DiffieHellmanParty
{
    // The largest safe prime below 2^64 (p = 2q + 1 with q prime): big enough to demonstrate, far too small to be secure.
    public static readonly BigInteger P = BigInteger.Parse("18446744073709550147");
    public static readonly BigInteger G = 2;

    private readonly BigInteger _private;

    public DiffieHellmanParty(BigInteger? privateKey = null)
    {
        _private = privateKey ?? 2 + new BigInteger(RandomNumberGenerator.GetBytes(8), isUnsigned: true) % (P - 3);
        PublicKey = BigInteger.ModPow(G, _private, P);
    }

    public BigInteger PublicKey { get; }

    public BigInteger SharedSecret(BigInteger otherPublicKey)
    {
        // Reject values that would force the secret into a tiny subgroup (0, 1, p - 1).
        if (otherPublicKey <= 1 || otherPublicKey >= P - 1) throw new CryptographicException("Invalid public key.");
        return BigInteger.ModPow(otherPublicKey, _private, P);
    }

    /// <summary>A symmetric key from the shared secret: never use the raw number directly.</summary>
    public byte[] DeriveKey(BigInteger otherPublicKey, string context) =>
        HKDF.DeriveKey(HashAlgorithmName.SHA256, SharedSecret(otherPublicKey).ToByteArray(), 32, info: System.Text.Encoding.UTF8.GetBytes(context));

    /// <summary>The same agreement with real elliptic-curve keys from the framework.</summary>
    public static (byte[] Alice, byte[] Bob) RealEcdh()
    {
        using var alice = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        using var bob = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        return (alice.DeriveKeyFromHash(bob.PublicKey, HashAlgorithmName.SHA256), bob.DeriveKeyFromHash(alice.PublicKey, HashAlgorithmName.SHA256));
    }
}
