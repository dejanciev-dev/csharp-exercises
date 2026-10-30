using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using CSharpExercises.Ciphers;

namespace CSharpExercises.Tests;

public class CiphersTests
{
    private const string English =
        "It was the best of times, it was the worst of times, it was the age of wisdom, it was the age of foolishness, " +
        "it was the epoch of belief, it was the epoch of incredulity, it was the season of Light, it was the season of " +
        "Darkness, it was the spring of hope, it was the winter of despair, we had everything before us, we had nothing " +
        "before us, we were all going direct to Heaven, we were all going direct the other way.";

    [Fact]
    public void Caesar_EncryptsAndIsCrackedByFrequencies()
    {
        Assert.Equal("Khoor, Zruog!", Caesar.Encrypt("Hello, World!", 3));
        Assert.Equal("Hello, World!", Caesar.Decrypt("Khoor, Zruog!", 3));

        var secret = Caesar.Encrypt(English, 17);
        Assert.Equal(17, Caesar.Crack(secret));
        Assert.True(Caesar.ChiSquared(English) < Caesar.ChiSquared(secret));
    }

    [Fact]
    public void Vigenere_RoundTripsAndCoincidenceDiffers()
    {
        var ciphertext = Vigenere.Encrypt("Attack at dawn!", "LEMON");

        Assert.Equal("Lxfopv ef rnhr!", ciphertext);
        Assert.Equal("Attack at dawn!", Vigenere.Decrypt(ciphertext, "lemon"));
        Assert.InRange(Vigenere.IndexOfCoincidence(English), 0.06, 0.075);
        Assert.True(Vigenere.IndexOfCoincidence(Vigenere.Encrypt(English, "cryptography")) < 0.05);
    }

    [Fact]
    public void Vigenere_IsCrackedByStatistics()
    {
        var text = string.Concat(Enumerable.Repeat(English, 3));
        var ciphertext = Vigenere.Encrypt(text, "dickens");

        Assert.Equal(7, Vigenere.GuessKeyLength(ciphertext));
        Assert.Equal("dickens", Vigenere.Crack(ciphertext));
    }

    [Fact]
    public void Rsa_TextbookExample()
    {
        var rsa = new ToyRsa(61, 53, 17); // n = 3233, d = 2753

        Assert.Equal(2790, rsa.Encrypt(65));
        Assert.Equal(65, rsa.Decrypt(2790));
        Assert.Equal(2753, ToyRsa.ModInverse(17, 3120));
        Assert.Throws<ArgumentOutOfRangeException>(() => rsa.Encrypt(5000));
    }

    [Fact]
    public void Rsa_GeneratedKeysEncryptAndSign()
    {
        var rsa = ToyRsa.Generate(256);
        var message = BigInteger.Parse("123456789012345678901234567890");

        Assert.Equal(message, rsa.Decrypt(rsa.Encrypt(message)));
        Assert.Equal(rsa.Encrypt(message), rsa.Encrypt(message)); // deterministic: one reason textbook RSA is insecure

        var document = Encoding.UTF8.GetBytes("pay Bob 10 EUR");
        var signature = rsa.Sign(document);
        Assert.True(rsa.Verify(document, signature));
        Assert.False(rsa.Verify(Encoding.UTF8.GetBytes("pay Bob 1000 EUR"), signature));
    }

    [Fact]
    public void DiffieHellman_BothSidesAgree()
    {
        var alice = new DiffieHellmanParty();
        var bob = new DiffieHellmanParty();

        Assert.Equal(alice.SharedSecret(bob.PublicKey), bob.SharedSecret(alice.PublicKey));
        Assert.Equal(alice.DeriveKey(bob.PublicKey, "chat v1"), bob.DeriveKey(alice.PublicKey, "chat v1"));
        Assert.NotEqual(alice.DeriveKey(bob.PublicKey, "chat v1"), alice.DeriveKey(bob.PublicKey, "files v1"));
        Assert.Throws<CryptographicException>(() => alice.SharedSecret(1));

        var (a, b) = DiffieHellmanParty.RealEcdh();
        Assert.Equal(a, b);
    }

    [Fact]
    public void SecretSharing_AnyThresholdOfSharesRecoversTheSecret()
    {
        var secret = SecretSharing.FromText("vault: 4-8-15-16");
        var shares = SecretSharing.Split(secret, threshold: 3, shares: 5);

        Assert.Equal("vault: 4-8-15-16", SecretSharing.ToText(SecretSharing.Combine([shares[0], shares[2], shares[4]])));
        Assert.Equal(secret, SecretSharing.Combine([shares[1], shares[3], shares[4]]));
        Assert.Equal(secret, SecretSharing.Combine(shares.ToList()));
        Assert.NotEqual(secret, SecretSharing.Combine([shares[0], shares[1]])); // two shares reveal nothing useful
    }

    [Fact]
    public void Aead_EncryptsAndDetectsTampering()
    {
        var crypto = new AuthenticatedEncryption(AuthenticatedEncryption.NewKey());

        var first = crypto.Encrypt("top secret", associatedData: "user-42");
        var second = crypto.Encrypt("top secret", associatedData: "user-42");

        Assert.NotEqual(first, second); // a fresh nonce every time
        Assert.Equal("top secret", crypto.Decrypt(first, "user-42"));
        Assert.ThrowsAny<CryptographicException>(() => crypto.Decrypt(first, "user-43")); // wrong context

        first[^1] ^= 1;
        Assert.ThrowsAny<CryptographicException>(() => crypto.Decrypt(first, "user-42"));
        Assert.ThrowsAny<CryptographicException>(() => new AuthenticatedEncryption(AuthenticatedEncryption.NewKey()).Decrypt(second, "user-42"));

        var salt = RandomNumberGenerator.GetBytes(16);
        Assert.Equal(AuthenticatedEncryption.KeyFromPassword("pw", salt, 1000), AuthenticatedEncryption.KeyFromPassword("pw", salt, 1000));
    }
}
