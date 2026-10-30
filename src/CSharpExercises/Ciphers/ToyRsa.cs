using System.Numerics;
using System.Security.Cryptography;

namespace CSharpExercises.Ciphers;

/// <summary>
/// RSA from the maths up, to see why it works: pick primes p and q, publish n = pq and e, keep d = e⁻¹ mod φ(n).
/// Encryption is m^e mod n, decryption c^d mod n, and signing is decryption applied to a hash. This toy uses small keys
/// and no padding, which makes it insecure (equal messages encrypt equally, small messages can be recovered); real code
/// uses <see cref="RSA"/> with OAEP or PSS padding and 2048+ bit keys.
/// </summary>
public sealed class ToyRsa
{
    public BigInteger N { get; }
    public BigInteger E { get; }
    private readonly BigInteger _d;

    public ToyRsa(BigInteger p, BigInteger q, BigInteger? e = null)
    {
        if (p == q) throw new ArgumentException("p and q must differ.");
        N = p * q;
        var phi = (p - 1) * (q - 1);
        E = e ?? 65537;
        if (BigInteger.GreatestCommonDivisor(E, phi) != 1) throw new ArgumentException("e must be coprime with φ(n).");
        _d = ModInverse(E, phi);
    }

    /// <summary>Generates a key with random primes of the given size (Miller–Rabin from <see cref="BigInteger.IsProbablePrime"/>-style testing).</summary>
    public static ToyRsa Generate(int bitsPerPrime)
    {
        while (true)
        {
            var p = RandomPrime(bitsPerPrime);
            var q = RandomPrime(bitsPerPrime);
            if (p != q && BigInteger.GreatestCommonDivisor(65537, (p - 1) * (q - 1)) == 1) return new ToyRsa(p, q);
        }
    }

    public BigInteger Encrypt(BigInteger message) =>
        message >= 0 && message < N ? BigInteger.ModPow(message, E, N) : throw new ArgumentOutOfRangeException(nameof(message), "The message must be smaller than n.");

    public BigInteger Decrypt(BigInteger ciphertext) => BigInteger.ModPow(ciphertext, _d, N);

    public BigInteger Sign(byte[] data) => BigInteger.ModPow(HashToNumber(data), _d, N);

    public bool Verify(byte[] data, BigInteger signature) => BigInteger.ModPow(signature, E, N) == HashToNumber(data);

    /// <summary>The extended Euclidean algorithm: x with a·x ≡ 1 (mod m).</summary>
    public static BigInteger ModInverse(BigInteger a, BigInteger m)
    {
        (BigInteger oldR, BigInteger r) = (a, m);
        (BigInteger oldS, BigInteger s) = (1, 0);
        while (r != 0)
        {
            var quotient = oldR / r;
            (oldR, r) = (r, oldR - quotient * r);
            (oldS, s) = (s, oldS - quotient * s);
        }
        if (oldR != 1) throw new ArgumentException("Not invertible.");
        return (oldS % m + m) % m;
    }

    private BigInteger HashToNumber(byte[] data) => new BigInteger(SHA256.HashData(data), isUnsigned: true) % N;

    private static BigInteger RandomPrime(int bits)
    {
        var bytes = new byte[(bits + 7) / 8];
        while (true)
        {
            RandomNumberGenerator.Fill(bytes);
            bytes[^1] |= (byte)(1 << ((bits - 1) % 8)); // top bit set: full size
            bytes[0] |= 1;                              // odd
            var candidate = new BigInteger(bytes, isUnsigned: true);
            if (IsProbablePrime(candidate)) return candidate;
        }
    }

    private static bool IsProbablePrime(BigInteger n, int rounds = 20)
    {
        if (n < 4) return n >= 2;
        if (n.IsEven) return false;
        var (d, s) = (n - 1, 0);
        while (d.IsEven) (d, s) = (d / 2, s + 1);
        var random = new Random(n.GetHashCode());
        for (var i = 0; i < rounds; i++)
        {
            var a = 2 + new BigInteger(random.NextInt64(long.MaxValue)) % (n - 3);
            var x = BigInteger.ModPow(a, d, n);
            if (x == 1 || x == n - 1) continue;
            var composite = true;
            for (var r = 1; r < s && composite; r++)
            {
                x = BigInteger.ModPow(x, 2, n);
                if (x == n - 1) composite = false;
            }
            if (composite) return false;
        }
        return true;
    }
}
