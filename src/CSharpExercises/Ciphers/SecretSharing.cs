using System.Numerics;
using System.Security.Cryptography;

namespace CSharpExercises.Ciphers;

/// <summary>
/// Shamir's secret sharing: split a secret into n shares so that any k of them recover it and k - 1 reveal nothing (a
/// vault opened by any 3 of 5 directors). The secret is the constant term of a random polynomial of degree k - 1 over a
/// prime field; each share is a point on it, and k points determine the polynomial by Lagrange interpolation at x = 0.
/// </summary>
public static class SecretSharing
{
    /// <summary>The Mersenne prime 2^127 - 1: secrets up to 16 bytes fit below it.</summary>
    public static readonly BigInteger Prime = BigInteger.Pow(2, 127) - 1;

    public static IReadOnlyList<(int X, BigInteger Y)> Split(BigInteger secret, int threshold, int shares)
    {
        if (secret < 0 || secret >= Prime) throw new ArgumentOutOfRangeException(nameof(secret));
        if (threshold < 1 || threshold > shares) throw new ArgumentException("Need 1 ≤ threshold ≤ shares.");

        var coefficients = new BigInteger[threshold];
        coefficients[0] = secret;
        for (var i = 1; i < threshold; i++) coefficients[i] = new BigInteger(RandomNumberGenerator.GetBytes(16), isUnsigned: true) % Prime;

        return Enumerable.Range(1, shares).Select(x =>
        {
            BigInteger y = 0;
            for (var i = threshold - 1; i >= 0; i--) y = (y * x + coefficients[i]) % Prime; // Horner's rule
            return (x, y);
        }).ToList();
    }

    public static BigInteger Combine(IReadOnlyCollection<(int X, BigInteger Y)> shares)
    {
        if (shares.Select(s => s.X).Distinct().Count() != shares.Count) throw new ArgumentException("Shares must be distinct.");
        BigInteger secret = 0;
        foreach (var (xi, yi) in shares)
        {
            // The Lagrange basis polynomial for xi, evaluated at 0: Π xj / (xj - xi).
            BigInteger numerator = 1, denominator = 1;
            foreach (var (xj, _) in shares)
            {
                if (xj == xi) continue;
                numerator = numerator * xj % Prime;
                denominator = denominator * Mod(xj - xi) % Prime;
            }
            secret = Mod(secret + yi * numerator % Prime * BigInteger.ModPow(denominator, Prime - 2, Prime));
        }
        return secret;
    }

    public static BigInteger FromText(string text) => new(System.Text.Encoding.UTF8.GetBytes(text), isUnsigned: true, isBigEndian: true);

    public static string ToText(BigInteger value) => System.Text.Encoding.UTF8.GetString(value.ToByteArray(isUnsigned: true, isBigEndian: true));

    private static BigInteger Mod(BigInteger x) => (x % Prime + Prime) % Prime;
}
