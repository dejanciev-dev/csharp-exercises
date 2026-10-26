using CSharpExercises.Testing;

namespace CSharpExercises.Tests.Testing;

/// <summary>
/// Test data builder: every field has a sensible default, so each test sets only what matters to it.
/// <c>ASubscription().ExpiringIn(TimeSpan.FromDays(2)).WithoutAutoRenew()</c> says more than a
/// seven-argument constructor call, and adding a field to <see cref="Subscription"/> only changes this class.
/// </summary>
public sealed class SubscriptionBuilder
{
    /// <summary>The "current time" used across these tests; pass it to a FakeTimeProvider too.</summary>
    public static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    private Guid _id = Guid.NewGuid();
    private string _email = "customer@example.com";
    private string _plan = "Pro";
    private decimal _price = 10m;
    private DateTimeOffset _expiresAt = Now.AddDays(1);
    private bool _autoRenew = true;
    private SubscriptionStatus _status = SubscriptionStatus.Active;

    public static SubscriptionBuilder ASubscription() => new();

    public SubscriptionBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public SubscriptionBuilder ForCustomer(string email)
    {
        _email = email;
        return this;
    }

    public SubscriptionBuilder OnPlan(string plan, decimal monthlyPrice)
    {
        _plan = plan;
        _price = monthlyPrice;
        return this;
    }

    public SubscriptionBuilder ExpiringIn(TimeSpan fromNow)
    {
        _expiresAt = Now + fromNow;
        return this;
    }

    public SubscriptionBuilder ExpiredAgo(TimeSpan beforeNow)
    {
        _expiresAt = Now - beforeNow;
        return this;
    }

    public SubscriptionBuilder WithoutAutoRenew()
    {
        _autoRenew = false;
        return this;
    }

    public SubscriptionBuilder WithStatus(SubscriptionStatus status)
    {
        _status = status;
        return this;
    }

    public Subscription Build() => new(_id, _email, _plan, _price, _expiresAt, _autoRenew, _status);

    /// <summary>Lets a builder be passed wherever a Subscription is expected, without calling Build().</summary>
    public static implicit operator Subscription(SubscriptionBuilder builder) => builder.Build();
}
