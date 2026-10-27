namespace CSharpExercises.StateMachines;

// Events: what the customer or the operator does.
public abstract record VendingEvent;
public sealed record CoinInserted(int Cents) : VendingEvent;
public sealed record ProductSelected(string Code) : VendingEvent;
public sealed record CancelPressed : VendingEvent;
public sealed record Restocked(string Code, int Quantity) : VendingEvent;

// Effects: what the machine does in response. Returned as data, so tests can check them without any hardware.
public abstract record VendingEffect;
public sealed record Dispense(string Code) : VendingEffect;
public sealed record ReturnCoins(int Cents) : VendingEffect;
public sealed record Show(string Message) : VendingEffect;

public sealed record Product(string Name, int PriceCents, int Stock);

/// <summary>The machine's whole state as one immutable value.</summary>
public sealed record VendingState(int CreditCents, IReadOnlyDictionary<string, Product> Products)
{
    public static VendingState Empty(IReadOnlyDictionary<string, Product> products) => new(0, products);
}

/// <summary>
/// The other way to write a state machine: one pure function <c>(state, event) =&gt; (new state, effects)</c>,
/// using pattern matching on the pair. No mutation, no I/O, so every rule can be tested in isolation and any
/// sequence of events can be replayed to reach the same state.
/// </summary>
public static class VendingMachine
{
    public static readonly int[] AcceptedCoins = [5, 10, 20, 50, 100, 200];

    public static (VendingState State, IReadOnlyList<VendingEffect> Effects) Handle(VendingState state, VendingEvent @event) =>
        @event switch
        {
            CoinInserted { Cents: var cents } when !AcceptedCoins.Contains(cents) =>
                (state, [new ReturnCoins(cents), new Show("Coin not accepted")]),

            CoinInserted { Cents: var cents } =>
                (state with { CreditCents = state.CreditCents + cents }, [new Show($"Credit: {Format(state.CreditCents + cents)}")]),

            ProductSelected { Code: var code } when !state.Products.ContainsKey(code) =>
                (state, [new Show($"Unknown product {code}")]),

            ProductSelected { Code: var code } when state.Products[code] is { Stock: 0 } =>
                (state, [new Show($"{state.Products[code].Name} is sold out")]),

            ProductSelected { Code: var code } when state.Products[code] is { PriceCents: var price } && state.CreditCents < price =>
                (state, [new Show($"Insert {Format(price - state.CreditCents)} more")]),

            ProductSelected { Code: var code } => Sell(state, code),

            CancelPressed when state.CreditCents == 0 => (state, []),

            CancelPressed => (state with { CreditCents = 0 }, [new ReturnCoins(state.CreditCents), new Show("Cancelled")]),

            Restocked { Code: var code, Quantity: > 0 and var quantity } when state.Products.TryGetValue(code, out var product) =>
                (WithProduct(state, code, product with { Stock = product.Stock + quantity }), []),

            _ => (state, [new Show("Ignored")]),
        };

    /// <summary>Applies events one after another, collecting every effect: the state at any point can be rebuilt from its history.</summary>
    public static (VendingState State, List<VendingEffect> Effects) Run(VendingState state, IEnumerable<VendingEvent> events)
    {
        List<VendingEffect> effects = [];
        foreach (var @event in events)
        {
            (state, var produced) = Handle(state, @event);
            effects.AddRange(produced);
        }
        return (state, effects);
    }

    private static (VendingState, IReadOnlyList<VendingEffect>) Sell(VendingState state, string code)
    {
        var product = state.Products[code];
        var change = state.CreditCents - product.PriceCents;

        List<VendingEffect> effects = [new Dispense(code)];
        if (change > 0) effects.Add(new ReturnCoins(change));
        effects.Add(new Show($"Enjoy your {product.Name}"));

        return (WithProduct(state, code, product with { Stock = product.Stock - 1 }) with { CreditCents = 0 }, effects);
    }

    private static VendingState WithProduct(VendingState state, string code, Product product) =>
        state with { Products = new Dictionary<string, Product>(state.Products) { [code] = product } };

    private static string Format(int cents) => $"{cents / 100}.{cents % 100:00}";
}
