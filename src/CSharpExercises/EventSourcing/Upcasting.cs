namespace CSharpExercises.EventSourcing;

public sealed record CustomerRegisteredV1(Guid Id, string FullName);
public sealed record CustomerRegisteredV2(Guid Id, string FirstName, string LastName);
public sealed record CustomerRegisteredV3(Guid Id, string FirstName, string LastName, string Country);

/// <summary>
/// Events are immutable history, so when their shape changes the old ones can't be edited. Instead, upcasters convert
/// old versions to the current one as they are read, one step at a time (V1 → V2 → V3), so the rest of the code only
/// ever deals with the latest version and each upcaster stays small.
/// </summary>
public sealed class Upcaster
{
    private readonly Dictionary<Type, Func<object, object>> _steps = new();

    public Upcaster Register<TFrom, TTo>(Func<TFrom, TTo> step) where TFrom : notnull where TTo : notnull
    {
        _steps[typeof(TFrom)] = e => step((TFrom)e);
        return this;
    }

    public object Upcast(object e)
    {
        var seen = new HashSet<Type>();
        while (_steps.TryGetValue(e.GetType(), out var step))
        {
            if (!seen.Add(e.GetType())) throw new InvalidOperationException($"Upcasting cycle at {e.GetType().Name}.");
            e = step(e);
        }
        return e;
    }

    public IEnumerable<object> UpcastAll(IEnumerable<StoredEvent> events) => events.Select(e => Upcast(e.Data));

    public static Upcaster ForCustomers() => new Upcaster()
        .Register<CustomerRegisteredV1, CustomerRegisteredV2>(v1 =>
        {
            var space = v1.FullName.LastIndexOf(' ');
            return space < 0
                ? new(v1.Id, v1.FullName, "")
                : new(v1.Id, v1.FullName[..space], v1.FullName[(space + 1)..]);
        })
        .Register<CustomerRegisteredV2, CustomerRegisteredV3>(v2 => new(v2.Id, v2.FirstName, v2.LastName, Country: "unknown"));
}
