using CSharpExercises.Testing;

namespace CSharpExercises.Tests.Testing;

// Hand-written test doubles, one of each kind (names from Gerard Meszaros' xUnit Test Patterns):
//
//   Dummy - passed in only to satisfy a parameter; using it is a test failure.
//   Stub  - returns canned answers so the code under test takes a particular path.
//   Spy   - records how it was called, so the test can check afterwards.
//   Fake  - a real, working implementation that takes a shortcut (in memory instead of a database).
//   Mock  - is told up front what calls to expect; usually made with a library (see RenewalServiceMockTests).

/// <summary>Dummy: for tests where no notification should ever be sent.</summary>
public sealed class DummyNotifier : INotifier
{
    public void Notify(string email, string subject, string body) =>
        throw new InvalidOperationException($"No notification was expected, but '{subject}' was sent to {email}.");
}

/// <summary>Stub, plus a little spying: answers every charge the same way and counts the calls.</summary>
public sealed class StubPaymentGateway : IPaymentGateway
{
    private readonly Func<PaymentResult> _respond;

    private StubPaymentGateway(Func<PaymentResult> respond) => _respond = respond;

    public static StubPaymentGateway Approving() => new(PaymentResult.Success);

    public static StubPaymentGateway Declining(string reason) => new(() => PaymentResult.Declined(reason));

    public static StubPaymentGateway Throwing(Exception exception) => new(() => throw exception);

    public List<(string Email, decimal Amount)> Charges { get; } = [];

    public PaymentResult Charge(string customerEmail, decimal amount)
    {
        Charges.Add((customerEmail, amount));
        return _respond();
    }
}

/// <summary>Spy: remembers every notification.</summary>
public sealed class SpyNotifier : INotifier
{
    public List<(string Email, string Subject, string Body)> Sent { get; } = [];

    public void Notify(string email, string subject, string body) => Sent.Add((email, subject, body));
}

/// <summary>Fake: a fully working repository that keeps data in a dictionary.</summary>
public sealed class InMemorySubscriptionRepository : ISubscriptionRepository
{
    private readonly Dictionary<Guid, Subscription> _items = new();

    public InMemorySubscriptionRepository(params Subscription[] subscriptions)
    {
        foreach (var subscription in subscriptions) Save(subscription);
    }

    public Subscription this[Guid id] => _items[id];

    public IReadOnlyList<Subscription> FindExpiringBefore(DateTimeOffset cutoff) =>
        _items.Values.Where(s => s.ExpiresAt < cutoff).ToList();

    public void Save(Subscription subscription) => _items[subscription.Id] = subscription;
}
