namespace CSharpExercises.Accounting;

public enum CostingMethod { Fifo, Lifo, WeightedAverage }

/// <summary>
/// The cost of goods sold depends on which units you pretend were sold when identical units were bought at different
/// prices. FIFO sells the oldest purchases first, LIFO the newest, weighted average blends them. When prices rise,
/// FIFO shows higher profit and higher closing stock value, LIFO the opposite; the total cost is the same, just split
/// differently between what was sold and what remains. Purchase lots are kept in a deque-like list.
/// </summary>
public sealed class InventoryCosting(CostingMethod method)
{
    private readonly List<(int Quantity, decimal UnitCost)> _lots = [];

    public decimal CostOfGoodsSold { get; private set; }
    public int UnitsOnHand => _lots.Sum(l => l.Quantity);
    public decimal StockValue => _lots.Sum(l => l.Quantity * l.UnitCost);

    public void Purchase(int quantity, decimal unitCost)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (method == CostingMethod.WeightedAverage && _lots.Count > 0)
        {
            // Keep a single lot at the running average cost.
            var (q, value) = (UnitsOnHand + quantity, StockValue + quantity * unitCost);
            _lots.Clear();
            _lots.Add((q, value / q));
            return;
        }
        _lots.Add((quantity, unitCost));
    }

    /// <returns>The cost of the units sold.</returns>
    public decimal Sell(int quantity)
    {
        if (quantity > UnitsOnHand) throw new InvalidOperationException($"Only {UnitsOnHand} units on hand.");
        decimal cost = 0;
        while (quantity > 0)
        {
            var index = method == CostingMethod.Lifo ? _lots.Count - 1 : 0;
            var (lotQuantity, unitCost) = _lots[index];
            var take = Math.Min(quantity, lotQuantity);
            cost += take * unitCost;
            quantity -= take;
            if (take == lotQuantity) _lots.RemoveAt(index);
            else _lots[index] = (lotQuantity - take, unitCost);
        }
        cost = Math.Round(cost, 2, MidpointRounding.AwayFromZero);
        CostOfGoodsSold += cost;
        return cost;
    }
}
