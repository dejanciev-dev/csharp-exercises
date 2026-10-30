namespace CSharpExercises.FunctionalProgramming;

/// <summary>
/// An optional value as a type, instead of null: the compiler makes callers deal with "nothing" through
/// <see cref="Match"/>, <see cref="Map"/> or <see cref="GetValueOrDefault"/>. A readonly struct, so an Option itself is
/// never null, and <c>default</c> is None. Supports LINQ query syntax through Select and SelectMany.
/// </summary>
public readonly struct Option<T> : IEquatable<Option<T>>
{
    private readonly T _value;

    private Option(T value)
    {
        _value = value;
        IsSome = true;
    }

    public bool IsSome { get; }
    public bool IsNone => !IsSome;

    public static Option<T> None => default;

    public static Option<T> Some(T value) =>
        value is null ? throw new ArgumentNullException(nameof(value), "Use None instead of Some(null).") : new(value);

    public static implicit operator Option<T>(T value) => value is null ? None : new(value);

    public TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none) => IsSome ? some(_value) : none();

    public Option<TResult> Map<TResult>(Func<T, TResult> map) => IsSome ? Option<TResult>.Some(map(_value)) : Option<TResult>.None;

    public Option<TResult> Bind<TResult>(Func<T, Option<TResult>> bind) => IsSome ? bind(_value) : Option<TResult>.None;

    public Option<T> Where(Func<T, bool> predicate) => IsSome && predicate(_value) ? this : None;

    public T GetValueOrDefault(T fallback) => IsSome ? _value : fallback;

    public bool TryGetValue(out T value)
    {
        value = _value;
        return IsSome;
    }

    public Option<TResult> Select<TResult>(Func<T, TResult> map) => Map(map);

    public Option<TResult> SelectMany<TNext, TResult>(Func<T, Option<TNext>> bind, Func<T, TNext, TResult> project)
    {
        var self = this;
        return Bind(a => bind(a).Map(b => project(self._value, b)));
    }

    public bool Equals(Option<T> other) =>
        IsSome == other.IsSome && (IsNone || EqualityComparer<T>.Default.Equals(_value, other._value));

    public override bool Equals(object? obj) => obj is Option<T> other && Equals(other);
    public override int GetHashCode() => IsSome ? HashCode.Combine(_value) : 0;
    public static bool operator ==(Option<T> a, Option<T> b) => a.Equals(b);
    public static bool operator !=(Option<T> a, Option<T> b) => !a.Equals(b);

    public override string ToString() => IsSome ? $"Some({_value})" : "None";
}

public static class Option
{
    public static Option<T> Some<T>(T value) => Option<T>.Some(value);

    /// <summary>Dictionary lookups and parsing without out parameters.</summary>
    public static Option<TValue> Find<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key) =>
        dictionary.TryGetValue(key, out var value) ? Option<TValue>.Some(value!) : Option<TValue>.None;

    public static Option<int> ParseInt(string text) => int.TryParse(text, out var n) ? Some(n) : Option<int>.None;

    public static Option<T> FirstOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        foreach (var item in source)
            if (predicate(item)) return item;
        return Option<T>.None;
    }

    /// <summary>Keeps only the values that are present.</summary>
    public static IEnumerable<T> Values<T>(this IEnumerable<Option<T>> options)
    {
        foreach (var option in options)
            if (option.TryGetValue(out var value)) yield return value;
    }
}
