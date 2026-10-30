namespace CSharpExercises.Patterns;

/// <summary>
/// Command: each operation is an object that knows how to do and undo itself. The history only sees the interface,
/// so undo/redo, macros (a command made of commands) and logging work for any operation, including ones added later.
/// </summary>
public interface IUndoableCommand
{
    string Description { get; }
    void Execute();
    void Undo();
}

public sealed class Calculator
{
    public decimal Value { get; set; }
}

public sealed class AddCommand(Calculator calculator, decimal amount) : IUndoableCommand
{
    public string Description => $"add {amount}";
    public void Execute() => calculator.Value += amount;
    public void Undo() => calculator.Value -= amount;
}

/// <summary>Multiplying by zero can't be undone by dividing, so this command remembers the previous value instead.</summary>
public sealed class MultiplyCommand(Calculator calculator, decimal factor) : IUndoableCommand
{
    private decimal _before;
    public string Description => $"multiply by {factor}";
    public void Execute()
    {
        _before = calculator.Value;
        calculator.Value *= factor;
    }
    public void Undo() => calculator.Value = _before;
}

/// <summary>A composite command: undone as one step, in reverse order.</summary>
public sealed class MacroCommand(string name, params IUndoableCommand[] steps) : IUndoableCommand
{
    public string Description => $"{name} ({string.Join(", ", steps.Select(s => s.Description))})";
    public void Execute()
    {
        foreach (var step in steps) step.Execute();
    }
    public void Undo()
    {
        for (var i = steps.Length - 1; i >= 0; i--) steps[i].Undo();
    }
}

public sealed class CommandHistory
{
    private readonly Stack<IUndoableCommand> _undo = new();
    private readonly Stack<IUndoableCommand> _redo = new();

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public IEnumerable<string> Log => _undo.Reverse().Select(c => c.Description);

    public void Execute(IUndoableCommand command)
    {
        command.Execute();
        _undo.Push(command);
        _redo.Clear(); // a new action makes the redo branch meaningless
    }

    public void Undo()
    {
        if (!_undo.TryPop(out var command)) throw new InvalidOperationException("Nothing to undo.");
        command.Undo();
        _redo.Push(command);
    }

    public void Redo()
    {
        if (!_redo.TryPop(out var command)) throw new InvalidOperationException("Nothing to redo.");
        command.Execute();
        _undo.Push(command);
    }
}
