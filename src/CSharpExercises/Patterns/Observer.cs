namespace CSharpExercises.Patterns;

public record StockPrice(string Symbol, decimal Price);

/// <summary>
/// Observer, using .NET's built-in <see cref="IObservable{T}"/> / <see cref="IObserver{T}"/> pair:
/// the ticker pushes every price to whoever subscribed, without knowing what they do with it.
/// </summary>
public class StockTicker : IObservable<StockPrice>
{
    private readonly List<IObserver<StockPrice>> _observers = [];

    public int SubscriberCount => _observers.Count;

    /// <summary>Returns a handle; disposing it unsubscribes.</summary>
    public IDisposable Subscribe(IObserver<StockPrice> observer)
    {
        _observers.Add(observer);
        return new Unsubscriber(_observers, observer);
    }

    public void Publish(string symbol, decimal price)
    {
        var update = new StockPrice(symbol, price);

        // Iterate over a copy so an observer can unsubscribe while being notified.
        foreach (var observer in _observers.ToArray()) observer.OnNext(update);
    }

    public void Close()
    {
        foreach (var observer in _observers.ToArray()) observer.OnCompleted();
        _observers.Clear();
    }

    private sealed class Unsubscriber(List<IObserver<StockPrice>> observers, IObserver<StockPrice> observer) : IDisposable
    {
        public void Dispose() => observers.Remove(observer);
    }
}

/// <summary>Records every update where <paramref name="symbol"/> reaches <paramref name="threshold"/> or more.</summary>
public class PriceAlert(string symbol, decimal threshold) : IObserver<StockPrice>
{
    private readonly List<StockPrice> _triggered = [];

    public IReadOnlyList<StockPrice> Triggered => _triggered;

    public bool IsCompleted { get; private set; }

    public void OnNext(StockPrice value)
    {
        if (value.Symbol == symbol && value.Price >= threshold) _triggered.Add(value);
    }

    public void OnError(Exception error) { }

    public void OnCompleted() => IsCompleted = true;
}
