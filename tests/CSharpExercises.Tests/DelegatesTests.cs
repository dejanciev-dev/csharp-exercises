using System.Numerics;
using CSharpExercises.Delegates;

namespace CSharpExercises.Tests;

public class DelegatesTests
{
    [Fact]
    public void Then_ComposesLeftToRight()
    {
        Func<string, string> trim = s => s.Trim();
        var shout = trim.Then(s => s.ToUpperInvariant()).Then(s => s + "!");

        Assert.Equal("HELLO!", shout("  hello "));
    }

    [Fact]
    public void Pipeline_AppliesStepsInOrder()
    {
        var pipeline = Functional.Pipeline<int>(x => x + 1, x => x * 10, x => x - 3);

        Assert.Equal(17, pipeline(1));
    }

    [Fact]
    public void Pipeline_WithNoSteps_IsTheIdentity()
    {
        Assert.Equal(5, Functional.Pipeline<int>()(5));
    }

    [Fact]
    public void Curry_AndPartial_FixArgumentsOneAtATime()
    {
        Func<decimal, decimal, decimal> applyTax = (rate, price) => price * (1 + rate);

        var withVat = applyTax.Partial(0.2m);
        var curried = applyTax.Curry();

        Assert.Equal(120m, withVat(100m));
        Assert.Equal(110m, curried(0.1m)(100m));
    }

    [Fact]
    public void Memoize_ComputesEachInputOnce()
    {
        var calls = 0;
        Func<int, int> square = x =>
        {
            calls++;
            return x * x;
        };
        var cached = square.Memoize();

        Assert.Equal(49, cached(7));
        Assert.Equal(49, cached(7));
        Assert.Equal(9, cached(3));
        Assert.Equal(2, calls);
    }

    [Fact]
    public void MemoizeRecursive_MakesNaiveFibonacciLinear()
    {
        var calls = 0;
        var fibonacci = Functional.MemoizeRecursive<int, BigInteger>((self, n) =>
        {
            calls++;
            return n < 2 ? n : self(n - 1) + self(n - 2);
        });

        // Without memoizing the recursive calls this would take about 10^19 calls.
        Assert.Equal(BigInteger.Parse("2880067194370816120"), fibonacci(90));
        Assert.Equal(91, calls);
    }

    [Fact]
    public void Sensor_RaisesOnlyForChangesAboveTheThreshold()
    {
        var sensor = new TemperatureSensor();
        List<(double, double)> changes = [];
        sensor.TemperatureChanged += (_, e) => changes.Add((e.Previous, e.Current));

        sensor.Report(20);
        sensor.Report(20.3);
        sensor.Report(21);

        Assert.Equal([(0, 20), (20, 21)], changes);
    }

    [Fact]
    public void Sensor_AThrowingHandlerDoesNotStopTheOthers()
    {
        var sensor = new TemperatureSensor();
        var secondCalled = false;
        sensor.TemperatureChanged += (_, _) => throw new InvalidOperationException("display offline");
        sensor.TemperatureChanged += (_, _) => secondCalled = true;

        sensor.Report(25);

        Assert.True(secondCalled);
        Assert.Equal("display offline", Assert.Single(sensor.HandlerErrors).Message);
    }

    [Fact]
    public void Sensor_RemovingADifferentLambdaDoesNothing()
    {
        var sensor = new TemperatureSensor();
        sensor.TemperatureChanged += (_, e) => Console.WriteLine(e.Current);

        // Looks identical, but it is a different delegate instance, so nothing is removed.
        sensor.TemperatureChanged -= (_, e) => Console.WriteLine(e.Current);

        Assert.Equal(1, sensor.SubscriberCount);
    }

    [Fact]
    public void Sensor_DisposingTheSubscriptionUnsubscribes()
    {
        var sensor = new TemperatureSensor();
        List<double> readings = [];

        using (sensor.Subscribe(readings.Add))
        {
            sensor.Report(18);
            Assert.Equal(1, sensor.SubscriberCount);
        }
        sensor.Report(30);

        Assert.Equal([18], readings);
        Assert.Equal(0, sensor.SubscriberCount);
    }

    [Fact]
    public async Task Pipeline_RunsMiddlewareInOrderAndUnwindsInReverse()
    {
        var app = new PipelineBuilder()
            .UseLogging()
            .Use(async (context, next) =>
            {
                context.Log.Add("auth in");
                await next();
                context.Log.Add("auth out");
            })
            .Build(context =>
            {
                context.Log.Add("endpoint");
                context.Body = "hi";
                return Task.CompletedTask;
            });
        var context = new RequestContext("/hello");

        await app(context);

        Assert.Equal(["-> /hello", "auth in", "endpoint", "auth out", "<- 200"], context.Log);
        Assert.Equal("hi", context.Body);
    }

    [Fact]
    public async Task Pipeline_MiddlewareCanShortCircuit()
    {
        var endpointRan = false;
        var app = new PipelineBuilder()
            .UseLogging()
            .UseBlockList("/admin")
            .Build(_ =>
            {
                endpointRan = true;
                return Task.CompletedTask;
            });
        var context = new RequestContext("/admin");

        await app(context);

        Assert.False(endpointRan);
        Assert.Equal(["-> /admin", "<- 403"], context.Log);
    }

    [Fact]
    public async Task Pipeline_ErrorHandlingCatchesExceptionsFromFurtherDown()
    {
        var app = new PipelineBuilder()
            .UseErrorHandling()
            .Build(_ => throw new InvalidOperationException("database down"));
        var context = new RequestContext("/orders");

        await app(context);

        Assert.Equal(500, context.StatusCode);
        Assert.Equal("Error: database down", context.Body);
    }

    [Fact]
    public async Task Pipeline_WithoutAnEndpoint_Returns404()
    {
        var context = new RequestContext("/missing");

        await new PipelineBuilder().UseLogging().Build()(context);

        Assert.Equal(404, context.StatusCode);
    }

    [Fact]
    public async Task Pipeline_AcceptsRawNextToHandlerMiddleware()
    {
        var app = new PipelineBuilder()
            .Use(next => async context =>
            {
                context.Log.Add("raw");
                await next(context);
            })
            .Build(context =>
            {
                context.StatusCode = 204;
                return Task.CompletedTask;
            });
        var context = new RequestContext("/");

        await app(context);

        Assert.Equal(["raw"], context.Log);
        Assert.Equal(204, context.StatusCode);
    }
}
