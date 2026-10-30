using System.Text;

namespace CSharpExercises.Diagnostics;

public sealed class Span : IDisposable
{
    private readonly Tracer _tracer;
    private readonly long _start;

    internal Span(Tracer tracer, string name, string traceId, Span? parent)
    {
        (_tracer, Name, TraceId, Parent) = (tracer, name, traceId, parent);
        SpanId = Guid.NewGuid().ToString("N")[..16];
        _start = tracer.Time.GetTimestamp();
    }

    public string Name { get; }
    public string TraceId { get; }
    public string SpanId { get; }
    public Span? Parent { get; }
    public TimeSpan Duration { get; private set; }
    public Dictionary<string, object> Attributes { get; } = [];
    public string Status { get; private set; } = "ok";

    public Span SetAttribute(string key, object value)
    {
        Attributes[key] = value;
        return this;
    }

    public void RecordError(Exception e)
    {
        Status = "error";
        Attributes["exception"] = e.GetType().Name;
    }

    public void Dispose() => _tracer.End(this, _tracer.Time.GetElapsedTime(_start));

    internal void SetDuration(TimeSpan duration) => Duration = duration;

    /// <summary>The W3C traceparent header that carries the trace to the next service.</summary>
    public string TraceParent => $"00-{TraceId}-{SpanId}-01";
}

/// <summary>
/// Distributed tracing in miniature (the model behind OpenTelemetry and System.Diagnostics.Activity): a trace is a tree
/// of timed spans. The current span lives in an <see cref="AsyncLocal{T}"/>, so a span started anywhere below, even
/// across awaits and Task.Run, becomes its child automatically. The trace id travels to other services in a
/// traceparent header, so one request can be followed through every system it touches.
/// </summary>
public sealed class Tracer(TimeProvider? time = null)
{
    private static readonly AsyncLocal<Span?> CurrentSpan = new();

    internal TimeProvider Time { get; } = time ?? TimeProvider.System;

    public List<Span> Finished { get; } = [];

    public static Span? Current => CurrentSpan.Value;

    /// <param name="traceParent">An incoming traceparent header, to continue a trace started elsewhere.</param>
    public Span StartSpan(string name, string? traceParent = null)
    {
        var parent = CurrentSpan.Value;
        var traceId = parent?.TraceId
            ?? (traceParent?.Split('-') is [_, var id, _, _] && id.Length == 32 ? id : Guid.NewGuid().ToString("N"));
        var span = new Span(this, name, traceId, parent);
        CurrentSpan.Value = span;
        return span;
    }

    internal void End(Span span, TimeSpan duration)
    {
        span.SetDuration(duration);
        lock (Finished) Finished.Add(span);
        if (CurrentSpan.Value == span) CurrentSpan.Value = span.Parent;
    }

    /// <summary>The finished spans of one trace as an indented tree with durations.</summary>
    public string Render(string traceId)
    {
        var spans = Finished.Where(s => s.TraceId == traceId).ToList();
        var sb = new StringBuilder();
        void Write(Span? parent, int depth)
        {
            foreach (var span in spans.Where(s => s.Parent == parent).OrderBy(s => s.Name))
            {
                sb.Append(new string(' ', depth * 2)).Append($"{span.Name} {span.Duration.TotalMilliseconds:0}ms{(span.Status == "error" ? " ERROR" : "")}\n");
                Write(span, depth + 1);
            }
        }
        Write(spans.FirstOrDefault(s => s.Parent is null || !spans.Contains(s.Parent))?.Parent, 0);
        return sb.ToString();
    }
}
