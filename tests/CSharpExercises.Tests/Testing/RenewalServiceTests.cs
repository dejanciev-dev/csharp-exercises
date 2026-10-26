using CSharpExercises.Testing;
using Microsoft.Extensions.Time.Testing;
using static CSharpExercises.Tests.Testing.SubscriptionBuilder;

namespace CSharpExercises.Tests.Testing;

/// <summary>
/// RenewalService tested with the hand-written doubles from TestDoubles.cs.
/// These tests check outcomes (what ended up in the repository, what was sent) rather than exact calls,
/// so they keep passing if the service is refactored without changing its behavior.
/// </summary>
public class RenewalServiceTests
{
    private readonly FakeTimeProvider _time = new(Now);

    private RenewalService CreateService(ISubscriptionRepository repository, IPaymentGateway payments, INotifier notifier) =>
        new(repository, payments, notifier, _time);

    [Fact]
    public void Renews_ChargesExtendsAndSendsReceipt()
    {
        Subscription subscription = ASubscription().ForCustomer("ana@example.com").OnPlan("Team", 49m).ExpiringIn(TimeSpan.FromDays(2));
        var repository = new InMemorySubscriptionRepository(subscription);
        var payments = StubPaymentGateway.Approving();
        var notifier = new SpyNotifier();

        var report = CreateService(repository, payments, notifier).RenewDue();

        Assert.Equal(new RenewalReport(Renewed: 1, Declined: 0, Errors: 0, Skipped: 0), report);
        Assert.Equal([("ana@example.com", 49m)], payments.Charges);
        Assert.Equal(new DateTimeOffset(2026, 2, 17, 9, 0, 0, TimeSpan.Zero), repository[subscription.Id].ExpiresAt);

        var (email, subject, body) = Assert.Single(notifier.Sent);
        Assert.Equal("ana@example.com", email);
        Assert.Equal("Subscription renewed", subject);
        Assert.Contains("until 2026-02-17", body);
    }

    [Fact]
    public void Renews_LapsedSubscriptionFromToday_NotFromOldExpiry()
    {
        Subscription lapsed = ASubscription().ExpiredAgo(TimeSpan.FromDays(60));
        var repository = new InMemorySubscriptionRepository(lapsed);

        CreateService(repository, StubPaymentGateway.Approving(), new SpyNotifier()).RenewDue();

        Assert.Equal(Now.AddMonths(1), repository[lapsed.Id].ExpiresAt);
    }

    [Fact]
    public void Declined_MarksPastDueAndWarnsCustomer()
    {
        Subscription subscription = ASubscription();
        var repository = new InMemorySubscriptionRepository(subscription);
        var notifier = new SpyNotifier();

        var report = CreateService(repository, StubPaymentGateway.Declining("card expired"), notifier).RenewDue();

        Assert.Equal(1, report.Declined);
        Assert.Equal(SubscriptionStatus.PastDue, repository[subscription.Id].Status);
        Assert.Equal(subscription.ExpiresAt, repository[subscription.Id].ExpiresAt);
        Assert.Contains("card expired", Assert.Single(notifier.Sent).Body);
    }

    [Fact]
    public void GatewayOutage_LeavesSubscriptionForNextRun()
    {
        Subscription subscription = ASubscription();
        var repository = new InMemorySubscriptionRepository(subscription);

        // DummyNotifier: an outage must not send the customer anything.
        var report = CreateService(repository, StubPaymentGateway.Throwing(new HttpRequestException("503")), new DummyNotifier()).RenewDue();

        Assert.Equal(1, report.Errors);
        Assert.Equal(subscription, repository[subscription.Id]);

        // The gateway is back an hour later.
        _time.Advance(TimeSpan.FromHours(1));
        var retry = CreateService(repository, StubPaymentGateway.Approving(), new SpyNotifier()).RenewDue();

        Assert.Equal(1, retry.Renewed);
    }

    [Fact]
    public void UnexpectedErrors_AreNotSwallowed()
    {
        var repository = new InMemorySubscriptionRepository(ASubscription());
        var payments = StubPaymentGateway.Throwing(new InvalidOperationException("bug"));

        Assert.Throws<InvalidOperationException>(() => CreateService(repository, payments, new DummyNotifier()).RenewDue());
    }

    [Fact]
    public void Skips_SubscriptionsThatShouldNotRenew()
    {
        var repository = new InMemorySubscriptionRepository(
            ASubscription().WithoutAutoRenew(),
            ASubscription().WithStatus(SubscriptionStatus.PastDue),
            ASubscription().WithStatus(SubscriptionStatus.Cancelled),
            ASubscription().ExpiringIn(TimeSpan.FromDays(10))); // not due yet: not even returned by the repository
        var payments = StubPaymentGateway.Approving();

        var report = CreateService(repository, payments, new DummyNotifier()).RenewDue();

        Assert.Equal(new RenewalReport(0, 0, 0, Skipped: 3), report);
        Assert.Empty(payments.Charges);
    }

    [Fact]
    public void RenewalWindow_MovesWithTheClock()
    {
        Subscription inFiveDays = ASubscription().ExpiringIn(TimeSpan.FromDays(5));
        var repository = new InMemorySubscriptionRepository(inFiveDays);
        var payments = StubPaymentGateway.Approving();
        var service = CreateService(repository, payments, new SpyNotifier());

        Assert.Equal(0, service.RenewDue().Renewed);

        _time.Advance(TimeSpan.FromDays(2) + TimeSpan.FromMinutes(1)); // now inside the 3-day window
        Assert.Equal(1, service.RenewDue().Renewed);
    }
}
