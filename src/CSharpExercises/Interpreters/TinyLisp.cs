using System.Globalization;

namespace CSharpExercises.Interpreters;

public sealed record Symbol(string Name)
{
    public override string ToString() => Name;
}

public sealed record Lambda(IReadOnlyList<Symbol> Parameters, object Body, LispEnvironment Closure);

/// <summary>A scope: its own bindings plus a link to the enclosing scope, searched outward.</summary>
public sealed class LispEnvironment(LispEnvironment? outer = null)
{
    private readonly Dictionary<string, object> _values = new();

    public object Get(string name) =>
        _values.TryGetValue(name, out var v) ? v : outer?.Get(name) ?? throw new KeyNotFoundException($"Unbound symbol '{name}'.");

    public void Define(string name, object value) => _values[name] = value;
}

/// <summary>
/// A small Lisp in the style of Peter Norvig's lis.py: the reader turns text into nested lists, and eval is one switch
/// over the special forms (quote, if, define, lambda, let, begin) plus function application. Lambdas capture the
/// environment they were created in, which gives closures and recursion for free.
/// </summary>
public sealed class TinyLisp
{
    private readonly LispEnvironment _global = new();

    public TinyLisp()
    {
        void Math2(string name, Func<double, double, object> f) =>
            _global.Define(name, (Func<object[], object>)(args => args.Skip(1).Aggregate((object)args[0], (acc, x) => f((double)acc, (double)x))));

        Math2("+", (a, b) => a + b);
        Math2("-", (a, b) => a - b);
        Math2("*", (a, b) => a * b);
        Math2("/", (a, b) => a / b);
        _global.Define("<", (Func<object[], object>)(a => (double)a[0] < (double)a[1]));
        _global.Define(">", (Func<object[], object>)(a => (double)a[0] > (double)a[1]));
        _global.Define("=", (Func<object[], object>)(a => Equals(a[0], a[1])));
        _global.Define("list", (Func<object[], object>)(a => a.ToList()));
        _global.Define("car", (Func<object[], object>)(a => ((List<object>)a[0])[0]));
        _global.Define("cdr", (Func<object[], object>)(a => ((List<object>)a[0]).Skip(1).ToList()));
        _global.Define("cons", (Func<object[], object>)(a => new List<object> { a[0] }.Concat((List<object>)a[1]).ToList()));
        _global.Define("null?", (Func<object[], object>)(a => a[0] is List<object> { Count: 0 }));
    }

    public object Run(string program)
    {
        var tokens = new Queue<string>(program.Replace("(", " ( ").Replace(")", " ) ").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        object result = new List<object>();
        while (tokens.Count > 0) result = Eval(Read(tokens), _global);
        return result;
    }

    public static string Print(object value) => value switch
    {
        List<object> list => $"({string.Join(' ', list.Select(Print))})",
        bool b => b ? "#t" : "#f",
        double d => d.ToString(CultureInfo.InvariantCulture),
        Lambda => "#<lambda>",
        _ => value.ToString() ?? "",
    };

    private static object Read(Queue<string> tokens)
    {
        if (tokens.Count == 0) throw new FormatException("Unexpected end of input.");
        var token = tokens.Dequeue();
        if (token == ")") throw new FormatException("Unexpected ')'.");
        if (token != "(") return double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n
            : token is "#t" or "#f" ? token == "#t" : new Symbol(token);

        var list = new List<object>();
        while (tokens.TryPeek(out var next) && next != ")") list.Add(Read(tokens));
        if (tokens.Count == 0) throw new FormatException("Missing ')'.");
        tokens.Dequeue();
        return list;
    }

    private object Eval(object x, LispEnvironment env)
    {
        while (true) // a loop instead of recursion for tail calls, so deep recursion in Lisp doesn't overflow C#'s stack
        {
            switch (x)
            {
                case Symbol s: return env.Get(s.Name);
                case not List<object>: return x;
            }

            var list = (List<object>)x;
            if (list.Count == 0) return list;

            switch (list[0])
            {
                case Symbol { Name: "quote" }: return list[1];
                case Symbol { Name: "if" }:
                    x = Eval(list[1], env) is false ? (list.Count > 3 ? list[3] : new List<object>()) : list[2];
                    continue;
                case Symbol { Name: "define" }:
                    env.Define(((Symbol)list[1]).Name, Eval(list[2], env));
                    return list[1];
                case Symbol { Name: "lambda" }:
                    return new Lambda(((List<object>)list[1]).Cast<Symbol>().ToList(), list[2], env);
                case Symbol { Name: "begin" }:
                    foreach (var e in list.Skip(1).SkipLast(1)) Eval(e, env);
                    x = list[^1];
                    continue;
                case Symbol { Name: "let" }:
                    var scope = new LispEnvironment(env);
                    foreach (List<object> binding in (List<object>)list[1]) scope.Define(((Symbol)binding[0]).Name, Eval(binding[1], env));
                    (x, env) = (list[2], scope);
                    continue;
            }

            var function = Eval(list[0], env);
            var args = list.Skip(1).Select(a => Eval(a, env)).ToArray();
            switch (function)
            {
                case Func<object[], object> builtin: return builtin(args);
                case Lambda lambda:
                    if (args.Length != lambda.Parameters.Count) throw new ArgumentException($"Expected {lambda.Parameters.Count} arguments, got {args.Length}.");
                    env = new LispEnvironment(lambda.Closure);
                    for (var i = 0; i < args.Length; i++) env.Define(lambda.Parameters[i].Name, args[i]);
                    x = lambda.Body;
                    continue;
                default: throw new InvalidOperationException($"'{Print(function)}' is not a function.");
            }
        }
    }
}
