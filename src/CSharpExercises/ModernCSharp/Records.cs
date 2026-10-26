using System.Collections.Immutable;

namespace CSharpExercises.ModernCSharp;

public enum OrderStatus
{
    Draft,
    Placed,
    Shipped,
    Cancelled
}

public record OrderLine(string Sku, int Quantity, decimal UnitPrice)
{
    public decimal Total => Quantity * UnitPrice;
}

/// <summary>
/// An immutable order. Every change returns a new copy made with a <c>with</c> expression,
/// so an order that was shared or saved can never change underneath its holder.
/// </summary>
public sealed record Order(int Id, string Customer)
{
    public OrderStatus Status { get; init; } = OrderStatus.Draft;

    public ImmutableArray<OrderLine> Lines { get; init; } = [];

    public decimal Total => Lines.Sum(l => l.Total);

    public Order AddLine(OrderLine line) =>
        Status == OrderStatus.Draft
            ? this with { Lines = Lines.Add(line) }
            : throw new InvalidOperationException($"Cannot add lines to a {Status} order.");

    public Order Place() => Status switch
    {
        OrderStatus.Draft when Lines.IsEmpty => throw new InvalidOperationException("Cannot place an empty order."),
        OrderStatus.Draft => this with { Status = OrderStatus.Placed },
        _ => throw InvalidTransition(OrderStatus.Placed)
    };

    public Order Ship() =>
        Status == OrderStatus.Placed ? this with { Status = OrderStatus.Shipped } : throw InvalidTransition(OrderStatus.Shipped);

    public Order Cancel() =>
        Status is OrderStatus.Draft or OrderStatus.Placed
            ? this with { Status = OrderStatus.Cancelled }
            : throw InvalidTransition(OrderStatus.Cancelled);

    // Records compare collection properties by reference, so two orders with the same lines in different
    // arrays would not be equal. Overriding Equals makes the comparison look at the lines themselves.
    public bool Equals(Order? other) =>
        other is not null
        && Id == other.Id
        && Customer == other.Customer
        && Status == other.Status
        && Lines.SequenceEqual(other.Lines);

    public override int GetHashCode() => HashCode.Combine(Id, Customer, Status, Lines.Length);

    private InvalidOperationException InvalidTransition(OrderStatus target) =>
        new($"Cannot move an order from {Status} to {target}.");
}
