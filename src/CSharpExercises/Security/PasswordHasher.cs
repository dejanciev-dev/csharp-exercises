using System.Security.Cryptography;
using System.Text;

namespace CSharpExercises.Security;

/// <summary>
/// Storing passwords: never the password, never a fast hash like SHA-256 (billions of guesses per second on a GPU), but a
/// deliberately slow key-derivation function with a random salt per user, so identical passwords look different and
/// precomputed tables are useless. The stored string records the algorithm and cost, so the cost can be raised later
/// and old hashes upgraded at the next login. Verification compares in constant time.
/// </summary>
public sealed class PasswordHasher(int iterations = 600_000)
{
    private const int SaltSize = 16;
    private const int HashSize = 32;

    /// <returns>A self-describing string: "pbkdf2-sha256$iterations$salt$hash".</returns>
    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Derive(password, salt, iterations);
        return $"pbkdf2-sha256${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public PasswordVerification Verify(string password, string stored)
    {
        var parts = stored.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2-sha256" || !int.TryParse(parts[1], out var storedIterations))
            return PasswordVerification.Failed;

        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        var actual = Derive(password, salt, storedIterations);

        // Equality that takes the same time however many bytes match, so timing can't reveal the hash bit by bit.
        if (!CryptographicOperations.FixedTimeEquals(actual, expected)) return PasswordVerification.Failed;
        return storedIterations < iterations ? PasswordVerification.SuccessRehashNeeded : PasswordVerification.Success;
    }

    private static byte[] Derive(string password, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, HashSize);
}

public enum PasswordVerification { Failed, Success, SuccessRehashNeeded }
