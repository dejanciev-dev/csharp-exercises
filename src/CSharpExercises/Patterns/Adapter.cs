namespace CSharpExercises.Patterns;

/// <summary>What our code wants: readings in Celsius, asynchronously, with a timestamp.</summary>
public interface ITemperatureSource
{
    Task<(double Celsius, DateTimeOffset At)> ReadAsync(CancellationToken cancellationToken = default);
}

/// <summary>A third-party driver we can't change: Fahrenheit, tenths of a degree as an int, a status code, synchronous.</summary>
public sealed class LegacyThermometer(int tenthsFahrenheit)
{
    public int ReadingCount { get; private set; }

    public int Poll(out int tenths)
    {
        ReadingCount++;
        tenths = tenthsFahrenheit;
        return tenthsFahrenheit == int.MinValue ? -1 : 0; // -1 means sensor error
    }
}

/// <summary>
/// Adapter: wraps the legacy API so it fits the interface the rest of the code expects. All the translation (units,
/// error codes into exceptions, sync into async) lives in one place, and nothing else learns about the old driver.
/// </summary>
public sealed class LegacyThermometerAdapter(LegacyThermometer thermometer, TimeProvider time) : ITemperatureSource
{
    public Task<(double Celsius, DateTimeOffset At)> ReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (thermometer.Poll(out var tenths) != 0) throw new IOException("The thermometer reported a sensor error.");

        var fahrenheit = tenths / 10.0;
        var celsius = Math.Round((fahrenheit - 32) * 5 / 9, 2);
        return Task.FromResult((celsius, time.GetUtcNow()));
    }
}

/// <summary>Adapting a plain function, handy in tests: a delegate becomes an implementation of the interface.</summary>
public sealed class DelegateTemperatureSource(Func<double> readCelsius, TimeProvider time) : ITemperatureSource
{
    public Task<(double Celsius, DateTimeOffset At)> ReadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult((readCelsius(), time.GetUtcNow()));
}

public static class TemperatureMonitor
{
    /// <summary>Only knows the target interface, so it works with any adapted source.</summary>
    public static async Task<double> AverageAsync(IEnumerable<ITemperatureSource> sources, CancellationToken cancellationToken = default)
    {
        var readings = await Task.WhenAll(sources.Select(s => s.ReadAsync(cancellationToken)));
        return readings.Average(r => r.Celsius);
    }
}
