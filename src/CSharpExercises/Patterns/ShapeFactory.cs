using System.Globalization;
using CSharpExercises.Oop;

namespace CSharpExercises.Patterns;

/// <summary>
/// Factory: callers ask for a shape by name (e.g. from a config file or user input) and never call a constructor themselves.
/// New shapes are added with <see cref="Register"/>, without changing the factory's code.
/// </summary>
public class ShapeFactory
{
    private readonly Dictionary<string, (int ParameterCount, Func<double[], Shape> Create)> _creators =
        new(StringComparer.OrdinalIgnoreCase);

    public ShapeFactory()
    {
        Register("circle", 1, a => new Circle(a[0]));
        Register("rectangle", 2, a => new Rectangle(a[0], a[1]));
        Register("square", 1, a => new Square(a[0]));
    }

    public IEnumerable<string> Names => _creators.Keys;

    public void Register(string name, int parameterCount, Func<double[], Shape> create) =>
        _creators[name] = (parameterCount, create);

    /// <summary>Creates a shape from text such as "circle 2.5" or "rectangle 3 4".</summary>
    public Shape Create(string description)
    {
        var parts = description.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || !_creators.TryGetValue(parts[0], out var creator))
        {
            throw new ArgumentException($"Unknown shape: '{description}'.", nameof(description));
        }

        if (parts.Length - 1 != creator.ParameterCount)
        {
            throw new ArgumentException($"'{parts[0]}' needs {creator.ParameterCount} value(s).", nameof(description));
        }

        var args = new double[creator.ParameterCount];
        for (var i = 0; i < args.Length; i++)
        {
            if (!double.TryParse(parts[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out args[i]))
            {
                throw new ArgumentException($"'{parts[i + 1]}' is not a number.", nameof(description));
            }
        }

        return creator.Create(args);
    }
}
