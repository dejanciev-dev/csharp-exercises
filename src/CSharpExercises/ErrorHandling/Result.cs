namespace CSharpExercises.ErrorHandling;

public sealed record Error(string Code, string Message);

/// <summary>
/// Either a value or one or more errors. Expected failures (bad input, a rule that was not met) become values
/// the caller has to handle, instead of exceptions they might forget to catch.
/// </summary>
/// <remarks>
/// A class rather than a struct, so there is no <c>default(Result&lt;T&gt;)</c> that is neither a success nor a failure.
/// </remarks>
public sealed class Result<T>
{
    private readonly T _value;

    private Result(T value, IReadOnlyList<Error> errors)
    {
        _value = value;
        Errors = errors;
    }

    public static Result<T> Success(T value) => new(value, []);

    public static Result<T> Failure(params IReadOnlyList<Error> errors)
    {
        if (errors.Count == 0) throw new ArgumentException("A failure needs at least one error.", nameof(errors));
        return new(default!, errors);
    }

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);

    public bool IsSuccess => Errors.Count == 0;
    public IReadOnlyList<Error> Errors { get; }

    public T Value => IsSuccess
        ? _value
        : throw new InvalidOperationException($"Cannot read the value of a failed result: {string.Join("; ", Errors.Select(e => e.Message))}");

    /// <summary>Transforms the value; a failure passes through untouched.</summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> map) =>
        IsSuccess ? map(_value) : Result<TOut>.Failure(Errors);

    /// <summary>Chains a step that can itself fail. Stops at the first failure ("railway-oriented" programming).</summary>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> next) =>
        IsSuccess ? next(_value) : Result<TOut>.Failure(Errors);

    /// <summary>Turns a success into a failure when <paramref name="predicate"/> does not hold.</summary>
    public Result<T> Ensure(Func<T, bool> predicate, Error error) =>
        !IsSuccess || predicate(_value) ? this : error;

    /// <summary>Forces the caller to handle both outcomes.</summary>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure) =>
        IsSuccess ? onSuccess(_value) : onFailure(Errors);

    // Select and SelectMany let results be combined with LINQ query syntax: from a in ... from b in ... select ...
    public Result<TOut> Select<TOut>(Func<T, TOut> map) => Map(map);

    public Result<TOut> SelectMany<TNext, TOut>(Func<T, Result<TNext>> next, Func<T, TNext, TOut> project) =>
        Bind(value => next(value).Map(nextValue => project(value, nextValue)));

    public override string ToString() =>
        IsSuccess ? $"Success({_value})" : $"Failure({string.Join(", ", Errors.Select(e => e.Code))})";
}

public static class Result
{
    /// <summary>
    /// Combines independent results. Unlike <see cref="Result{T}.Bind{TOut}"/>, which stops at the first error,
    /// this checks every input and reports all of their errors together, as a form should.
    /// </summary>
    public static Result<TOut> Combine<T1, T2, T3, TOut>(
        Result<T1> first, Result<T2> second, Result<T3> third, Func<T1, T2, T3, TOut> combine)
    {
        Error[] errors = [.. first.Errors, .. second.Errors, .. third.Errors];
        return errors.Length == 0
            ? combine(first.Value, second.Value, third.Value)
            : Result<TOut>.Failure(errors);
    }

    /// <summary>Runs code that throws and turns the exception into a failure: the bridge from exception-based APIs.</summary>
    public static Result<T> Try<T>(Func<T> action, Func<Exception, Error> toError)
    {
        try
        {
            return action();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return toError(ex);
        }
    }
}
