using CSharpExercises.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace CSharpExercises.Tests;

public class DependencyInjectionTests
{
    // Small service graph for the hand-written container: Checkout -> (IPaymentGateway, AuditLog)

    private interface IPaymentGateway
    {
        string Charge(decimal amount);
    }

    private sealed class FakeGateway : IPaymentGateway
    {
        public string Charge(decimal amount) => $"charged {amount}";
    }

    private sealed class AuditLog : IDisposable
    {
        public List<string> Entries { get; } = [];
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    private sealed class Checkout(IPaymentGateway gateway, AuditLog audit)
    {
        public IPaymentGateway Gateway { get; } = gateway;
        public AuditLog Audit { get; } = audit;

        public string Pay(decimal amount)
        {
            var result = Gateway.Charge(amount);
            Audit.Entries.Add(result);
            return result;
        }
    }

    private sealed class NeedsScoped(AuditLog audit)
    {
        public AuditLog Audit { get; } = audit;
    }

    private sealed class ChickenService(EggService egg)
    {
        public EggService Egg { get; } = egg;
    }

    private sealed class EggService(ChickenService chicken)
    {
        public ChickenService Chicken { get; } = chicken;
    }

    private static MiniScope BuildShop() =>
        new MiniContainer()
            .Register<IPaymentGateway, FakeGateway>(Lifetime.Singleton)
            .Register<AuditLog, AuditLog>(Lifetime.Scoped)
            .Register<Checkout, Checkout>(Lifetime.Transient)
            .Build();

    [Fact]
    public void Mini_ResolvesConstructorDependencies()
    {
        using var root = BuildShop();
        using var scope = root.CreateScope();

        var checkout = scope.Resolve<Checkout>();

        Assert.Equal("charged 9.99", checkout.Pay(9.99m));
        Assert.IsType<FakeGateway>(checkout.Gateway);
        Assert.Equal(["charged 9.99"], checkout.Audit.Entries);
    }

    [Fact]
    public void Mini_LifetimesControlSharing()
    {
        using var root = BuildShop();
        using var scope1 = root.CreateScope();
        using var scope2 = root.CreateScope();

        var a = scope1.Resolve<Checkout>();
        var b = scope1.Resolve<Checkout>();
        var c = scope2.Resolve<Checkout>();

        Assert.NotSame(a, b);                 // transient: new each time
        Assert.Same(a.Audit, b.Audit);        // scoped: shared within a scope
        Assert.NotSame(a.Audit, c.Audit);     // ...but not across scopes
        Assert.Same(a.Gateway, c.Gateway);    // singleton: shared everywhere
    }

    [Fact]
    public void Mini_ScopedServiceCannotComeFromRoot()
    {
        using var root = BuildShop();

        var ex = Assert.Throws<InvalidOperationException>(() => root.Resolve<AuditLog>());
        Assert.Contains("Create a scope first", ex.Message);
    }

    [Fact]
    public void Mini_DetectsCaptiveDependency()
    {
        // A singleton holding a scoped service would keep the first scope's instance forever.
        using var root = new MiniContainer()
            .Register<AuditLog, AuditLog>(Lifetime.Scoped)
            .Register<NeedsScoped, NeedsScoped>(Lifetime.Singleton)
            .Build();
        using var scope = root.CreateScope();

        var ex = Assert.Throws<InvalidOperationException>(() => scope.Resolve<NeedsScoped>());
        Assert.Contains("needed by NeedsScoped", ex.Message);
    }

    [Fact]
    public void Mini_DetectsCircularDependency()
    {
        using var root = new MiniContainer()
            .Register<ChickenService, ChickenService>(Lifetime.Transient)
            .Register<EggService, EggService>(Lifetime.Transient)
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() => root.Resolve<ChickenService>());
        Assert.Equal("Circular dependency: ChickenService -> EggService -> ChickenService.", ex.Message);
    }

    [Fact]
    public void Mini_MissingRegistrationNamesWhoNeededIt()
    {
        using var root = new MiniContainer().Register<Checkout, Checkout>(Lifetime.Transient).Build();

        var ex = Assert.Throws<InvalidOperationException>(() => root.Resolve<Checkout>());
        Assert.Equal("No registration for IPaymentGateway (needed by Checkout).", ex.Message);
    }

    [Fact]
    public void Mini_DisposingScopeDisposesWhatItCreated()
    {
        using var root = BuildShop();

        AuditLog scoped;
        using (var scope = root.CreateScope())
        {
            scoped = scope.Resolve<AuditLog>();
            Assert.False(scoped.IsDisposed);
        }

        Assert.True(scoped.IsDisposed);
    }

    [Fact]
    public void Mini_ExternalInstancesAreNotDisposed()
    {
        var external = new AuditLog();
        var root = new MiniContainer().RegisterInstance(external).Build();

        Assert.Same(external, root.Resolve<AuditLog>());
        root.Dispose();

        Assert.False(external.IsDisposed); // whoever created it owns it
        Assert.Throws<ObjectDisposedException>(() => root.Resolve<AuditLog>());
    }

    [Fact]
    public void Mini_FactoryRegistration()
    {
        var created = 0;
        using var root = new MiniContainer()
            .Register<IPaymentGateway>(_ => { created++; return new FakeGateway(); }, Lifetime.Singleton)
            .Build();

        root.Resolve<IPaymentGateway>();
        root.Resolve<IPaymentGateway>();

        Assert.Equal(1, created);
    }

    // Microsoft.Extensions.DependencyInjection

    private sealed class FixedTimeProvider(int hour) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 1, 15, hour, 0, 0, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private static ServiceProvider BuildMessaging(int hour = 9) =>
        new ServiceCollection()
            .AddSingleton<TimeProvider>(new FixedTimeProvider(hour))
            .AddMessaging()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

    [Theory]
    [InlineData(8, "Good morning, Ana!")]
    [InlineData(14, "Good afternoon, Ana!")]
    [InlineData(21, "Good evening, Ana!")]
    public void Ms_InjectedClockMakesTimeTestable(int hour, string expected)
    {
        using var provider = BuildMessaging(hour);

        var text = provider.GetRequiredService<WelcomeService>().Welcome("Ana", "ana@example.com");

        Assert.Equal(expected, text);
        Assert.Equal([new SentMessage("email", "ana@example.com", expected)], provider.GetRequiredService<IOutbox>().Messages);
    }

    [Fact]
    public void Ms_KeyedServicePicksSmsSender()
    {
        using var provider = BuildMessaging();

        var code = provider.GetRequiredService<TwoFactorService>().SendCode("+38970123456");

        var message = Assert.Single(provider.GetRequiredService<IOutbox>().Messages);
        Assert.Equal("sms", message.Channel);
        Assert.Matches(@"^\d{6}$", code);
        Assert.EndsWith(code, message.Text);
    }

    [Fact]
    public void Ms_LifetimesControlSharing()
    {
        using var provider = BuildMessaging();
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        Assert.NotSame(scope1.ServiceProvider.GetRequiredService<WelcomeService>(),
                       scope1.ServiceProvider.GetRequiredService<WelcomeService>());
        Assert.Same(scope1.ServiceProvider.GetRequiredService<RequestContext>(),
                    scope1.ServiceProvider.GetRequiredService<RequestContext>());
        Assert.NotEqual(scope1.ServiceProvider.GetRequiredService<RequestContext>().RequestId,
                        scope2.ServiceProvider.GetRequiredService<RequestContext>().RequestId);
        Assert.Same(scope1.ServiceProvider.GetRequiredService<IOutbox>(),
                    scope2.ServiceProvider.GetRequiredService<IOutbox>());
    }

    private sealed class RequestLogger(RequestContext context)
    {
        public RequestContext Context { get; } = context;
    }

    [Fact]
    public void Ms_WithoutValidation_CaptiveDependencyGoesUnnoticed()
    {
        using var provider = new ServiceCollection()
            .AddMessaging()
            .AddSingleton<RequestLogger>()
            .BuildServiceProvider(); // no validation

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        // Bug: every request sees the same RequestContext, captured by the singleton.
        Assert.Same(scope1.ServiceProvider.GetRequiredService<RequestLogger>().Context,
                    scope2.ServiceProvider.GetRequiredService<RequestLogger>().Context);
    }

    [Fact]
    public void Ms_ValidationCatchesProblemsAtStartup()
    {
        var captive = new ServiceCollection().AddMessaging().AddSingleton<RequestLogger>();
        var missing = new ServiceCollection().AddTransient<WelcomeService>(); // no IMessageSender or TimeProvider
        var options = new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true };

        Assert.Throws<AggregateException>(() => captive.BuildServiceProvider(options));
        Assert.Throws<AggregateException>(() => missing.BuildServiceProvider(options));
    }

    [Fact]
    public void Ms_DisposingScopeDisposesScopedServices()
    {
        using var provider = new ServiceCollection().AddScoped<AuditLog>().BuildServiceProvider();

        AuditLog log;
        using (var scope = provider.CreateScope())
        {
            log = scope.ServiceProvider.GetRequiredService<AuditLog>();
        }

        Assert.True(log.IsDisposed);
    }

    [Fact]
    public void Ms_SmsIsTruncated()
    {
        var outbox = new Outbox();
        new SmsSender(outbox).Send("+1", new string('x', 200));

        Assert.Equal(SmsSender.MaxLength, outbox.Messages[0].Text.Length);
    }
}
