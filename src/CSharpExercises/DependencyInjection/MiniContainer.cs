namespace CSharpExercises.DependencyInjection;

public enum Lifetime
{
    /// <summary>A new instance every time it is resolved.</summary>
    Transient,

    /// <summary>One instance per scope (e.g. per web request).</summary>
    Scoped,

    /// <summary>One instance for the whole application.</summary>
    Singleton
}

/// <summary>
/// A small dependency injection container written from scratch, to show what Microsoft.Extensions.DependencyInjection
/// does inside: register services with a lifetime, then <see cref="Build"/> a root scope that creates them
/// by calling constructors and resolving each parameter. Not thread-safe; for learning only.
/// </summary>
public sealed class MiniContainer
{
    private readonly Dictionary<Type, Registration> _registrations = new();

    /// <summary>When <typeparamref name="TService"/> is requested, construct a <typeparamref name="TImplementation"/>.</summary>
    public MiniContainer Register<TService, TImplementation>(Lifetime lifetime)
        where TImplementation : class, TService
    {
        _registrations[typeof(TService)] = new Registration(lifetime, (scope, chain) => scope.Construct(typeof(TImplementation), chain));
        return this;
    }

    public MiniContainer Register<TService>(Func<MiniScope, TService> factory, Lifetime lifetime)
        where TService : class
    {
        _registrations[typeof(TService)] = new Registration(lifetime, (scope, _) => factory(scope));
        return this;
    }

    /// <summary>An instance created outside the container: always a singleton, and never disposed by the container.</summary>
    public MiniContainer RegisterInstance<TService>(TService instance)
        where TService : class
    {
        _registrations[typeof(TService)] = new Registration(Lifetime.Singleton, (_, _) => instance, OwnsInstance: false);
        return this;
    }

    public MiniScope Build() => new(new Dictionary<Type, Registration>(_registrations), root: null);
}

internal sealed record Registration(Lifetime Lifetime, Func<MiniScope, List<Type>, object> Create, bool OwnsInstance = true);

/// <summary>
/// Resolves services. The root scope holds singletons; child scopes from <see cref="CreateScope"/> hold scoped services.
/// Disposing a scope disposes every disposable instance it created.
/// </summary>
public sealed class MiniScope : IDisposable
{
    private readonly IReadOnlyDictionary<Type, Registration> _registrations;
    private readonly MiniScope? _root;
    private readonly Dictionary<Type, object> _cache = new();
    private readonly List<IDisposable> _disposables = [];
    private bool _disposed;

    internal MiniScope(IReadOnlyDictionary<Type, Registration> registrations, MiniScope? root)
    {
        _registrations = registrations;
        _root = root;
    }

    public bool IsRoot => _root is null;

    public MiniScope CreateScope()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new MiniScope(_registrations, _root ?? this);
    }

    public T Resolve<T>() => (T)Resolve(typeof(T), []);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Reverse order: things created later may depend on things created earlier.
        for (var i = _disposables.Count - 1; i >= 0; i--) _disposables[i].Dispose();
    }

    /// <param name="chain">The services currently being constructed, used to detect A → B → A cycles.</param>
    internal object Resolve(Type type, List<Type> chain)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (chain.Contains(type))
        {
            var cycle = string.Join(" -> ", chain.Append(type).Select(t => t.Name));
            throw new InvalidOperationException($"Circular dependency: {cycle}.");
        }

        if (!_registrations.TryGetValue(type, out var registration))
        {
            var neededBy = chain.Count > 0 ? $" (needed by {chain[^1].Name})" : "";
            throw new InvalidOperationException($"No registration for {type.Name}{neededBy}.");
        }

        chain.Add(type);
        try
        {
            return registration.Lifetime switch
            {
                // Singletons are always created by the root, so their dependencies are resolved from the root too.
                // A singleton that needs a scoped service therefore fails below instead of silently keeping
                // one scope's instance forever (a "captive dependency").
                Lifetime.Singleton => (_root ?? this).GetOrCreate(type, registration, chain),
                Lifetime.Scoped when IsRoot => throw new InvalidOperationException(
                    $"Cannot resolve scoped service {type.Name} from the root scope{(chain.Count > 1 ? $" (needed by {chain[^2].Name})" : "")}. Create a scope first."),
                Lifetime.Scoped => GetOrCreate(type, registration, chain),
                _ => Create(registration, chain)
            };
        }
        finally
        {
            chain.RemoveAt(chain.Count - 1);
        }
    }

    /// <summary>Calls the public constructor with the most parameters, resolving each parameter from the container.</summary>
    internal object Construct(Type implementation, List<Type> chain)
    {
        var constructor = implementation.GetConstructors()
                              .OrderByDescending(c => c.GetParameters().Length)
                              .FirstOrDefault()
                          ?? throw new InvalidOperationException($"{implementation.Name} has no public constructor.");

        var arguments = constructor.GetParameters().Select(p => Resolve(p.ParameterType, chain)).ToArray();
        return constructor.Invoke(arguments);
    }

    private object GetOrCreate(Type type, Registration registration, List<Type> chain)
    {
        if (!_cache.TryGetValue(type, out var instance))
        {
            instance = Create(registration, chain);
            _cache[type] = instance;
        }

        return instance;
    }

    private object Create(Registration registration, List<Type> chain)
    {
        var instance = registration.Create(this, chain);

        // The scope that creates an instance is the one that disposes it.
        if (registration.OwnsInstance && instance is IDisposable disposable) _disposables.Add(disposable);
        return instance;
    }
}
