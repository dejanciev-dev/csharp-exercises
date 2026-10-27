using System.Reflection;
using CSharpExercises.Strings;

namespace CSharpExercises.Reflection;

public interface ICommand
{
    string Execute(IReadOnlyList<string> args);
}

/// <summary>Marks a class as a command that <see cref="CommandRegistry"/> can discover.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class CommandAttribute(string name) : Attribute
{
    public string Name => name;
    public string Description { get; init; } = "";
}

/// <summary>
/// Plugin-style discovery: scans assemblies for <see cref="ICommand"/> classes marked with <see cref="CommandAttribute"/>,
/// so adding a command means adding a class, with no list to update.
/// </summary>
public sealed class CommandRegistry
{
    private readonly Dictionary<string, (Type Type, string Description)> _commands = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Registers the public commands in <paramref name="assemblies"/>; internal and private classes stay hidden.</summary>
    public static CommandRegistry FromAssemblies(params IEnumerable<Assembly> assemblies) =>
        FromTypes(assemblies.SelectMany(a => a.GetExportedTypes()));

    public static CommandRegistry FromTypes(params IEnumerable<Type> types)
    {
        var registry = new CommandRegistry();

        var found = types
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(ICommand).IsAssignableFrom(t))
            .Select(t => (Type: t, Attribute: t.GetCustomAttribute<CommandAttribute>()))
            .Where(x => x.Attribute is not null);

        foreach (var (type, attribute) in found)
        {
            if (type.GetConstructor(Type.EmptyTypes) is null)
                throw new InvalidOperationException($"Command {type.Name} needs a public parameterless constructor.");

            if (!registry._commands.TryAdd(attribute!.Name, (type, attribute.Description)))
                throw new InvalidOperationException(
                    $"Command '{attribute.Name}' is defined by both {registry._commands[attribute.Name].Type.Name} and {type.Name}.");
        }

        return registry;
    }

    public IReadOnlyDictionary<string, string> Help =>
        _commands.OrderBy(c => c.Key).ToDictionary(c => c.Key, c => c.Value.Description);

    /// <summary>Runs a line such as <c>"reverse hello world"</c>: the first word picks the command, the rest are its arguments.</summary>
    public string Run(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts is not [var name, .. var args]) throw new ArgumentException("No command given.", nameof(line));

        if (!_commands.TryGetValue(name, out var command))
            throw new KeyNotFoundException($"Unknown command '{name}'. Known commands: {string.Join(", ", Help.Keys)}.");

        var instance = (ICommand)Activator.CreateInstance(command.Type)!;
        return instance.Execute(args);
    }
}

[Command("echo", Description = "Prints its arguments.")]
public sealed class EchoCommand : ICommand
{
    public string Execute(IReadOnlyList<string> args) => string.Join(' ', args);
}

[Command("reverse", Description = "Reverses each argument.")]
public sealed class ReverseCommand : ICommand
{
    public string Execute(IReadOnlyList<string> args) => string.Join(' ', args.Select(StringExercises.Reverse));
}

[Command("sum", Description = "Adds up whole numbers.")]
public sealed class SumCommand : ICommand
{
    public string Execute(IReadOnlyList<string> args) => args.Sum(long.Parse).ToString();
}
