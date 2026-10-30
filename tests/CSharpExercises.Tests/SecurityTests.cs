using System.Text;
using CSharpExercises.Security;
using Microsoft.Extensions.Time.Testing;

namespace CSharpExercises.Tests;

public class SecurityTests
{
    [Fact]
    public void PasswordHasher_SaltsVerifiesAndUpgrades()
    {
        var hasher = new PasswordHasher(iterations: 10_000);

        var first = hasher.Hash("correct horse battery staple");
        var second = hasher.Hash("correct horse battery staple");

        Assert.NotEqual(first, second); // different salts
        Assert.StartsWith("pbkdf2-sha256$10000$", first);
        Assert.Equal(PasswordVerification.Success, hasher.Verify("correct horse battery staple", first));
        Assert.Equal(PasswordVerification.Failed, hasher.Verify("Correct horse battery staple", first));
        Assert.Equal(PasswordVerification.Failed, hasher.Verify("x", "md5$abc"));

        var stronger = new PasswordHasher(iterations: 20_000);
        Assert.Equal(PasswordVerification.SuccessRehashNeeded, stronger.Verify("correct horse battery staple", first));
    }

    private static readonly byte[] Key = Encoding.UTF8.GetBytes("a-256-bit-secret-for-tests-only!!");

    [Fact]
    public void Jwt_RoundTripsClaims()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero));
        var tokens = new SignedToken(Key, time);

        var token = tokens.Create(new Dictionary<string, object> { ["sub"] = "user-42", ["role"] = "admin" }, TimeSpan.FromMinutes(15));
        var claims = tokens.Validate(token);

        Assert.Equal(3, token.Split('.').Length);
        Assert.Equal("user-42", (string?)claims["sub"]);
        Assert.Equal(time.GetUtcNow().AddMinutes(15).ToUnixTimeSeconds(), (long)claims["exp"]!);
        Assert.DoesNotContain('=', token); // base64url has no padding
    }

    [Fact]
    public void Jwt_RejectsTamperingExpiryAndAlgNone()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero));
        var tokens = new SignedToken(Key, time);
        var token = tokens.Create(new Dictionary<string, object> { ["role"] = "user" }, TimeSpan.FromMinutes(5));
        var parts = token.Split('.');

        // Change the role in the payload without re-signing.
        var forgedPayload = SignedToken.Base64Url(Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(SignedToken.FromBase64Url(parts[1])).Replace("user", "admin")));
        Assert.Throws<SecurityTokenException>(() => tokens.Validate($"{parts[0]}.{forgedPayload}.{parts[2]}"));

        var noneHeader = SignedToken.Base64Url("""{"alg":"none","typ":"JWT"}"""u8.ToArray());
        Assert.Contains("not accepted", Assert.Throws<SecurityTokenException>(() => tokens.Validate($"{noneHeader}.{parts[1]}.")).Message);

        Assert.Throws<SecurityTokenException>(() => new SignedToken(Encoding.UTF8.GetBytes("another key entirely, also long"), time).Validate(token));

        time.Advance(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(20));
        tokens.Validate(token); // within the 30 s clock-skew allowance
        time.Advance(TimeSpan.FromSeconds(20));
        Assert.Contains("expired", Assert.Throws<SecurityTokenException>(() => tokens.Validate(token)).Message);
    }

    [Theory]
    [InlineData(59L, "94287082")]
    [InlineData(1111111109L, "07081804")]
    [InlineData(1234567890L, "89005924")]
    [InlineData(2000000000L, "69279037")]
    public void Totp_MatchesRfc6238TestVectors(long unixSeconds, string expected)
    {
        var time = new FakeTimeProvider(DateTimeOffset.FromUnixTimeSeconds(unixSeconds));
        var totp = new Totp(Encoding.ASCII.GetBytes("12345678901234567890"), time, digits: 8);

        Assert.Equal(expected, totp.Generate());
    }

    [Fact]
    public void Totp_AllowsDriftButNotReplay()
    {
        var time = new FakeTimeProvider(DateTimeOffset.FromUnixTimeSeconds(1_700_000_000));
        var secret = Encoding.ASCII.GetBytes("12345678901234567890");
        var phone = new Totp(secret, time);
        var server = new Totp(secret, time);

        var code = phone.Generate();
        time.Advance(TimeSpan.FromSeconds(30)); // the user was slow

        Assert.True(server.Verify(code));
        Assert.False(server.Verify(code)); // replay
        time.Advance(TimeSpan.FromMinutes(5));
        Assert.False(new Totp(secret, time).Verify(code)); // far too old

        Assert.Equal("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ", Totp.Base32NoPadding(secret));
        Assert.StartsWith("otpauth://totp/Acme:ada%40example.com?secret=GEZD", phone.ProvisioningUri("Acme", "ada@example.com"));
    }

    [Theory]
    [InlineData("password", Strength.VeryWeak)]
    [InlineData("P@ssw0rd", Strength.VeryWeak)]   // leetspeak doesn't fool anyone
    [InlineData("aaaaaaaaaaaa", Strength.VeryWeak)]
    [InlineData("abcdef123456", Strength.VeryWeak)]
    [InlineData("Tr0ub4dor&3", Strength.Strong)]       // a limit of naive entropy: xkcd 936 puts this dictionary word at ~28 bits
    [InlineData("correct horse battery staple", Strength.VeryStrong)]
    public void PasswordStrength_Rates(string password, Strength expected) =>
        Assert.Equal(expected, PasswordStrength.Evaluate(password).Strength);

    [Fact]
    public void PasswordStrength_ExplainsProblems()
    {
        var report = PasswordStrength.Evaluate("ada1815", userInputs: ["Ada", "ada@example.com"]);

        Assert.Contains("Use at least 8 characters.", report.Warnings);
        Assert.Contains("Don't include 'Ada'.", report.Warnings);
    }

    [Theory]
    [InlineData("<b>bold</b> and <i>italic</i>", "<b>bold</b> and <i>italic</i>")]
    [InlineData("<script>alert(1)</script>hello", "hello")]
    [InlineData("<img src=x onerror=alert(1)>text", "text")]
    [InlineData("<p onclick=\"steal()\">hi</p>", "<p>hi</p>")]
    [InlineData("<a href=\"javascript:alert(1)\">x</a>", "<a rel=\"nofollow noopener\">x</a>")]
    [InlineData("<a href=\"java\tscript:alert(1)\">x</a>", "<a rel=\"nofollow noopener\">x</a>")]
    [InlineData("<a href='https://example.com/?a=1&b=2' target=_blank>link</a>", "<a href=\"https://example.com/?a=1&amp;b=2\" rel=\"nofollow noopener\">link</a>")]
    [InlineData("5 < 6 & \"quotes\"", "5 &lt; 6 &amp; &quot;quotes&quot;")]
    [InlineData("<STYLE>body{}</STYLE><B>x</B>", "<b>x</b>")]
    public void HtmlSanitizer_KeepsOnlyTheAllowlist(string input, string expected) =>
        Assert.Equal(expected, HtmlSanitizer.Sanitize(input));

    [Theory]
    [InlineData("/docs/page", true)]
    [InlineData("page?x=a:b", true)]
    [InlineData("mailto:ada@example.com", true)]
    [InlineData("JAVASCRIPT:alert(1)", false)]
    [InlineData("data:text/html;base64,PHNjcmlwdD4=", false)]
    public void HtmlSanitizer_ChecksUrlSchemes(string url, bool safe) => Assert.Equal(safe, HtmlSanitizer.IsSafeUrl(url));
}
