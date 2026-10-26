using System.Net.Mail;

namespace CSharpExercises.Patterns;

/// <summary>An immutable email; build one with <see cref="EmailBuilder"/>.</summary>
public record Email(
    string From,
    IReadOnlyList<string> To,
    IReadOnlyList<string> Cc,
    string Subject,
    string Body,
    IReadOnlyList<string> Attachments);

/// <summary>
/// Builder: assembles a complex object step by step with a fluent API, validates it once in
/// <see cref="Build"/>, and hands back an immutable result instead of a constructor with six parameters.
/// </summary>
public class EmailBuilder
{
    private readonly List<string> _to = [];
    private readonly List<string> _cc = [];
    private readonly List<string> _attachments = [];
    private string? _from;
    private string _subject = "";
    private string _body = "";

    public EmailBuilder From(string address)
    {
        _from = ValidateAddress(address);
        return this;
    }

    public EmailBuilder To(params string[] addresses)
    {
        _to.AddRange(addresses.Select(ValidateAddress));
        return this;
    }

    public EmailBuilder Cc(params string[] addresses)
    {
        _cc.AddRange(addresses.Select(ValidateAddress));
        return this;
    }

    public EmailBuilder Subject(string subject)
    {
        _subject = subject;
        return this;
    }

    public EmailBuilder Body(string body)
    {
        _body = body;
        return this;
    }

    public EmailBuilder Attach(string fileName)
    {
        _attachments.Add(fileName);
        return this;
    }

    public Email Build()
    {
        if (_from is null) throw new InvalidOperationException("A sender is required.");
        if (_to.Count == 0) throw new InvalidOperationException("At least one recipient is required.");
        if (string.IsNullOrWhiteSpace(_subject)) throw new InvalidOperationException("A subject is required.");

        // Copy the lists so later builder calls can't change an email that was already built.
        return new Email(_from, _to.ToArray(), _cc.ToArray(), _subject, _body, _attachments.ToArray());
    }

    private static string ValidateAddress(string address) =>
        MailAddress.TryCreate(address, out var parsed)
            ? parsed.Address
            : throw new ArgumentException($"Invalid email address: '{address}'.", nameof(address));
}
