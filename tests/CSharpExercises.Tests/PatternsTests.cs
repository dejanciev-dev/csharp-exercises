using CSharpExercises.Oop;
using CSharpExercises.Patterns;

namespace CSharpExercises.Tests;

public class PatternsTests
{
    private static ShoppingCart CartWorth(decimal amount)
    {
        var cart = new ShoppingCart();
        cart.Add("item", amount);
        return cart;
    }

    [Fact]
    public void Strategy_CanBeSwappedAtRuntime()
    {
        var cart = new ShoppingCart();
        cart.Add("keyboard", 80);
        cart.Add("mouse", 40);

        Assert.Equal(120, cart.Total);

        cart.Discount = new PercentageDiscount(25);
        Assert.Equal(90, cart.Total);

        cart.Discount = new FixedAmountDiscount(500);
        Assert.Equal(0, cart.Total); // never goes negative
    }

    [Theory]
    [InlineData(99, 99)]
    [InlineData(100, 90)]
    [InlineData(250, 225)]
    public void Strategy_Composes(decimal subtotal, decimal expected)
    {
        var cart = CartWorth(subtotal);
        cart.Discount = new MinimumSpendDiscount(100, new PercentageDiscount(10));

        Assert.Equal(expected, cart.Total);
    }

    [Fact]
    public void Strategy_RejectsInvalidPercentage() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new PercentageDiscount(150));

    [Fact]
    public void Observer_NotifiesOnlyCurrentSubscribers()
    {
        var ticker = new StockTicker();
        var msftAlert = new PriceAlert("MSFT", 500);
        var aaplAlert = new PriceAlert("AAPL", 200);

        using var msftSubscription = ticker.Subscribe(msftAlert);
        var aaplSubscription = ticker.Subscribe(aaplAlert);

        ticker.Publish("MSFT", 480);
        ticker.Publish("MSFT", 510);
        ticker.Publish("AAPL", 205);

        aaplSubscription.Dispose();
        ticker.Publish("AAPL", 230); // AAPL alert has unsubscribed

        Assert.Equal([new StockPrice("MSFT", 510)], msftAlert.Triggered);
        Assert.Equal([new StockPrice("AAPL", 205)], aaplAlert.Triggered);
        Assert.Equal(1, ticker.SubscriberCount);
    }

    [Fact]
    public void Observer_CloseCompletesSubscribers()
    {
        var ticker = new StockTicker();
        var alert = new PriceAlert("MSFT", 0);
        ticker.Subscribe(alert);

        ticker.Close();

        Assert.True(alert.IsCompleted);
        Assert.Equal(0, ticker.SubscriberCount);
    }

    [Fact]
    public void Observer_CanUnsubscribeDuringNotification()
    {
        var ticker = new StockTicker();
        var received = 0;
        IDisposable? subscription = null;
        subscription = ticker.Subscribe(new OneShotObserver(() =>
        {
            received++;
            subscription!.Dispose();
        }));

        ticker.Publish("MSFT", 1);
        ticker.Publish("MSFT", 2);

        Assert.Equal(1, received);
    }

    private sealed class OneShotObserver(Action onNext) : IObserver<StockPrice>
    {
        public void OnNext(StockPrice value) => onNext();
        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }

    [Theory]
    [InlineData("circle 1", typeof(Circle), Math.PI)]
    [InlineData("Rectangle 2 3.5", typeof(Rectangle), 7)]
    [InlineData("  square   4 ", typeof(Square), 16)]
    public void Factory_CreatesShapesFromText(string description, Type expectedType, double expectedArea)
    {
        var shape = new ShapeFactory().Create(description);

        Assert.IsType(expectedType, shape);
        Assert.Equal(expectedArea, shape.Area, precision: 10);
    }

    [Theory]
    [InlineData("hexagon 2")]
    [InlineData("circle")]
    [InlineData("rectangle 2")]
    [InlineData("circle abc")]
    [InlineData("")]
    public void Factory_RejectsBadInput(string description) =>
        Assert.Throws<ArgumentException>(() => new ShapeFactory().Create(description));

    [Fact]
    public void Factory_AcceptsNewShapesWithoutChangingIt()
    {
        var factory = new ShapeFactory();
        factory.Register("triangle", 1, a => new EquilateralTriangle(a[0]));

        var shape = factory.Create("triangle 2");

        Assert.IsType<EquilateralTriangle>(shape);
        Assert.Equal(6, shape.Perimeter);
        Assert.Contains("triangle", factory.Names);
    }

    private sealed class EquilateralTriangle(double side) : Shape
    {
        public override double Area => Math.Sqrt(3) / 4 * side * side;
        public override double Perimeter => 3 * side;
    }

    private static readonly Dictionary<string, decimal> Prices = new() { ["BOOK"] = 20m, ["PEN"] = 2.50m };

    [Fact]
    public void Decorator_CacheAvoidsRepeatedLookups()
    {
        var catalog = new CatalogPriceService(Prices);
        IPriceService service = new CachingPriceService(catalog);

        service.GetPrice("BOOK");
        service.GetPrice("BOOK");
        service.GetPrice("PEN");

        Assert.Equal(2, catalog.Lookups);
    }

    [Fact]
    public void Decorator_OrderMatters()
    {
        var logAroundCache = new List<string>();
        var cacheAroundLog = new List<string>();

        IPriceService a = new LoggingPriceService(new CachingPriceService(new CatalogPriceService(Prices)), logAroundCache.Add);
        IPriceService b = new CachingPriceService(new LoggingPriceService(new CatalogPriceService(Prices), cacheAroundLog.Add));
        for (var i = 0; i < 3; i++)
        {
            a.GetPrice("PEN");
            b.GetPrice("PEN");
        }

        Assert.Equal(3, logAroundCache.Count); // logs every request
        Assert.Equal(["PEN = 2.50"], cacheAroundLog); // logs only cache misses
    }

    [Fact]
    public void Decorator_SaleWrapsAnyService()
    {
        IPriceService service = new SalePriceService(new CachingPriceService(new CatalogPriceService(Prices)), percentOff: 15);

        Assert.Equal(17m, service.GetPrice("BOOK"));
        Assert.Throws<KeyNotFoundException>(() => service.GetPrice("LAPTOP"));
    }

    [Fact]
    public void Builder_BuildsEmailFluently()
    {
        var email = new EmailBuilder()
            .From("dejan@example.com")
            .To("ana@example.com", "marko@example.com")
            .Cc("team@example.com")
            .Subject("Release notes")
            .Body("v1.2 is out.")
            .Attach("notes.pdf")
            .Build();

        Assert.Equal("dejan@example.com", email.From);
        Assert.Equal(["ana@example.com", "marko@example.com"], email.To);
        Assert.Equal(["team@example.com"], email.Cc);
        Assert.Equal(["notes.pdf"], email.Attachments);
    }

    [Fact]
    public void Builder_ValidatesOnBuild()
    {
        Assert.Throws<InvalidOperationException>(() => new EmailBuilder().To("a@example.com").Subject("Hi").Build());
        Assert.Throws<InvalidOperationException>(() => new EmailBuilder().From("a@example.com").Subject("Hi").Build());
        Assert.Throws<InvalidOperationException>(() => new EmailBuilder().From("a@example.com").To("b@example.com").Build());
    }

    [Fact]
    public void Builder_RejectsInvalidAddressImmediately() =>
        Assert.Throws<ArgumentException>(() => new EmailBuilder().To("not-an-email"));

    [Fact]
    public void Builder_BuiltEmailIsNotAffectedByLaterChanges()
    {
        var builder = new EmailBuilder().From("a@example.com").To("b@example.com").Subject("First");
        var first = builder.Build();

        var second = builder.To("c@example.com").Subject("Second").Build();

        Assert.Equal(["b@example.com"], first.To);
        Assert.Equal("First", first.Subject);
        Assert.Equal(2, second.To.Count);
    }
}
