using CSharpExercises.Testing;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using static CSharpExercises.Tests.Testing.SubscriptionBuilder;

namespace CSharpExercises.Tests.Testing;

/// <summary>
/// The same service tested with NSubstitute, which creates doubles at runtime instead of by hand.
/// Mocks shine for checking interactions that leave no other trace, like which cutoff date was asked for.
/// Use them sparingly: tests that verify every call break whenever the implementation changes,
/// even if the behavior didn't.
/// </summary>
public class RenewalServiceMockTests
{
    private readonly ISubscriptionRepository _repository = Substitute.For<ISubscriptionRepository>();
    private readonly IPaymentGateway _payments = Substitute.For<IPaymentGateway>();
    private readonly INotifier _notifier = Substitute.For<INotifier>();
    private readonly RenewalService _service;

    public RenewalServiceMockTests()
    {
        _service = new RenewalService(_repository, _payments, _notifier, new FakeTimeProvider(Now));
    }

    private void RepositoryReturns(params Subscription[] subscriptions) =>
        _repository.FindExpiringBefore(Arg.Any<DateTimeOffset>()).Returns(subscriptions);

    [Fact]
    public void AsksRepositoryForThreeDayWindow()
    {
        RepositoryReturns();

        _service.RenewDue();

        _repository.Received(1).FindExpiringBefore(Now + TimeSpan.FromDays(3));
    }

    [Fact]
    public void Renewal_SavesAndNotifies()
    {
        RepositoryReturns(ASubscription().ForCustomer("ana@example.com").OnPlan("Pro", 10m));
        _payments.Charge("ana@example.com", 10m).Returns(PaymentResult.Success());

        _service.RenewDue();

        _repository.Received(1).Save(Arg.Is<Subscription>(s => s.ExpiresAt == Now.AddDays(1).AddMonths(1)));
        _notifier.Received(1).Notify("ana@example.com", "Subscription renewed", Arg.Is<string>(body => body.Contains("Pro")));
    }

    [Fact]
    public void Decline_SavesPastDue()
    {
        RepositoryReturns(ASubscription());
        _payments.Charge(default!, default).ReturnsForAnyArgs(PaymentResult.Declined("insufficient funds"));

        _service.RenewDue();

        _repository.Received(1).Save(Arg.Is<Subscription>(s => s.Status == SubscriptionStatus.PastDue));
        _notifier.Received(1).Notify(Arg.Any<string>(), "Payment failed", Arg.Is<string>(b => b.Contains("insufficient funds")));
    }

    [Fact]
    public void Outage_SavesNothingAndSendsNothing()
    {
        RepositoryReturns(ASubscription());
        _payments.Charge(default!, default).ThrowsForAnyArgs(new TimeoutException());

        var report = _service.RenewDue();

        Assert.Equal(1, report.Errors);
        _repository.DidNotReceiveWithAnyArgs().Save(default!);
        _notifier.DidNotReceiveWithAnyArgs().Notify(default!, default!, default!);
    }

    [Fact]
    public void Skipped_IsNeverCharged()
    {
        RepositoryReturns(ASubscription().WithoutAutoRenew());

        _service.RenewDue();

        _payments.DidNotReceiveWithAnyArgs().Charge(default!, default);
    }
}
