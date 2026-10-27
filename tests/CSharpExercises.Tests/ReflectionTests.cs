using System.Reflection;
using CSharpExercises.Reflection;

namespace CSharpExercises.Tests;

/// <summary>Lives in the test assembly, to show commands being discovered across assemblies.</summary>
[Command("shout", Description = "Upper-cases its arguments.")]
public sealed class ShoutCommand : ICommand
{
    public string Execute(IReadOnlyList<string> args) => string.Join(' ', args).ToUpperInvariant() + "!";
}

public class ReflectionTests
{
    private sealed class Person
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public static string Species { get; set; } = "human";
        public string ReadOnly => "fixed";
    }

    private sealed class Product
    {
        [Required, MaxLength(10)]
        public string? Name { get; init; }

        [Range(0.01, 1_000)]
        public decimal Price { get; init; }

        [Range(0, 100)]
        public int? Discount { get; init; }

        public string? NotValidated { get; init; }
    }

    [Command("echo")] private sealed class SecondEcho : ICommand { public string Execute(IReadOnlyList<string> args) => ""; }
    [Command("needs-args")] private sealed class NoDefaultConstructor(int x) : ICommand { public string Execute(IReadOnlyList<string> args) => $"{x}"; }

    private static PropertyInfo PersonProperty(string name) =>
        typeof(Person).GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)!;

    [Fact]
    public void CreateGetter_ReadsInstanceProperties()
    {
        var getName = PropertyAccessor.CreateGetter(PersonProperty(nameof(Person.Name)));
        var getAge = PropertyAccessor.CreateGetter(PersonProperty(nameof(Person.Age)));
        var person = new Person { Name = "Ana", Age = 30 };

        Assert.Equal("Ana", getName(person));
        Assert.Equal(30, getAge(person)); // value types come back boxed
    }

    [Fact]
    public void CreateSetter_WritesInstanceProperties()
    {
        var setAge = PropertyAccessor.CreateSetter(PersonProperty(nameof(Person.Age)));
        var person = new Person();

        setAge(person, 42);

        Assert.Equal(42, person.Age);
    }

    [Fact]
    public void Accessors_WorkForStaticProperties()
    {
        var property = PersonProperty(nameof(Person.Species));
        var original = Person.Species;
        try
        {
            PropertyAccessor.CreateSetter(property)(null!, "robot");
            Assert.Equal("robot", PropertyAccessor.CreateGetter(property)(null!));
        }
        finally
        {
            Person.Species = original;
        }
    }

    [Fact]
    public void CreateSetter_RejectsReadOnlyProperties()
    {
        Assert.Throws<ArgumentException>(() => PropertyAccessor.CreateSetter(PersonProperty(nameof(Person.ReadOnly))));
    }

    [Fact]
    public void CreateSetter_WithWrongValueType_Throws()
    {
        var setAge = PropertyAccessor.CreateSetter(PersonProperty(nameof(Person.Age)));

        Assert.Throws<InvalidCastException>(() => setAge(new Person(), "not a number"));
    }

    [Fact]
    public void Validate_ValidObject_HasNoErrors()
    {
        Assert.Empty(AttributeValidator.Validate(new Product { Name = "Lamp", Price = 25m, Discount = 10 }));
    }

    [Fact]
    public void Validate_ReportsEveryBrokenRule()
    {
        var errors = AttributeValidator.Validate(new Product { Name = " ", Price = 0, Discount = 150 });

        Assert.Equal(
            [
                "Name is required.",
                "Price must be between 0.01 and 1000.",
                "Discount must be between 0 and 100.",
            ],
            errors);
    }

    [Fact]
    public void Validate_NullOptionalNumber_SkipsRange()
    {
        Assert.Empty(AttributeValidator.Validate(new Product { Name = "Lamp", Price = 1m, Discount = null }));
    }

    [Fact]
    public void Validate_MaxLength()
    {
        Assert.Equal(["Name must be at most 10 characters."],
            AttributeValidator.Validate(new Product { Name = "Extra long name", Price = 1m }));
    }

    [Fact]
    public void Validate_ObjectWithoutRules_IsValid()
    {
        Assert.Empty(AttributeValidator.Validate(new Person()));
    }

    [Fact]
    public void Registry_DiscoversCommandsAcrossAssemblies()
    {
        var registry = CommandRegistry.FromAssemblies(typeof(ICommand).Assembly, typeof(ShoutCommand).Assembly)
            .Help.Keys;

        Assert.Equal(["echo", "reverse", "shout", "sum"], registry.Where(k => k is "echo" or "reverse" or "shout" or "sum"));
    }

    [Theory]
    [InlineData("echo hello   world", "hello world")]
    [InlineData("REVERSE abc xyz", "cba zyx")]
    [InlineData("sum 1 2 39", "42")]
    [InlineData("shout hi", "HI!")]
    public void Registry_RunsCommandsByName(string line, string expected)
    {
        var registry = CommandRegistry.FromTypes(typeof(EchoCommand), typeof(ReverseCommand), typeof(SumCommand), typeof(ShoutCommand));

        Assert.Equal(expected, registry.Run(line));
    }

    [Fact]
    public void Registry_IgnoresTypesWithoutTheAttributeOrInterface()
    {
        var registry = CommandRegistry.FromTypes(typeof(EchoCommand), typeof(Person), typeof(string));

        Assert.Equal(["echo"], registry.Help.Keys);
    }

    [Fact]
    public void Registry_UnknownCommand_ListsTheKnownOnes()
    {
        var registry = CommandRegistry.FromTypes(typeof(EchoCommand), typeof(SumCommand));

        var ex = Assert.Throws<KeyNotFoundException>(() => registry.Run("fly away"));
        Assert.Contains("echo, sum", ex.Message);
    }

    [Fact]
    public void Registry_RejectsDuplicateNames()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => CommandRegistry.FromTypes(typeof(EchoCommand), typeof(SecondEcho)));
        Assert.Contains("'echo'", ex.Message);
    }

    [Fact]
    public void Registry_RejectsCommandsItCannotCreate()
    {
        Assert.Throws<InvalidOperationException>(() => CommandRegistry.FromTypes(typeof(NoDefaultConstructor)));
    }
}
