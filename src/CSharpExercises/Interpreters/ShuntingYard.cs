using System.Globalization;

namespace CSharpExercises.Interpreters;

/// <summary>
/// Dijkstra's shunting-yard algorithm converts infix ("3 + 4 * 2") to reverse Polish notation ("3 4 2 * +") with one
/// operator stack: an incoming operator first pops every stacked operator that binds tighter (or as tight, for
/// left-associative ones). RPN then evaluates with a single value stack and no parentheses or precedence rules.
/// </summary>
public static class ShuntingYard
{
    private static readonly Dictionary<string, (int Precedence, bool RightAssociative)> Operators = new()
    {
        ["+"] = (1, false), ["-"] = (1, false), ["*"] = (2, false), ["/"] = (2, false), ["%"] = (2, false),
        ["^"] = (4, true),  // 2 ^ 3 ^ 2 = 2 ^ 9
        ["neg"] = (3, true), // unary minus: binds tighter than * but looser than ^, so -2^2 = -4
    };

    private static readonly HashSet<string> Functions = ["sqrt", "max", "min", "abs"];

    public static IReadOnlyList<string> ToRpn(string expression)
    {
        var output = new List<string>();
        var stack = new Stack<string>();
        string? previous = null;

        foreach (var token in Tokenize(expression))
        {
            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                output.Add(token);
            }
            else if (Functions.Contains(token) || token == "(")
            {
                stack.Push(token);
            }
            else if (token == ",")
            {
                while (stack.Count > 0 && stack.Peek() != "(") output.Add(stack.Pop());
            }
            else if (token == ")")
            {
                while (stack.Count > 0 && stack.Peek() != "(") output.Add(stack.Pop());
                if (stack.Count == 0) throw new FormatException("Mismatched ')'.");
                stack.Pop();
                if (stack.Count > 0 && Functions.Contains(stack.Peek())) output.Add(stack.Pop());
            }
            else if (Operators.ContainsKey(token))
            {
                // A minus at the start, or after an operator or '(', is unary.
                var op = token == "-" && (previous is null or "(" or "," || Operators.ContainsKey(previous)) ? "neg" : token;
                var (precedence, right) = Operators[op];
                while (stack.Count > 0 && Operators.TryGetValue(stack.Peek(), out var top) &&
                       (top.Precedence > precedence || (top.Precedence == precedence && !right)))
                    output.Add(stack.Pop());
                stack.Push(op);
                previous = op;
                continue;
            }
            else throw new FormatException($"Unknown token '{token}'.");
            previous = token;
        }

        while (stack.Count > 0)
        {
            var op = stack.Pop();
            if (op == "(") throw new FormatException("Mismatched '('.");
            output.Add(op);
        }
        return output;
    }

    public static double EvaluateRpn(IEnumerable<string> rpn)
    {
        var stack = new Stack<double>();
        double Pop() => stack.Count > 0 ? stack.Pop() : throw new FormatException("Not enough operands.");

        foreach (var token in rpn)
        {
            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            {
                stack.Push(number);
                continue;
            }
            if (token is "neg" or "sqrt" or "abs")
            {
                var x = Pop();
                stack.Push(token switch { "neg" => -x, "sqrt" => Math.Sqrt(x), _ => Math.Abs(x) });
                continue;
            }
            var (b, a) = (Pop(), Pop());
            stack.Push(token switch
            {
                "+" => a + b, "-" => a - b, "*" => a * b, "/" => a / b, "%" => a % b,
                "^" => Math.Pow(a, b), "max" => Math.Max(a, b), "min" => Math.Min(a, b),
                _ => throw new FormatException($"Unknown operator '{token}'."),
            });
        }
        if (stack.Count != 1) throw new FormatException("Too many operands.");
        return stack.Pop();
    }

    public static double Evaluate(string expression) => EvaluateRpn(ToRpn(expression));

    private static IEnumerable<string> Tokenize(string s)
    {
        for (var i = 0; i < s.Length;)
        {
            if (char.IsWhiteSpace(s[i])) { i++; continue; }
            var start = i;
            if (char.IsAsciiDigit(s[i]) || s[i] == '.')
                while (i < s.Length && (char.IsAsciiDigit(s[i]) || s[i] == '.')) i++;
            else if (char.IsAsciiLetter(s[i]))
                while (i < s.Length && char.IsAsciiLetter(s[i])) i++;
            else i++;
            yield return s[start..i];
        }
    }
}
