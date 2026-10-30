using CSharpExercises.Diagnostics;
using Microsoft.Extensions.Time.Testing;

namespace CSharpExercises.Tests;

public class DiagnosticsTests
{
    [Fact]
    public void Logger_KeepsPropertiesAndRendersTemplates()
    {
        var logger = new StructuredLogger();

        logger.Information("{User} logged in from {Ip} in {Elapsed:0.0} ms", "ada", "10.0.0.1", 12.345);
        logger.Log(LogSeverity.Debug, "not recorded {X}", 1); // below the minimum level

        var e = Assert.Single(logger.Events);
        Assert.Equal("\"ada\" logged in from \"10.0.0.1\" in 12.3 ms", e.Message);
        Assert.Equal("ada", e.Properties["User"]);
        Assert.Equal(12.345, e.Properties["Elapsed"]);
        Assert.Equal("{User} logged in from {Ip} in {Elapsed:0.0} ms", e.Template); // groupable regardless of values
    }

    [Fact]
    public async Task Logger_ScopesFlowAcrossAwaits()
    {
        var logger = new StructuredLogger();

        using (StructuredLogger.BeginScope(("RequestId", "r-1")))
        {
            await Task.Delay(5);
            using (StructuredLogger.BeginScope(("Step", "charge"), ("RequestId", "r-1b")))
                await Task.Run(() => logger.Warning("card declined"));
            logger.Information("done");
        }
        logger.Information("outside");

        Assert.Equal("r-1b", logger.Events[0].Properties["RequestId"]); // the inner scope wins
        Assert.Equal("charge", logger.Events[0].Properties["Step"]);
        Assert.Equal("r-1", logger.Events[1].Properties["RequestId"]);
        Assert.Empty(logger.Events[2].Properties);
    }

    [Fact]
    public void Metrics_CountersGaugesAndHistograms()
    {
        var requests = new CounterMetric("http_requests_total", "Requests");
        Parallel.For(0, 1000, _ => requests.Increment());
        Assert.Equal(1000, requests.Value);
        Assert.Throws<ArgumentOutOfRangeException>(() => requests.Increment(-1));

        var queue = new GaugeMetric("queue_length", "Items waiting");
        queue.Set(5);
        queue.Set(2.5);
        Assert.Equal(2.5, queue.Value);

        var latency = new HistogramMetric("latency_seconds", "Request latency", [0.1, 0.25, 0.5, 1, 2.5]);
        foreach (var ms in Enumerable.Range(1, 1000)) latency.Observe(ms / 1000.0); // uniform 1 ms … 1 s

        Assert.Equal(1000, latency.Count);
        Assert.Equal(500.5, latency.Sum, 6);
        Assert.InRange(latency.Percentile(50), 0.49, 0.51);
        Assert.InRange(latency.Percentile(95), 0.94, 0.96);
        Assert.Contains("latency_seconds_bucket{le=\"0.25\"} 250\n", latency.Expose());
        Assert.Contains("latency_seconds_bucket{le=\"+Inf\"} 1000\n", latency.Expose());
    }

    [Fact]
    public async Task Tracing_BuildsASpanTreeAcrossAwaits()
    {
        var time = new FakeTimeProvider();
        var tracer = new Tracer(time);

        string traceId;
        using (var request = tracer.StartSpan("GET /orders/42"))
        {
            traceId = request.TraceId;
            using (tracer.StartSpan("db.query").SetAttribute("table", "orders")) time.Advance(TimeSpan.FromMilliseconds(30));
            await Task.Run(() =>
            {
                using var span = tracer.StartSpan("payment.call"); // a child even on another thread
                time.Advance(TimeSpan.FromMilliseconds(120));
                span.RecordError(new TimeoutException());
            });
        }

        Assert.Null(Tracer.Current);
        Assert.Equal("GET /orders/42 150ms\n  db.query 30ms\n  payment.call 120ms ERROR\n", tracer.Render(traceId));
        Assert.All(tracer.Finished, s => Assert.Equal(traceId, s.TraceId));
    }

    [Fact]
    public void Tracing_ContinuesARemoteTrace()
    {
        var tracer = new Tracer();
        const string incoming = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";

        using var span = tracer.StartSpan("handle", traceParent: incoming);

        Assert.Equal("4bf92f3577b34da6a3ce929d0e0e4736", span.TraceId);
        Assert.StartsWith("00-4bf92f3577b34da6a3ce929d0e0e4736-", span.TraceParent);
    }

    [Fact]
    public async Task Health_WorstStatusWinsAndTimeoutsCount()
    {
        var health = new HealthChecks(TimeSpan.FromMilliseconds(100))
            .Add("database", _ => Task.FromResult(new HealthResult(HealthStatus.Healthy)))
            .Add("cache", _ => throw new InvalidOperationException("connection refused"), critical: false)
            .Add("disk", _ => Task.FromResult(new HealthResult(HealthStatus.Degraded, "85% full")));

        var report = await health.RunAsync();
        Assert.Equal(HealthStatus.Degraded, report.Status);  // the cache failure only degrades
        Assert.Equal("connection refused", report.Entries["cache"].Result.Description);
        Assert.Equal(200, HealthChecks.HttpStatus(report.Status));

        var hung = await new HealthChecks(TimeSpan.FromMilliseconds(50))
            .Add("payments", async _ => { await Task.Delay(Timeout.Infinite); return new HealthResult(HealthStatus.Healthy); }) // ignores the token
            .RunAsync()
            .WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(HealthStatus.Unhealthy, hung.Status);
        Assert.Contains("Timed out", hung.Entries["payments"].Result.Description);
        Assert.Equal(503, HealthChecks.HttpStatus(hung.Status));
    }

    [Fact]
    public async Task Correlation_FlowsDownButNotBackUp()
    {
        string? inChild = null, afterChildChanged = null;

        var result = await CorrelationContext.RunAsync(async () =>
        {
            var id = CorrelationContext.Id;
            await Task.Delay(5);
            await Task.Run(() =>
            {
                inChild = CorrelationContext.Id;
                CorrelationContext.Begin("child-only"); // changes the child's copy only
            });
            afterChildChanged = CorrelationContext.Id;
            return id;
        }, id: "req-7");

        Assert.Equal("req-7", result);
        Assert.Equal("req-7", inChild);
        Assert.Equal("req-7", afterChildChanged);
        Assert.Null(CorrelationContext.Id); // restored outside

        string? detached = "unset";
        using (CorrelationContext.Begin("req-8")) await CorrelationContext.RunDetached(() => { detached = CorrelationContext.Id; return Task.CompletedTask; });
        Assert.Null(detached);
    }

    [Fact]
    public void Deduplicator_CollapsesRepeats()
    {
        var time = new FakeTimeProvider();
        var lines = new List<string>();
        var log = new LogDeduplicator(lines.Add, TimeSpan.FromMinutes(1), time);

        for (var i = 0; i < 500; i++) log.Write("db timeout");
        log.Write("db recovered");
        log.Write("tick");
        time.Advance(TimeSpan.FromMinutes(2));
        log.Write("tick"); // outside the window: written again
        log.Write("tick");
        log.Flush();

        Assert.Equal(["db timeout", "last message repeated 499 times", "db recovered", "tick", "tick", "last message repeated 1 time"], lines);
    }
}
