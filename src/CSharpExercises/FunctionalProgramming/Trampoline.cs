using System.Numerics;

namespace CSharpExercises.FunctionalProgramming;

/// <summary>
/// C# doesn't guarantee tail-call elimination, so deeply recursive functions overflow the stack. A trampoline makes
/// each step return either a final result or "call me again with this", and a loop keeps bouncing until done:
/// recursion written naturally, executed in constant stack space.
/// </summary>
public abstract record Bounce<T>
{
    public sealed record Done(T Result) : Bounce<T>;
    public sealed record More(Func<Bounce<T>> Next) : Bounce<T>;
}

public static class Trampoline
{
    public static T Run<T>(Bounce<T> bounce)
    {
        while (true)
        {
            switch (bounce)
            {
                case Bounce<T>.Done done: return done.Result;
                case Bounce<T>.More more: bounce = more.Next(); break;
            }
        }
    }

    public static Bounce<T> Done<T>(T result) => new Bounce<T>.Done(result);
    public static Bounce<T> More<T>(Func<Bounce<T>> next) => new Bounce<T>.More(next);

    /// <summary>Sum 1..n with an accumulator, written recursively; a million levels deep is no problem.</summary>
    public static long Sum(long n) => Run(SumStep(n, 0));

    private static Bounce<long> SumStep(long n, long accumulator) =>
        n == 0 ? Done(accumulator) : More(() => SumStep(n - 1, accumulator + n));

    /// <summary>Mutual recursion works too: IsEven calls IsOdd calls IsEven…</summary>
    public static bool IsEven(int n) => Run(Even(n));

    private static Bounce<bool> Even(int n) => n == 0 ? Done(true) : More(() => Odd(n - 1));
    private static Bounce<bool> Odd(int n) => n == 0 ? Done(false) : More(() => Even(n - 1));

    public static BigInteger Factorial(int n) => Run(FactorialStep(n, BigInteger.One));

    private static Bounce<BigInteger> FactorialStep(int n, BigInteger accumulator) =>
        n <= 1 ? Done(accumulator) : More(() => FactorialStep(n - 1, accumulator * n));

    /// <summary>The naive version, to show where the stack runs out.</summary>
    public static long SumRecursive(long n) => n == 0 ? 0 : n + SumRecursive(n - 1);
}
