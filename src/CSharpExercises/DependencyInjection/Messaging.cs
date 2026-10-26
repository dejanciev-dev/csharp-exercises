using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CSharpExercises.DependencyInjection;

/// <summary>
/// A small messaging feature wired up with Microsoft.Extensions.DependencyInjection. Every class asks for what it
/// needs through its constructor and depends on interfaces, so tests can swap in a fixed clock or a fake sender.
/// </summary>
public record SentMessage(string Channel, string To, string Text);

/// <summary>Collects sent messages instead of really delivering them.</summary>
public interface IOutbox
{
    IReadOnlyList<SentMessage> Messages { get; }

    void Add(SentMessage message);
}

public sealed class Outbox : IOutbox
{
    private readonly List<SentMessage> _messages = [];
    private readonly Lock _lock = new();

    public IReadOnlyList<SentMessage> Messages
    {
        get
        {
            lock (_lock) return _messages.ToArray();
        }
    }

    public void Add(SentMessage message)
    {
        lock (_lock) _messages.Add(message);
    }
}

public interface IMessageSender
{
    void Send(string to, string text);
}

public sealed class EmailSender(IOutbox outbox) : IMessageSender
{
    public void Send(string to, string text) => outbox.Add(new SentMessage("email", to, text));
}

public sealed class SmsSender(IOutbox outbox) : IMessageSender
{
    public const int MaxLength = 160;

    public void Send(string to, string text) =>
        outbox.Add(new SentMessage("sms", to, text.Length <= MaxLength ? text : text[..MaxLength]));
}

/// <summary>Registered as scoped: one per request, shared by everything resolved within that request.</summary>
public sealed class RequestContext
{
    public Guid RequestId { get; } = Guid.NewGuid();
}

/// <summary>Greets by time of day. Asking for <see cref="TimeProvider"/> instead of reading DateTime.Now makes it testable.</summary>
public sealed class WelcomeService(IMessageSender sender, TimeProvider time)
{
    public string Welcome(string name, string address)
    {
        var greeting = time.GetLocalNow().Hour switch
        {
            < 12 => "Good morning",
            < 18 => "Good afternoon",
            _ => "Good evening"
        };

        var text = $"{greeting}, {name}!";
        sender.Send(address, text);
        return text;
    }
}

/// <summary>Uses a keyed service to get the SMS sender specifically, rather than the default email one.</summary>
public sealed class TwoFactorService([FromKeyedServices("sms")] IMessageSender sms)
{
    public string SendCode(string phone)
    {
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        sms.Send(phone, $"Your verification code is {code}");
        return code;
    }
}

public static class MessagingServiceCollectionExtensions
{
    /// <summary>Registers the whole feature with one call, the convention used across ASP.NET Core (AddMvc, AddLogging, ...).</summary>
    public static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        // TryAdd: keep any TimeProvider the app (or a test) registered before calling AddMessaging.
        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<IOutbox, Outbox>();
        services.AddKeyedTransient<IMessageSender, EmailSender>("email");
        services.AddKeyedTransient<IMessageSender, SmsSender>("sms");
        services.AddTransient<IMessageSender>(sp => sp.GetRequiredKeyedService<IMessageSender>("email"));
        services.AddScoped<RequestContext>();
        services.AddTransient<WelcomeService>();
        services.AddTransient<TwoFactorService>();
        return services;
    }
}
