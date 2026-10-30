namespace CSharpExercises.FunctionalProgramming;

public readonly record struct ParseResult<T>(bool Success, T Value, int Position, string? Expected = null);

/// <summary>
/// Parser combinators: a parser is just a function from (text, position) to a result, and small parsers combine
/// into bigger ones with ordinary methods (Then, Or, Many, Select). Compared with the hand-written recursive descent
/// parser in Parsing/, the grammar reads almost like its BNF. LINQ query syntax works for sequencing.
/// </summary>
public sealed class TextParser<T>(Func<string, int, ParseResult<T>> parse)
{
    public ParseResult<T> Parse(string text, int position) => parse(text, position);

    /// <summary>Parses the whole input or throws, reporting the furthest position reached.</summary>
    public T ParseAll(string text)
    {
        var result = Parse(text, 0);
        if (result.Success && result.Position == text.Length) return result.Value;
        throw new FormatException($"Expected {(result.Success ? "end of input" : result.Expected)} at position {result.Position}.");
    }

    public TextParser<TResult> Select<TResult>(Func<T, TResult> map) => new((s, i) =>
    {
        var r = parse(s, i);
        return r.Success ? new(true, map(r.Value), r.Position) : new(false, default!, r.Position, r.Expected);
    });

    public TextParser<TResult> SelectMany<TNext, TResult>(Func<T, TextParser<TNext>> next, Func<T, TNext, TResult> project) => new((s, i) =>
    {
        var first = parse(s, i);
        if (!first.Success) return new(false, default!, first.Position, first.Expected);
        var second = next(first.Value).Parse(s, first.Position);
        return second.Success
            ? new(true, project(first.Value, second.Value), second.Position)
            : new(false, default!, second.Position, second.Expected);
    });

    /// <summary>Tries this parser, and the other one from the same position if this one fails.</summary>
    public TextParser<T> Or(TextParser<T> other) => new((s, i) =>
    {
        var r = parse(s, i);
        if (r.Success) return r;
        var o = other.Parse(s, i);
        return o.Success ? o : o with { Expected = $"{r.Expected} or {o.Expected}" };
    });

    public TextParser<IReadOnlyList<T>> Many() => new((s, i) =>
    {
        var items = new List<T>();
        while (true)
        {
            var r = parse(s, i);
            if (!r.Success || r.Position == i) return new(true, items, i); // stop on failure or no progress
            items.Add(r.Value);
            i = r.Position;
        }
    });

    public TextParser<IReadOnlyList<T>> AtLeastOnce() =>
        from first in this
        from rest in Many()
        select (IReadOnlyList<T>)[first, .. rest];

    /// <summary>The same parser, skipping whitespace on both sides.</summary>
    public TextParser<T> Token() =>
        Parsers.Whitespace
            .SelectMany(_ => this, (_, value) => value)
            .SelectMany(_ => Parsers.Whitespace, (value, _) => value);
}

public static class Parsers
{
    public static TextParser<char> Char(Func<char, bool> predicate, string expected) => new((s, i) =>
        i < s.Length && predicate(s[i]) ? new(true, s[i], i + 1) : new(false, default, i, expected));

    public static TextParser<char> Char(char c) => Char(x => x == c, $"'{c}'");

    public static TextParser<string> Whitespace { get; } = Char(char.IsWhiteSpace, "whitespace").Many().Select(cs => new string([.. cs]));

    public static TextParser<double> Number { get; } =
        Char(char.IsAsciiDigit, "a digit").AtLeastOnce()
            .Select(ds => double.Parse(new string([.. ds]), System.Globalization.CultureInfo.InvariantCulture))
            .Token();

    /// <summary>Lazily refers to a parser defined later, which recursive grammars need.</summary>
    public static TextParser<T> Ref<T>(Func<TextParser<T>> parser) => new((s, i) => parser().Parse(s, i));

    /// <summary>One or more operands separated by operators, folded left-to-right: left-associative.</summary>
    public static TextParser<T> ChainLeft<T>(TextParser<T> operand, TextParser<Func<T, T, T>> op) =>
        from first in operand
        from rest in (from f in op from y in operand select (f, y)).Many()
        select rest.Aggregate(first, (acc, pair) => pair.f(acc, pair.y));

    /// <summary>
    /// A calculator grammar in a few lines:
    /// expr = term (('+' | '-') term)*,  term = factor (('*' | '/') factor)*,  factor = number | '(' expr ')'.
    /// </summary>
    public static TextParser<double> Calculator { get; } = BuildCalculator();

    private static TextParser<double> BuildCalculator()
    {
        TextParser<double> expr = null!;
        var factor = Number.Or(
            from open in Char('(').Token()
            from e in Ref(() => expr)
            from close in Char(')').Token()
            select e);

        static TextParser<Func<double, double, double>> Op(char c, Func<double, double, double> f) => Char(c).Token().Select(_ => f);

        var term = ChainLeft(factor, Op('*', (a, b) => a * b).Or(Op('/', (a, b) => a / b)));
        expr = ChainLeft(term, Op('+', (a, b) => a + b).Or(Op('-', (a, b) => a - b)));
        return expr;
    }
}
