using System.Text.Json;
using CSharpExercises.Serialization;

namespace CSharpExercises.Tests;

public class SerializationTests
{
    private static readonly Guid OrderId = Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff");
    private static readonly DateTimeOffset Monday = new(2026, 10, 26, 9, 0, 0, TimeSpan.Zero);

    private static OrderPlaced Placed() => new(OrderId, Monday,
    [
        new OrderLine("LAMP-01", 2, Money.Parse("19.99 EUR")),
        new OrderLine("BULB-60", 4, Money.Parse("2.50 EUR")),
    ]);

    [Fact]
    public void Money_SerializesAsASingleString()
    {
        Assert.Equal("\"12.50 EUR\"", JsonSerializer.Serialize(new Money(12.50m, "EUR")));
    }

    [Fact]
    public void Money_RoundTripsWithoutLosingPrecision()
    {
        var money = new Money(0.1m + 0.2m, "USD");

        var copy = JsonSerializer.Deserialize<Money>(JsonSerializer.Serialize(money));

        Assert.Equal(money, copy);
        Assert.Equal("0.3 USD", copy.ToString());
    }

    [Theory]
    [InlineData("\"12.50\"")]
    [InlineData("\"12.50 euro\"")]
    [InlineData("\"twelve EUR\"")]
    [InlineData("12.50")]
    public void Money_InvalidJson_ThrowsJsonException(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Money>(json));
    }

    [Fact]
    public void Money_ErrorsPointToWhereInTheJsonTheyHappened()
    {
        const string json = """[{"type":"placed","orderId":"6f9619ff-8b86-d011-b42d-00cf4fc964ff","at":"2026-10-26T09:00:00+00:00","lines":[{"sku":"A","quantity":1,"unitPrice":"oops"}]}]""";

        var ex = Assert.Throws<JsonException>(() => OrderEventLog.Deserialize(json));

        Assert.Equal("$[0].lines[0].unitPrice", ex.Path);
        Assert.Contains("'oops'", ex.Message);
    }

    [Fact]
    public void Money_CannotAddDifferentCurrencies()
    {
        Assert.Throws<InvalidOperationException>(() => Money.Parse("1 EUR") + Money.Parse("1 USD"));
    }

    [Fact]
    public void Events_AreWrittenWithATypeDiscriminatorAndCamelCase()
    {
        var json = OrderEventLog.Serialize([new OrderShipped(OrderId, Monday, "TRACK-123")]);

        Assert.Equal(
            """[{"type":"shipped","trackingNumber":"TRACK-123","orderId":"6f9619ff-8b86-d011-b42d-00cf4fc964ff","at":"2026-10-26T09:00:00+00:00"}]""",
            json);
    }

    [Fact]
    public void Events_RoundTripToTheirOriginalSubclasses()
    {
        OrderEvent[] events = [Placed(), new OrderShipped(OrderId, Monday.AddDays(1), "TRACK-123")];

        var copy = OrderEventLog.Deserialize(OrderEventLog.Serialize(events));

        Assert.IsType<OrderPlaced>(copy[0]);
        Assert.Equal(events[1], copy[1]);
        Assert.Equal(((OrderPlaced)events[0]).Lines, ((OrderPlaced)copy[0]).Lines);
    }

    [Fact]
    public void Events_UnknownPropertyIsRejected()
    {
        const string json = """[{"type":"cancelled","orderId":"6f9619ff-8b86-d011-b42d-00cf4fc964ff","at":"2026-10-26T09:00:00+00:00","reason":"changed mind","refund":true}]""";

        var ex = Assert.Throws<JsonException>(() => OrderEventLog.Deserialize(json));
        Assert.Contains("refund", ex.Message);
    }

    [Fact]
    public void Events_UnknownTypeIsRejected()
    {
        const string json = """[{"type":"teleported","orderId":"6f9619ff-8b86-d011-b42d-00cf4fc964ff","at":"2026-10-26T09:00:00+00:00"}]""";

        var ex = Assert.Throws<JsonException>(() => OrderEventLog.Deserialize(json));
        Assert.Contains("teleported", ex.Message);
    }

    [Fact]
    public void Replay_BuildsTheCurrentState()
    {
        var summary = OrderEventLog.Replay([new OrderShipped(OrderId, Monday.AddDays(1), "TRACK-123"), Placed()]);

        Assert.Equal(new OrderSummary(OrderStatus.Shipped, Money.Parse("49.98 EUR"), "TRACK-123"), summary);
    }

    [Fact]
    public void Replay_RejectsImpossibleTransitions()
    {
        OrderEvent[] events = [Placed(), new OrderCancelled(OrderId, Monday.AddHours(1), "out of stock"), new OrderShipped(OrderId, Monday.AddDays(1), "TRACK-123")];

        var ex = Assert.Throws<InvalidOperationException>(() => OrderEventLog.Replay(events));
        Assert.Contains("Cancelled", ex.Message);
    }

    [Fact]
    public void Summary_WritesEnumsAsStrings()
    {
        var json = JsonSerializer.Serialize(new OrderSummary(OrderStatus.Placed, null, null), OrderJsonContext.Default.OrderSummary);

        Assert.Equal("""{"status":"Placed","total":null,"trackingNumber":null}""", json);
    }
}
