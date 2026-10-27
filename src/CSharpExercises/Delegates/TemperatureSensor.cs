namespace CSharpExercises.Delegates;

public sealed class TemperatureChangedEventArgs(double previous, double current) : EventArgs
{
    public double Previous => previous;
    public double Current => current;
}

/// <summary>
/// The standard .NET event pattern, and two of its traps: a throwing handler stopping the others,
/// and lambdas that cannot be unsubscribed because you no longer have a reference to them.
/// </summary>
public sealed class TemperatureSensor
{
    private double _current;

    public event EventHandler<TemperatureChangedEventArgs>? TemperatureChanged;

    /// <summary>Exceptions thrown by handlers. They are collected so one broken subscriber cannot stop the rest.</summary>
    public List<Exception> HandlerErrors { get; } = [];

    public double Current => _current;

    public int SubscriberCount => TemperatureChanged?.GetInvocationList().Length ?? 0;

    /// <summary>Records a reading and raises the event if it changed by at least <paramref name="threshold"/>.</summary>
    public void Report(double reading, double threshold = 0.5)
    {
        var previous = _current;
        if (Math.Abs(reading - previous) < threshold) return;

        _current = reading;
        OnTemperatureChanged(new TemperatureChangedEventArgs(previous, reading));
    }

    /// <summary>
    /// Subscribes and returns a token that unsubscribes when disposed, so a lambda handler can still be removed.
    /// </summary>
    public IDisposable Subscribe(Action<double> onChange)
    {
        EventHandler<TemperatureChangedEventArgs> handler = (_, e) => onChange(e.Current);
        TemperatureChanged += handler;
        return new Unsubscriber(() => TemperatureChanged -= handler);
    }

    private void OnTemperatureChanged(TemperatureChangedEventArgs e)
    {
        // Copy to a local first: another thread could unsubscribe the last handler between the null check and the call.
        // `TemperatureChanged?.Invoke(this, e)` does the same, but would stop at the first handler that throws.
        var handlers = TemperatureChanged;
        if (handlers is null) return;

        foreach (var handler in handlers.GetInvocationList().Cast<EventHandler<TemperatureChangedEventArgs>>())
        {
            try
            {
                handler(this, e);
            }
            catch (Exception ex)
            {
                HandlerErrors.Add(ex);
            }
        }
    }

    private sealed class Unsubscriber(Action unsubscribe) : IDisposable
    {
        private Action? _unsubscribe = unsubscribe;

        public void Dispose() => Interlocked.Exchange(ref _unsubscribe, null)?.Invoke();
    }
}
