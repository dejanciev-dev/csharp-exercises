using System.Globalization;

namespace CSharpExercises.Patterns;

/// <summary>
/// Decorator: each class wraps another <see cref="IPriceService"/> and adds one behavior (caching, logging, a sale)
/// while looking exactly like the thing it wraps. Behaviors are combined by nesting, and the nesting order matters.
/// </summary>
public interface IPriceService
{
    decimal GetPrice(string sku);
}

/// <summary>The real service being decorated; counts lookups so tests can see what the cache saves.</summary>
public class CatalogPriceService(IReadOnlyDictionary<string, decimal> prices) : IPriceService
{
    public int Lookups { get; private set; }

    public decimal GetPrice(string sku)
    {
        Lookups++;
        return prices.TryGetValue(sku, out var price) ? price : throw new KeyNotFoundException($"Unknown SKU '{sku}'.");
    }
}

public class CachingPriceService(IPriceService inner) : IPriceService
{
    private readonly Dictionary<string, decimal> _cache = new();

    public decimal GetPrice(string sku)
    {
        if (!_cache.TryGetValue(sku, out var price))
        {
            price = inner.GetPrice(sku);
            _cache[sku] = price;
        }

        return price;
    }
}

public class LoggingPriceService(IPriceService inner, Action<string> log) : IPriceService
{
    public decimal GetPrice(string sku)
    {
        var price = inner.GetPrice(sku);
        log($"{sku} = {price.ToString(CultureInfo.InvariantCulture)}");
        return price;
    }
}

public class SalePriceService(IPriceService inner, decimal percentOff) : IPriceService
{
    public decimal GetPrice(string sku) => Math.Round(inner.GetPrice(sku) * (1 - percentOff / 100m), 2);
}
