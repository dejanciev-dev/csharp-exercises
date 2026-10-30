namespace CSharpExercises.Interpreters;

/// <summary>
/// A regular expression engine in Thompson's style, supporting literals, '.', '*', '+', '?', '|' and parentheses.
/// The pattern compiles to an NFA, and matching tracks the set of states the automaton could be in, advancing all of
/// them per input character. That is O(pattern × text) in the worst case, where backtracking engines (like .NET's
/// default) can take exponential time on patterns such as (a*)*b ("catastrophic backtracking").
/// </summary>
public sealed class TinyRegex
{
    private sealed class State
    {
        public char? Match;       // null: a split or the final state
        public bool AnyChar;
        public State? Out, Out1;  // Out1 is only used by splits
        public bool IsFinal;
    }

    private sealed record Fragment(State Start, List<Action<State>> Dangling);

    private readonly State _start;

    public TinyRegex(string pattern)
    {
        var postfix = ToPostfix(pattern);
        var stack = new Stack<Fragment>();
        Fragment Pop() => stack.Count > 0 ? stack.Pop() : throw new FormatException($"Invalid pattern '{pattern}'.");

        foreach (var c in postfix)
        {
            switch (c)
            {
                case '\u0001': // explicit concatenation
                {
                    var (b, a) = (Pop(), Pop());
                    Patch(a, b.Start);
                    stack.Push(new Fragment(a.Start, b.Dangling));
                    break;
                }
                case '|':
                {
                    var (b, a) = (Pop(), Pop());
                    stack.Push(new Fragment(new State { Out = a.Start, Out1 = b.Start }, [.. a.Dangling, .. b.Dangling]));
                    break;
                }
                case '*' or '+' or '?':
                {
                    var e = Pop();
                    var split = new State { Out = e.Start };
                    if (c != '?') Patch(e, split); // loop back
                    var dangling = new List<Action<State>> { s => split.Out1 = s };
                    if (c == '?') dangling.AddRange(e.Dangling);
                    stack.Push(new Fragment(c == '+' ? e.Start : split, dangling));
                    break;
                }
                default:
                {
                    var state = new State { Match = c, AnyChar = c == '.' };
                    stack.Push(new Fragment(state, [s => state.Out = s]));
                    break;
                }
            }
        }

        var whole = Pop();
        if (stack.Count > 0) throw new FormatException($"Invalid pattern '{pattern}'.");
        Patch(whole, new State { IsFinal = true });
        _start = whole.Start;
    }

    /// <summary>True if the whole input matches the pattern.</summary>
    public bool IsMatch(string input)
    {
        var current = Closure([_start]);
        foreach (var c in input)
        {
            current = Closure(current.Where(s => s.Match is not null && (s.AnyChar || s.Match == c)).Select(s => s.Out!));
            if (current.Count == 0) return false;
        }
        return current.Any(s => s.IsFinal);
    }

    /// <summary>Follows splits without consuming input; the set handles loops that lead back to themselves.</summary>
    private static HashSet<State> Closure(IEnumerable<State> states)
    {
        var result = new HashSet<State>();
        var pending = new Stack<State>(states);
        while (pending.TryPop(out var s))
        {
            if (!result.Add(s)) continue;
            if (s.Match is null && !s.IsFinal)
            {
                if (s.Out is not null) pending.Push(s.Out);
                if (s.Out1 is not null) pending.Push(s.Out1);
            }
        }
        return result;
    }

    private static void Patch(Fragment fragment, State target)
    {
        foreach (var connect in fragment.Dangling) connect(target);
    }

    /// <summary>Inserts explicit concatenation operators, then converts to postfix with the shunting-yard algorithm.</summary>
    private static string ToPostfix(string pattern)
    {
        const char Concat = '\u0001';
        var withConcat = new List<char>();
        for (var i = 0; i < pattern.Length; i++)
        {
            withConcat.Add(pattern[i]);
            if (i + 1 < pattern.Length && pattern[i] is not ('(' or '|') && pattern[i + 1] is not (')' or '|' or '*' or '+' or '?'))
                withConcat.Add(Concat);
        }

        int Precedence(char op) => op switch { '|' => 1, Concat => 2, _ => 3 };
        var output = new List<char>();
        var ops = new Stack<char>();
        foreach (var c in withConcat)
        {
            switch (c)
            {
                case '(': ops.Push(c); break;
                case ')':
                    while (ops.Count > 0 && ops.Peek() != '(') output.Add(ops.Pop());
                    if (ops.Count == 0) throw new FormatException("Unbalanced ')'.");
                    ops.Pop();
                    break;
                case '*' or '+' or '?': output.Add(c); break; // postfix operators bind tightest
                case '|' or Concat:
                    while (ops.Count > 0 && ops.Peek() != '(' && Precedence(ops.Peek()) >= Precedence(c)) output.Add(ops.Pop());
                    ops.Push(c);
                    break;
                default: output.Add(c); break;
            }
        }
        while (ops.Count > 0)
        {
            var op = ops.Pop();
            if (op == '(') throw new FormatException("Unbalanced '('.");
            output.Add(op);
        }
        return new string([.. output]);
    }
}
