using System.Globalization;

namespace CSharpExercises.Parsing;

/// <summary>
/// Things to do with a syntax tree once it is parsed: evaluate it, simplify it and print it back.
/// Each one is a recursive function with one case per node type, written as a switch expression.
/// </summary>
public static class ExpressionTools
{
    private static readonly Dictionary<string, (int MinArgs, int MaxArgs, Func<double[], double> Apply)> Functions = new()
    {
        ["abs"] = (1, 1, a => Math.Abs(a[0])),
        ["sqrt"] = (1, 1, a => Math.Sqrt(a[0])),
        ["round"] = (1, 1, a => Math.Round(a[0])),
        ["min"] = (1, int.MaxValue, a => a.Min()),
        ["max"] = (1, int.MaxValue, a => a.Max()),
    };

    /// <summary>Parses and evaluates in one step.</summary>
    public static double Evaluate(string text, IReadOnlyDictionary<string, double>? variables = null) =>
        Evaluate(Parser.Parse(text), variables);

    public static double Evaluate(Expr expr, IReadOnlyDictionary<string, double>? variables = null) => expr switch
    {
        Number n => n.Value,
        Variable v => variables is not null && variables.TryGetValue(v.Name, out var value)
            ? value
            : throw new KeyNotFoundException($"Unknown variable '{v.Name}'."),
        Negate n => -Evaluate(n.Operand, variables),
        Binary b => Apply(b.Operator, Evaluate(b.Left, variables), Evaluate(b.Right, variables)),
        Call c => Invoke(c.Function, [.. c.Arguments.Select(a => Evaluate(a, variables))]),
        _ => throw new ArgumentOutOfRangeException(nameof(expr), expr, "Unknown expression type."),
    };

    /// <summary>
    /// Folds constants and removes identities, bottom-up: <c>x * (3 - 2) + 0</c> becomes <c>x</c>.
    /// Anything that would fail when evaluated, such as <c>1 / 0</c>, is left alone for <see cref="Evaluate(Expr, IReadOnlyDictionary{string, double}?)"/> to report.
    /// Rules such as <c>x - x = 0</c> and <c>x * 0 = 0</c> assume finite values; they do not hold for NaN or infinity.
    /// </summary>
    public static Expr Simplify(Expr expr) => expr switch
    {
        Negate n => Simplify(n.Operand) switch
        {
            Number { Value: var v } => new Number(-v),
            Negate inner => inner.Operand,
            var operand => new Negate(operand),
        },
        Binary b => SimplifyBinary(b.Operator, Simplify(b.Left), Simplify(b.Right)),
        Call c => SimplifyCall(c.Function, [.. c.Arguments.Select(Simplify)]),
        _ => expr,
    };

    /// <summary>Prints the tree back as text, with only the parentheses it needs.</summary>
    public static string Format(Expr expr) => expr switch
    {
        Number n => n.Value.ToString(CultureInfo.InvariantCulture),
        Variable v => v.Name,
        Negate n => "-" + Wrap(n.Operand, Precedence(n.Operand) < UnaryPrecedence),
        Binary b => FormatBinary(b),
        Call c => $"{c.Function}({string.Join(", ", c.Arguments.Select(Format))})",
        _ => throw new ArgumentOutOfRangeException(nameof(expr), expr, "Unknown expression type."),
    };

    private static double Apply(char op, double left, double right) => op switch
    {
        '+' => left + right,
        '-' => left - right,
        '*' => left * right,
        '/' => right == 0 ? throw new DivideByZeroException() : left / right,
        '%' => right == 0 ? throw new DivideByZeroException() : left % right,
        '^' => Math.Pow(left, right),
        _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Unknown operator."),
    };

    private static double Invoke(string name, double[] args)
    {
        if (!Functions.TryGetValue(name, out var function))
            throw new KeyNotFoundException($"Unknown function '{name}'. Known functions: {string.Join(", ", Functions.Keys)}.");

        if (args.Length < function.MinArgs || args.Length > function.MaxArgs)
            throw new ArgumentException($"{name} does not take {args.Length} argument(s).");

        return function.Apply(args);
    }

    private static Expr SimplifyBinary(char op, Expr left, Expr right) => (op, left, right) switch
    {
        (_, Number l, Number r) when TryFold(() => Apply(op, l.Value, r.Value), out var folded) => folded,
        ('+', Number { Value: 0 }, _) => right,
        ('+' or '-', _, Number { Value: 0 }) => left,
        ('-', _, _) when left == right => new Number(0),
        ('*', Number { Value: 1 }, _) => right,
        ('*' or '/', _, Number { Value: 1 }) => left,
        ('*', Number { Value: 0 }, _) or ('*', _, Number { Value: 0 }) => new Number(0),
        ('^', _, Number { Value: 1 }) => left,
        ('^', _, Number { Value: 0 }) => new Number(1),
        _ => new Binary(op, left, right),
    };

    private static Expr SimplifyCall(string name, List<Expr> args) =>
        args.All(a => a is Number) && TryFold(() => Invoke(name, [.. args.Cast<Number>().Select(n => n.Value)]), out var folded)
            ? folded
            : new Call(name, args);

    private static bool TryFold(Func<double> compute, out Number result)
    {
        try
        {
            result = new Number(compute());
            return true;
        }
        catch (Exception ex) when (ex is DivideByZeroException or ArgumentException or KeyNotFoundException)
        {
            result = null!;
            return false;
        }
    }

    private const int UnaryPrecedence = 3;

    private static int Precedence(Expr expr) => expr switch
    {
        Binary { Operator: '+' or '-' } => 1,
        Binary { Operator: '*' or '/' or '%' } => 2,
        Negate => UnaryPrecedence,
        Number { Value: < 0 } => UnaryPrecedence, // prints with a leading '-', so it needs the same care as Negate
        Binary { Operator: '^' } => 4,
        _ => 5,
    };

    private static string FormatBinary(Binary b)
    {
        var precedence = Precedence(b);

        // ^ is right-associative: (2^3)^2 needs parentheses on the left, 2^(3^2) does not on the right.
        // The others are left-associative: a - (b - c) needs them on the right.
        var leftNeedsParens = Precedence(b.Left) < precedence || (b.Operator == '^' && Precedence(b.Left) == precedence);
        var rightNeedsParens = Precedence(b.Right) < precedence || (b.Operator != '^' && Precedence(b.Right) == precedence);

        return $"{Wrap(b.Left, leftNeedsParens)} {b.Operator} {Wrap(b.Right, rightNeedsParens)}";
    }

    private static string Wrap(Expr expr, bool parenthesize) => parenthesize ? $"({Format(expr)})" : Format(expr);
}
