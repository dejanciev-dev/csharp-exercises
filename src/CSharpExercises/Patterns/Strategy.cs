namespace CSharpExercises.Patterns;

/// <summary>
/// Strategy: the cart doesn't know how discounts work. Each rule is its own class behind one interface
/// and can be swapped at runtime, so adding a new rule never touches <see cref="ShoppingCart"/>.
/// </summary>
public interface IDiscountStrategy
{
    decimal Apply(decimal subtotal);
}

public class NoDiscount : IDiscountStrategy
{
    public decimal Apply(decimal subtotal) => subtotal;
}

public class PercentageDiscount(decimal percent) : IDiscountStrategy
{
    private readonly decimal _percent =
        percent is >= 0m and <= 100m ? percent : throw new ArgumentOutOfRangeException(nameof(percent));

    public decimal Apply(decimal subtotal) => Math.Round(subtotal * (1 - _percent / 100m), 2);
}

public class FixedAmountDiscount(decimal amount) : IDiscountStrategy
{
    public decimal Apply(decimal subtotal) => Math.Max(0, subtotal - amount);
}

/// <summary>Strategies compose: applies <paramref name="discount"/> only once the subtotal reaches <paramref name="threshold"/>.</summary>
public class MinimumSpendDiscount(decimal threshold, IDiscountStrategy discount) : IDiscountStrategy
{
    public decimal Apply(decimal subtotal) => subtotal >= threshold ? discount.Apply(subtotal) : subtotal;
}

public class ShoppingCart
{
    private readonly List<(string Item, decimal Price)> _items = [];

    public IDiscountStrategy Discount { get; set; } = new NoDiscount();

    public decimal Subtotal => _items.Sum(i => i.Price);

    public decimal Total => Discount.Apply(Subtotal);

    public void Add(string item, decimal price)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(price);
        _items.Add((item, price));
    }
}
