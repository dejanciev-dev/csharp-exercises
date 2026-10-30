using System.Globalization;
using System.Text;

namespace CSharpExercises.Diagnostics;

public enum LogSeverity { Trace, Debug, Information, Warning, Error }

public sealed record LogEvent(DateTimeOffset Time, LogSeverity Level, string Template, string Message, IReadOnlyDictionary<string, object?> Properties);

/// <summary>
/// Structured logging as in Serilog and Microsoft.Extensions.Logging: the message is a template ("{User} logged in from
/// {Ip}") and the values are kept as named properties, not just baked into a string, so logs can be filtered and
/// aggregated by field. Scopes add properties (a request id) to every event logged inside them, flowing across awaits
/// via <see cref="AsyncLocal{T}"/>. Disabled levels skip formatting entirely.
/// </summary>
public sealed class StructuredLogger(LogSeverity minimumLevel = LogSeverity.Information, TimeProvider? time = null)
{
    private static readonly AsyncLocal<ImmutableScope?> CurrentScope = new();
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    public List<LogEvent> Events { get; } = [];

    public bool IsEnabled(LogSeverity level) => level >= minimumLevel;

    public void Log(LogSeverity level, string template, params object?[] values)
    {
        if (!IsEnabled(level)) return; // no allocations for filtered-out events

        var properties = new Dictionary<string, object?>();
        for (var scope = CurrentScope.Value; scope is not null; scope = scope.Parent)
            foreach (var (key, value) in scope.Properties) properties.TryAdd(key, value); // inner scopes win

        var message = Render(template, values, properties);
        lock (Events) Events.Add(new LogEvent(_time.GetUtcNow(), level, template, message, properties));
    }

    public void Information(string template, params object?[] values) => Log(LogSeverity.Information, template, values);
    public void Warning(string template, params object?[] values) => Log(LogSeverity.Warning, template, values);
    public void Error(string template, params object?[] values) => Log(LogSeverity.Error, template, values);

    /// <summary>Adds properties to everything logged until the returned scope is disposed.</summary>
    public static IDisposable BeginScope(params (string Key, object? Value)[] properties)
    {
        var scope = new ImmutableScope(CurrentScope.Value, properties.ToDictionary(p => p.Key, p => p.Value));
        CurrentScope.Value = scope;
        return new ScopeHandle(scope);
    }

    /// <summary>Replaces {Name} placeholders positionally, records each value under its name; {Name:format} is supported.</summary>
    public static string Render(string template, object?[] values, IDictionary<string, object?> properties)
    {
        var sb = new StringBuilder();
        var index = 0;
        for (var i = 0; i < template.Length; i++)
        {
            if (template[i] == '{' && i + 1 < template.Length && template[i + 1] == '{') { sb.Append('{'); i++; continue; }
            if (template[i] != '{') { sb.Append(template[i]); continue; }

            var end = template.IndexOf('}', i);
            if (end < 0) { sb.Append(template[i..]); break; }
            var hole = template[(i + 1)..end].Split(':', 2);
            var value = index < values.Length ? values[index++] : null;
            properties[hole[0]] = value;
            sb.Append(value switch
            {
                null => "null",
                IFormattable f => f.ToString(hole.Length > 1 ? hole[1] : null, CultureInfo.InvariantCulture),
                string s => $"\"{s}\"", // quoted, as Serilog renders strings
                _ => value.ToString(),
            });
            i = end;
        }
        return sb.ToString();
    }

    private sealed record ImmutableScope(ImmutableScope? Parent, IReadOnlyDictionary<string, object?> Properties);

    private sealed class ScopeHandle(ImmutableScope scope) : IDisposable
    {
        public void Dispose()
        {
            if (CurrentScope.Value == scope) CurrentScope.Value = scope.Parent;
        }
    }
}
