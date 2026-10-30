using System.Reflection;

namespace CSharpExercises.Patterns;

public interface IDocumentStore
{
    string Load(string id);
    void Save(string id, string content);
}

/// <summary>The real thing: expensive to create (imagine opening connections or loading an index).</summary>
public sealed class DocumentStore : IDocumentStore
{
    private readonly Dictionary<string, string> _documents = new();

    public static int Instances { get; private set; }

    public DocumentStore() => Instances++;

    public string Load(string id) => _documents.TryGetValue(id, out var content) ? content : throw new KeyNotFoundException(id);
    public void Save(string id, string content) => _documents[id] = content;
}

/// <summary>
/// Virtual proxy: stands in for the real object and only creates it on first use, so a store that's never touched
/// never pays its start-up cost. Callers can't tell the difference.
/// </summary>
public sealed class LazyDocumentStore(Func<IDocumentStore> factory) : IDocumentStore
{
    private readonly Lazy<IDocumentStore> _real = new(factory);

    public bool IsCreated => _real.IsValueCreated;

    public string Load(string id) => _real.Value.Load(id);
    public void Save(string id, string content) => _real.Value.Save(id, content);
}

/// <summary>Protection proxy: same interface, but checks permissions before forwarding.</summary>
public sealed class SecuredDocumentStore(IDocumentStore inner, Func<string> currentRole) : IDocumentStore
{
    public string Load(string id) => inner.Load(id);

    public void Save(string id, string content)
    {
        if (currentRole() is not ("editor" or "admin"))
            throw new UnauthorizedAccessException($"Role '{currentRole()}' may not save documents.");
        inner.Save(id, content);
    }
}

/// <summary>
/// A proxy generated at run time with <see cref="DispatchProxy"/>: one class logs calls for any interface, instead of
/// writing a decorator per interface. This is how many mocking and interception libraries work.
/// </summary>
public class LoggingProxy<T> : DispatchProxy where T : class
{
    private T _target = null!;
    private List<string> _log = null!;

    public static T Create(T target, List<string> log)
    {
        var proxy = Create<T, LoggingProxy<T>>();
        var self = (LoggingProxy<T>)(object)proxy;
        self._target = target;
        self._log = log;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(method);
        _log.Add($"{method.Name}({string.Join(", ", args ?? [])})");
        try
        {
            return method.Invoke(_target, args);
        }
        catch (TargetInvocationException e) when (e.InnerException is not null)
        {
            _log.Add($"{method.Name} threw {e.InnerException.GetType().Name}");
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(e.InnerException); // rethrow the real error, not the wrapper
            throw;
        }
    }
}
