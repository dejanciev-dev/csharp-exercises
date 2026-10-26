using System.Globalization;

namespace CSharpExercises.Testing;

// Production code designed to be tested: every outside dependency (storage, payments, email, the clock)
// comes in through the constructor as an interface or TimeProvider, so tests can replace it.
// The tests in tests/CSharpExercises.Tests/Testing show the techniques.

public enum SubscriptionStatus
{
    Active,
    PastDue,
    Cancelled
}

public sealed record Subscription(
    Guid Id,
    string CustomerEmail,
    string Plan,
    decimal MonthlyPrice,
    DateTimeOffset ExpiresAt,
    bool AutoRenew,
    SubscriptionStatus Status);

public interface ISubscriptionRepository
{
    IReadOnlyList<Subscription> FindExpiringBefore(DateTimeOffset cutoff);

    void Save(Subscription subscription);
}

public sealed record PaymentResult(bool Succeeded, string? Error = null)
{
    public static PaymentResult Success() => new(true);

    public static PaymentResult Declined(string reason) => new(false, reason);
}

public interface IPaymentGateway
{
    /// <summary>Returns a declined result for card problems; throws when the gateway itself is unreachable.</summary>
    PaymentResult Charge(string customerEmail, decimal amount);
}

public interface INotifier
{
    void Notify(string email, string subject, string body);
}

public sealed record RenewalReport(int Renewed, int Declined, int Errors, int Skipped);

/// <summary>Charges and extends every auto-renewing subscription that expires within the next few days.</summary>
public sealed class RenewalService(
    ISubscriptionRepository repository,
    IPaymentGateway payments,
    INotifier notifier,
    TimeProvider time)
{
    public static readonly TimeSpan RenewalWindow = TimeSpan.FromDays(3);

    public RenewalReport RenewDue()
    {
        var now = time.GetUtcNow();
        int renewed = 0, declined = 0, errors = 0, skipped = 0;

        foreach (var subscription in repository.FindExpiringBefore(now + RenewalWindow))
        {
            if (subscription.Status != SubscriptionStatus.Active || !subscription.AutoRenew)
            {
                skipped++;
                continue;
            }

            PaymentResult result;
            try
            {
                result = payments.Charge(subscription.CustomerEmail, subscription.MonthlyPrice);
            }
            catch (Exception ex) when (ex is HttpRequestException or TimeoutException)
            {
                // Gateway outage: leave the subscription untouched so the next run tries again.
                errors++;
                continue;
            }

            if (result.Succeeded)
            {
                // Extend from the current expiry so renewing early doesn't lose days,
                // but never from a date in the past.
                var from = subscription.ExpiresAt > now ? subscription.ExpiresAt : now;
                var extended = subscription with { ExpiresAt = from.AddMonths(1) };

                repository.Save(extended);
                notifier.Notify(
                    subscription.CustomerEmail,
                    "Subscription renewed",
                    $"Your {subscription.Plan} plan is renewed until {extended.ExpiresAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.");
                renewed++;
            }
            else
            {
                repository.Save(subscription with { Status = SubscriptionStatus.PastDue });
                notifier.Notify(
                    subscription.CustomerEmail,
                    "Payment failed",
                    $"We couldn't renew your {subscription.Plan} plan: {result.Error}. Please update your card.");
                declined++;
            }
        }

        return new RenewalReport(renewed, declined, errors, skipped);
    }
}
