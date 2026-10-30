namespace CSharpExercises.Interpreters;

public enum OpCode { Push, Pop, Dup, Swap, Add, Sub, Mul, Div, Mod, Lt, Eq, Load, Store, Jump, JumpIfZero, Call, Return, Print, Halt }

public readonly record struct Instruction(OpCode Op, long Operand = 0);

/// <summary>
/// A bytecode virtual machine like the JVM or CLR at heart: instructions operate on a value stack, with a program
/// counter, local variable slots, a call stack for subroutines, and jumps for loops and conditionals. An assembler turns
/// readable text with labels into instructions. A step limit stops runaway programs.
/// </summary>
public sealed class StackMachine(int maxSteps = 1_000_000)
{
    private readonly long[] _variables = new long[16];

    public List<long> Output { get; } = [];
    public int Steps { get; private set; }

    public void Run(IReadOnlyList<Instruction> program)
    {
        var stack = new Stack<long>();
        var calls = new Stack<int>();
        var pc = 0;
        Steps = 0;

        long Pop() => stack.Count > 0 ? stack.Pop() : throw new InvalidOperationException($"Stack underflow at {pc - 1}.");

        while (pc < program.Count)
        {
            if (++Steps > maxSteps) throw new TimeoutException($"Gave up after {maxSteps} steps.");
            var (op, operand) = program[pc++];
            switch (op)
            {
                case OpCode.Push: stack.Push(operand); break;
                case OpCode.Pop: Pop(); break;
                case OpCode.Dup: var top = Pop(); stack.Push(top); stack.Push(top); break;
                case OpCode.Swap: var (x, y) = (Pop(), Pop()); stack.Push(x); stack.Push(y); break;
                case OpCode.Add or OpCode.Sub or OpCode.Mul or OpCode.Div or OpCode.Mod or OpCode.Lt or OpCode.Eq:
                    var (b, a) = (Pop(), Pop());
                    stack.Push(op switch
                    {
                        OpCode.Add => a + b, OpCode.Sub => a - b, OpCode.Mul => a * b,
                        OpCode.Div => b == 0 ? throw new DivideByZeroException() : a / b,
                        OpCode.Mod => a % b, OpCode.Lt => a < b ? 1 : 0, _ => a == b ? 1 : 0,
                    });
                    break;
                case OpCode.Load: stack.Push(_variables[operand]); break;
                case OpCode.Store: _variables[operand] = Pop(); break;
                case OpCode.Jump: pc = (int)operand; break;
                case OpCode.JumpIfZero: if (Pop() == 0) pc = (int)operand; break;
                case OpCode.Call: calls.Push(pc); pc = (int)operand; break;
                case OpCode.Return: pc = calls.Count > 0 ? calls.Pop() : throw new InvalidOperationException("Return without call."); break;
                case OpCode.Print: Output.Add(Pop()); break;
                case OpCode.Halt: return;
            }
        }
    }

    /// <summary>
    /// Two passes: first record the address of every "label:" line, then translate instructions, replacing label
    /// operands with addresses. Comments start with ';'.
    /// </summary>
    public static IReadOnlyList<Instruction> Assemble(string source)
    {
        var lines = source.Split('\n')
            .Select(l => l.Split(';')[0].Trim())
            .Where(l => l.Length > 0)
            .ToList();

        var labels = new Dictionary<string, int>();
        var address = 0;
        foreach (var line in lines)
        {
            if (line.EndsWith(':')) labels[line[..^1]] = address;
            else address++;
        }

        return lines.Where(l => !l.EndsWith(':')).Select(line =>
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (!Enum.TryParse<OpCode>(parts[0], ignoreCase: true, out var op)) throw new FormatException($"Unknown instruction '{parts[0]}'.");
            if (parts.Length == 1) return new Instruction(op);
            var operand = long.TryParse(parts[1], out var n) ? n
                : labels.TryGetValue(parts[1], out var target) ? target
                : throw new FormatException($"Unknown label '{parts[1]}'.");
            return new Instruction(op, operand);
        }).ToList();
    }
}
