using System.Text;

namespace CSharpExercises.Interpreters;

/// <summary>
/// An interpreter for the eight-instruction esoteric language: a tape of byte cells, a data pointer, + - &lt; &gt; to
/// change them, . and , for output and input, and [ ] for loops. Matching brackets are paired once up front, so jumps are
/// O(1), and runs of + - &lt; &gt; are folded into single steps: the same two ideas a real compiler's front end uses.
/// </summary>
public static class Brainfuck
{
    private readonly record struct Op(char Code, int Argument);

    public static string Run(string program, string input = "", int tapeSize = 30_000, long maxSteps = 10_000_000)
    {
        var ops = Compile(program);
        var tape = new byte[tapeSize];
        var output = new StringBuilder();
        var (pointer, inputIndex, steps) = (0, 0, 0L);

        for (var pc = 0; pc < ops.Length; pc++)
        {
            if (++steps > maxSteps) throw new TimeoutException("Program ran too long.");
            var (code, arg) = ops[pc];
            switch (code)
            {
                case '+': tape[pointer] = (byte)(tape[pointer] + arg); break; // wraps around at 256
                case '>':
                    pointer += arg;
                    if (pointer < 0 || pointer >= tapeSize) throw new IndexOutOfRangeException("Moved off the tape.");
                    break;
                case '.': output.Append((char)tape[pointer]); break;
                case ',': tape[pointer] = inputIndex < input.Length ? (byte)input[inputIndex++] : (byte)0; break;
                case '[': if (tape[pointer] == 0) pc = arg; break;
                case ']': if (tape[pointer] != 0) pc = arg; break;
                case '0': tape[pointer] = 0; break; // the "[-]" idiom
            }
        }
        return output.ToString();
    }

    /// <summary>Folds repeats, recognises [-] as "clear", and pairs brackets with a stack.</summary>
    private static Op[] Compile(string program)
    {
        var code = new string(program.Where(c => "+-<>.,[]".Contains(c)).ToArray()).Replace("[-]", "0");
        var ops = new List<Op>();
        var open = new Stack<int>();

        for (var i = 0; i < code.Length; i++)
        {
            var c = code[i];
            if (c is '+' or '-' or '<' or '>')
            {
                var sign = c is '+' or '>' ? 1 : -1;
                var kind = c is '+' or '-' ? '+' : '>';
                var total = sign;
                while (i + 1 < code.Length && code[i + 1] is var n && (kind == '+' ? n is '+' or '-' : n is '<' or '>'))
                {
                    total += n is '+' or '>' ? 1 : -1;
                    i++;
                }
                ops.Add(new Op(kind, total));
                continue;
            }
            if (c == '[') open.Push(ops.Count);
            if (c == ']')
            {
                if (!open.TryPop(out var start)) throw new FormatException($"Unmatched ']' at {i}.");
                ops[start] = new Op('[', ops.Count);
                ops.Add(new Op(']', start));
                continue;
            }
            ops.Add(new Op(c, 0));
        }
        if (open.Count > 0) throw new FormatException("Unmatched '['.");
        return [.. ops];
    }
}
