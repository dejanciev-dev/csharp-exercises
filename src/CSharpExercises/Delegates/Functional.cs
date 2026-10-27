using System.Collections.Concurrent;

namespace CSharpExercises.Delegates;

/// <summary>Functions as values: combining, partially applying and caching them.</summary>
public static class Functional
{
    /// <summary><c>f.Then(g)</c> is <c>x =&gt; g(f(x))</c>: reads left to right, in the order things happen.</summary>
    public static Func<T, TResult> Then<T, TMiddle, TResult>(this Func<T, TMiddle> first, Func<TMiddle, TResult> second) =>
        x => second(first(x));

    /// <summary>Applies the steps one after another; with none, returns the input unchanged.</summary>
    public static Func<T, T> Pipeline<T>(params IEnumerable<Func<T, T>> steps) =>
        steps.Aggregate((Func<T, T>)(x => x), (composed, step) => composed.Then(step));

    /// <summary>Turns a two-argument function into a chain of one-argument functions: <c>add(1)(2)</c>.</summary>
    public static Func<T1, Func<T2, TResult>> Curry<T1, T2, TResult>(this Func<T1, T2, TResult> function) =>
        a => b => function(a, b);

    /// <summary>Fixes the first argument, giving a function of the remaining one.</summary>
    public static Func<T2, TResult> Partial<T1, T2, TResult>(this Func<T1, T2, TResult> function, T1 first) =>
        second => function(first, second);

    /// <summary>
    /// Wraps a pure function so each input is computed once. Thread-safe; two threads that miss together may
    /// both compute the value, which is harmless for a pure function (see <c>OnceCache</c> when it is not).
    /// </summary>
    public static Func<T, TResult> Memoize<T, TResult>(this Func<T, TResult> function) where T : notnull
    {
        var cache = new ConcurrentDictionary<T, TResult>();
        return x => cache.GetOrAdd(x, function);
    }

    /// <summary>
    /// Memoizes a recursive function. The function receives the memoized version of itself to recurse with, so the
    /// recursive calls hit the cache too. Memoizing only the outer call would leave naive Fibonacci exponential.
    /// </summary>
    public static Func<T, TResult> MemoizeRecursive<T, TResult>(Func<Func<T, TResult>, T, TResult> function) where T : notnull
    {
        var cache = new ConcurrentDictionary<T, TResult>();
        Func<T, TResult>? self = null;
        self = x => cache.TryGetValue(x, out var cached) ? cached : cache.GetOrAdd(x, function(self!, x));
        return self;
    }
}
