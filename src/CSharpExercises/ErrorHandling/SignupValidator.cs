namespace CSharpExercises.ErrorHandling;

public sealed record Signup(string Email, int Age, string Password);

/// <summary>
/// Validates a signup form with <see cref="Result{T}"/>, in two styles: stop at the first error, or report them all.
/// </summary>
public static class SignupValidator
{
    public static Result<string> ParseEmail(string? input)
    {
        var email = input?.Trim() ?? "";
        if (email.Length == 0) return new Error("email.required", "Email is required.");

        var at = email.IndexOf('@');
        return at > 0 && at == email.LastIndexOf('@') && email.IndexOf('.', at) > at + 1 && !email.EndsWith('.')
            ? email.ToLowerInvariant()
            : new Error("email.invalid", $"'{email}' is not a valid email address.");
    }

    public static Result<int> ParseAge(string? input) =>
        Result.Try(() => int.Parse(input ?? ""), _ => new Error("age.invalid", $"'{input}' is not a whole number."))
              .Ensure(age => age is >= 13 and <= 130, new Error("age.range", "Age must be between 13 and 130."));

    public static Result<string> ParsePassword(string? input) =>
        Result<string>.Success(input ?? "")
            .Ensure(p => p.Length >= 8, new Error("password.short", "Password must be at least 8 characters."))
            .Ensure(p => p.Any(char.IsDigit), new Error("password.digit", "Password must contain a digit."));

    /// <summary>Stops at the first invalid field, written with LINQ query syntax over <see cref="Result{T}"/>.</summary>
    public static Result<Signup> ValidateFailFast(string? email, string? age, string? password) =>
        (from e in ParseEmail(email)
         from a in ParseAge(age)
         from p in ParsePassword(password)
         select new Signup(e, a, p))
        .Ensure(NotContainingEmailName, PasswordContainsEmailName);

    /// <summary>Checks every field and returns all the errors at once.</summary>
    public static Result<Signup> ValidateAll(string? email, string? age, string? password) =>
        Result.Combine(ParseEmail(email), ParseAge(age), ParsePassword(password), (e, a, p) => new Signup(e, a, p))
              .Ensure(NotContainingEmailName, PasswordContainsEmailName);

    // A rule across fields only makes sense once each field is valid on its own.
    private static bool NotContainingEmailName(Signup signup) =>
        !signup.Password.Contains(signup.Email[..signup.Email.IndexOf('@')], StringComparison.OrdinalIgnoreCase);

    private static readonly Error PasswordContainsEmailName =
        new("password.contains_email", "Password must not contain the name from your email address.");
}
