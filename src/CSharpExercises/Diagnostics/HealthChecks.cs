namespace CSharpExercises.Diagnostics;

public enum HealthStatus { Unhealthy, Degraded, Healthy }

public sealed record HealthResult(HealthStatus Status, string? Description = null);

public sealed record HealthReport(HealthStatus Status, IReadOnlyDictionary<string, (HealthResult Result, TimeSpan Duration)> Entries);

/// <summary>
/// Health checks for load balancers and orchestrators (Kubernetes liveness and readiness probes): each dependency gets a
/// check, all run in parallel with a timeout so one hung database can't hang the endpoint, exceptions count as
/// unhealthy, and the overall status is the worst one. Non-critical checks can only degrade the result, never fail it,
/// so a missing cache doesn't take the whole service out of rotation.
/// </summary>
public sealed class HealthChecks(TimeSpan timeout)
{
    private readonly List<(string Name, Func<CancellationToken, Task<HealthResult>> Check, bool Critical)> _checks = [];

    public HealthChecks Add(string name, Func<CancellationToken, Task<HealthResult>> check, bool critical = true)
    {
        _checks.Add((name, check, critical));
        return this;
    }

    public async Task<HealthReport> RunAsync(CancellationToken cancellationToken = default)
    {
        var results = await Task.WhenAll(_checks.Select(async c =>
        {
            var started = DateTime.UtcNow;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);
            HealthResult result;
            try
            {
                result = await c.Check(cts.Token).WaitAsync(cts.Token); // WaitAsync stops waiting even if the check ignores the token
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                result = new HealthResult(HealthStatus.Unhealthy, $"Timed out after {timeout.TotalSeconds:0.#} s.");
            }
            catch (Exception e)
            {
                result = new HealthResult(HealthStatus.Unhealthy, e.Message);
            }
            if (!c.Critical && result.Status == HealthStatus.Unhealthy) result = result with { Status = HealthStatus.Degraded };
            return (c.Name, Result: result, Duration: DateTime.UtcNow - started);
        }));

        var overall = results.Length == 0 ? HealthStatus.Healthy : results.Min(r => r.Result.Status);
        return new HealthReport(overall, results.ToDictionary(r => r.Name, r => (r.Result, r.Duration)));
    }

    /// <summary>The HTTP status a health endpoint should return: degraded still serves traffic.</summary>
    public static int HttpStatus(HealthStatus status) => status == HealthStatus.Unhealthy ? 503 : 200;
}
