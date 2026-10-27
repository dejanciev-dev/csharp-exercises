using System.Text.Json;
using System.Text.Json.Serialization;

namespace CSharpExercises.Serialization;

public sealed record OrderLine(string Sku, int Quantity, Money UnitPrice);

/// <summary>
/// Polymorphic JSON: the <c>"type"</c> property records which subclass was written, so reading
/// <c>OrderEvent</c> back gives an <see cref="OrderPlaced"/>, <see cref="OrderShipped"/> or <see cref="OrderCancelled"/>.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(OrderPlaced), "placed")]
[JsonDerivedType(typeof(OrderShipped), "shipped")]
[JsonDerivedType(typeof(OrderCancelled), "cancelled")]
public abstract record OrderEvent(Guid OrderId, DateTimeOffset At);

public sealed record OrderPlaced(Guid OrderId, DateTimeOffset At, IReadOnlyList<OrderLine> Lines) : OrderEvent(OrderId, At);
public sealed record OrderShipped(Guid OrderId, DateTimeOffset At, string TrackingNumber) : OrderEvent(OrderId, At);
public sealed record OrderCancelled(Guid OrderId, DateTimeOffset At, string Reason) : OrderEvent(OrderId, At);

public enum OrderStatus { None, Placed, Shipped, Cancelled }

public sealed record OrderSummary(OrderStatus Status, Money? Total, string? TrackingNumber);

/// <summary>
/// Serialization code generated at compile time instead of discovered with reflection at run time:
/// faster startup, and it works with trimming and Native AOT.
/// </summary>
[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    UseStringEnumConverter = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
[JsonSerializable(typeof(List<OrderEvent>))]
[JsonSerializable(typeof(OrderSummary))]
public sealed partial class OrderJsonContext : JsonSerializerContext;

/// <summary>Stores an order as the list of events that happened to it, and rebuilds its state by replaying them.</summary>
public static class OrderEventLog
{
    public static string Serialize(IEnumerable<OrderEvent> events) =>
        JsonSerializer.Serialize([.. events], OrderJsonContext.Default.ListOrderEvent);

    public static List<OrderEvent> Deserialize(string json) =>
        JsonSerializer.Deserialize(json, OrderJsonContext.Default.ListOrderEvent) ?? [];

    public static OrderSummary Replay(IEnumerable<OrderEvent> events) =>
        events.OrderBy(e => e.At).Aggregate(new OrderSummary(OrderStatus.None, null, null), Apply);

    private static OrderSummary Apply(OrderSummary state, OrderEvent @event) => (state.Status, @event) switch
    {
        (OrderStatus.None, OrderPlaced placed) => state with
        {
            Status = OrderStatus.Placed,
            Total = placed.Lines.Select(l => l.UnitPrice * l.Quantity).Aggregate((a, b) => a + b),
        },
        (OrderStatus.Placed, OrderShipped shipped) => state with { Status = OrderStatus.Shipped, TrackingNumber = shipped.TrackingNumber },
        (OrderStatus.Placed, OrderCancelled) => state with { Status = OrderStatus.Cancelled },
        _ => throw new InvalidOperationException($"Cannot apply {@event.GetType().Name} to an order that is {state.Status}."),
    };
}
