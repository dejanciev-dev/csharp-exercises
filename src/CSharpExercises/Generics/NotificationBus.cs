using System.Collections.Concurrent;
using System.Reflection;

namespace CSharpExercises.Generics;

public abstract record Notification(string Recipient);
public sealed record EmailNotification(string Recipient, string Subject) : Notification(Recipient);
public record SmsNotification(string Recipient, string Text) : Notification(Recipient);
public sealed record UrgentSmsNotification(string Recipient, string Text) : SmsNotification(Recipient, Text);

/// <summary>
/// <c>in T</c> makes the interface contravariant: a handler of <see cref="Notification"/> can handle <em>any</em>
/// notification, so it can be used wherever an <c>INotificationHandler&lt;EmailNotification&gt;</c> is expected.
/// </summary>
public interface INotificationHandler<in T> where T : Notification
{
    void Handle(T notification);
}

/// <summary>
/// Delivers each notification to every handler that can take it: handlers registered for its exact type and,
/// thanks to contravariance, handlers registered for any of its base types.
/// </summary>
public sealed class NotificationBus
{
    private static readonly MethodInfo PublishCoreMethod =
        typeof(NotificationBus).GetMethod(nameof(PublishCore), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly ConcurrentDictionary<Type, Func<NotificationBus, Notification, int>> Dispatchers = new();

    private readonly List<object> _handlers = [];

    public void Register<T>(INotificationHandler<T> handler) where T : Notification => _handlers.Add(handler);

    /// <summary>
    /// Dispatches on the notification's runtime type. A purely generic <c>Publish&lt;T&gt;</c> would use the
    /// compile-time type, so publishing an email typed as <see cref="Notification"/> would miss the email handlers.
    /// Returns the number of handlers that received it.
    /// </summary>
    public int Publish(Notification notification)
    {
        // Build PublishCore<TheRuntimeType> once per type with reflection, then call it as a normal delegate.
        var dispatch = Dispatchers.GetOrAdd(notification.GetType(), type =>
            PublishCoreMethod.MakeGenericMethod(type).CreateDelegate<Func<NotificationBus, Notification, int>>());

        return dispatch(this, notification);
    }

    private int PublishCore<T>(Notification notification) where T : Notification
    {
        // OfType performs a runtime cast, and that cast honours variance:
        // an INotificationHandler<Notification> *is* an INotificationHandler<UrgentSmsNotification>.
        var handlers = _handlers.OfType<INotificationHandler<T>>().ToList();
        foreach (var handler in handlers) handler.Handle((T)notification);
        return handlers.Count;
    }
}

/// <summary>Covariance (<c>out T</c>) in the framework types: a sequence of a derived type is a sequence of its base type.</summary>
public static class Variance
{
    public static IReadOnlyList<string> Recipients(IEnumerable<Notification> notifications) =>
        [.. notifications.Select(n => n.Recipient).Distinct()];

    /// <summary>Works for a <c>List&lt;EmailNotification&gt;</c> because <see cref="IReadOnlyList{T}"/> is covariant.</summary>
    public static Notification? Latest(IReadOnlyList<Notification> notifications) =>
        notifications.Count == 0 ? null : notifications[^1];
}
