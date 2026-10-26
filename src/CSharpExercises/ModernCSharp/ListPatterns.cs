namespace CSharpExercises.ModernCSharp;

public abstract record Command;
public sealed record MoveCommand(string Direction, int Steps) : Command;
public sealed record SayCommand(string Message) : Command;
public sealed record QuitCommand : Command;
public sealed record UnknownCommand(string Input) : Command;

/// <summary>
/// List patterns match on the shape of a sequence: its length, specific elements, and the rest via <c>..</c>.
/// </summary>
public static class ListPatterns
{
    public static string Describe(int[] values) => values switch
    {
        [] => "empty",
        [var only] => $"one item: {only}",
        [var first, var second] => $"a pair: {first} and {second}",
        [var first, .., var last] when first == last => $"{values.Length} items, starting and ending with {first}",
        [var first, .., var last] => $"{values.Length} items, from {first} to {last}"
    };

    /// <summary>Parses text commands like "move north 3", "say hello there" or "quit". Commands are lowercase.</summary>
    public static Command ParseCommand(string input)
    {
        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return words switch
        {
            ["move", ("north" or "south" or "east" or "west") and var direction] => new MoveCommand(direction, 1),
            ["move", ("north" or "south" or "east" or "west") and var direction, var steps]
                when int.TryParse(steps, out var count) && count > 0 => new MoveCommand(direction, count),
            ["say", .. var rest] when rest.Length > 0 => new SayCommand(string.Join(' ', rest)),
            ["quit" or "exit"] => new QuitCommand(),
            _ => new UnknownCommand(input)
        };
    }

    /// <summary>
    /// Recursive definition, written directly as patterns: an empty or single-item list is sorted;
    /// a longer one is sorted if its first two items are in order and the rest is sorted.
    /// (A loop avoids deep recursion on large inputs; this is for illustration.)
    /// </summary>
    public static bool IsSorted(ReadOnlySpan<int> values) => values switch
    {
        [] or [_] => true,
        [var a, var b, ..] => a <= b && IsSorted(values[1..])
    };
}
