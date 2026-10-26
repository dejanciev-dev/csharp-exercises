using System.Numerics;
using CSharpExercises.ModernCSharp;

namespace CSharpExercises.Tests;

public class ModernCSharpTests
{
    private static Order SampleOrder() =>
        new Order(1, "Ana")
            .AddLine(new OrderLine("BOOK", 2, 20m))
            .AddLine(new OrderLine("PEN", 3, 2.5m));

    [Fact]
    public void Records_WithCreatesCopyAndLeavesOriginalUntouched()
    {
        var draft = SampleOrder();
        var placed = draft.Place();

        Assert.Equal(OrderStatus.Draft, draft.Status);
        Assert.Equal(OrderStatus.Placed, placed.Status);
        Assert.Equal(47.5m, placed.Total);
        Assert.Equal(2, draft.Lines.Length);
    }

    [Fact]
    public void Records_HaveValueEquality_IncludingTheirLines()
    {
        Assert.Equal(SampleOrder(), SampleOrder());
        Assert.Equal(SampleOrder().GetHashCode(), SampleOrder().GetHashCode());
        Assert.NotEqual(SampleOrder(), SampleOrder().AddLine(new OrderLine("INK", 1, 5m)));
        Assert.NotEqual(SampleOrder(), SampleOrder() with { Customer = "Marko" });
    }

    [Fact]
    public void Records_EnforceValidStatusChanges()
    {
        var shipped = SampleOrder().Place().Ship();

        Assert.Throws<InvalidOperationException>(() => shipped.Cancel());
        Assert.Throws<InvalidOperationException>(() => shipped.AddLine(new OrderLine("X", 1, 1m)));
        Assert.Throws<InvalidOperationException>(() => new Order(2, "Empty").Place());
        Assert.Throws<InvalidOperationException>(() => SampleOrder().Ship());
        Assert.Equal(OrderStatus.Cancelled, SampleOrder().Place().Cancel().Status);
    }

    [Theory]
    [InlineData(new int[0], "empty")]
    [InlineData(new[] { 7 }, "one item: 7")]
    [InlineData(new[] { 1, 2 }, "a pair: 1 and 2")]
    [InlineData(new[] { 4, 9, 4 }, "3 items, starting and ending with 4")]
    [InlineData(new[] { 1, 2, 3, 4 }, "4 items, from 1 to 4")]
    public void ListPatterns_Describe(int[] values, string expected) =>
        Assert.Equal(expected, ListPatterns.Describe(values));

    [Fact]
    public void ListPatterns_ParseCommand()
    {
        Assert.Equal(new MoveCommand("north", 1), ListPatterns.ParseCommand("move north"));
        Assert.Equal(new MoveCommand("west", 3), ListPatterns.ParseCommand("  move   west 3 "));
        Assert.Equal(new SayCommand("hello there"), ListPatterns.ParseCommand("say hello there"));
        Assert.Equal(new QuitCommand(), ListPatterns.ParseCommand("exit"));
    }

    [Theory]
    [InlineData("move up")]
    [InlineData("move north zero")]
    [InlineData("move north -2")]
    [InlineData("say")]
    [InlineData("quit now")]
    [InlineData("")]
    public void ListPatterns_UnknownCommands(string input) =>
        Assert.Equal(new UnknownCommand(input), ListPatterns.ParseCommand(input));

    [Theory]
    [InlineData(new int[0], true)]
    [InlineData(new[] { 5 }, true)]
    [InlineData(new[] { 1, 2, 2, 8 }, true)]
    [InlineData(new[] { 1, 3, 2 }, false)]
    public void ListPatterns_IsSorted(int[] values, bool expected) =>
        Assert.Equal(expected, ListPatterns.IsSorted(values));

    [Fact]
    public void GenericMath_SumWorksForManyTypes()
    {
        Assert.Equal(6, GenericMath.Sum([1, 2, 3]));
        Assert.Equal(0.6m, GenericMath.Sum([0.1m, 0.2m, 0.3m]));
        Assert.Equal(BigInteger.Parse("200000000000000000000"),
            GenericMath.Sum([BigInteger.Parse("100000000000000000000"), BigInteger.Parse("100000000000000000000")]));
    }

    [Fact]
    public void GenericMath_SumWorksForCustomTypes()
    {
        Vector2D<int>[] moves = [new(1, 2), new(3, -1), new(0, 4)];

        Assert.Equal(new Vector2D<int>(4, 5), GenericMath.Sum(moves));
        Assert.Equal(new Vector2D<double>(1.5, 3), new Vector2D<double>(0.5, 1) * 3);
        Assert.Equal(11, new Vector2D<int>(1, 2).Dot(new Vector2D<int>(3, 4)));
    }

    [Fact]
    public void GenericMath_Average()
    {
        Assert.Equal(2, GenericMath.Average([1, 2, 4])); // integer division: 7 / 3
        Assert.Equal(2.5, GenericMath.Average([1.0, 2.0, 3.0, 4.0]));
        Assert.Throws<InvalidOperationException>(() => GenericMath.Average(Array.Empty<int>()));
    }

    [Fact]
    public void GenericMath_Gcd()
    {
        Assert.Equal(6, GenericMath.Gcd(48, -18));
        Assert.Equal(21L, GenericMath.Gcd(1071L, 462L));
        Assert.Equal(new BigInteger(1_000_000), GenericMath.Gcd(BigInteger.Pow(10, 30), new BigInteger(3_000_000)));
    }

    [Fact]
    public void GenericMath_StandardDeviation()
    {
        Assert.Equal(2.0, GenericMath.StandardDeviation([2.0, 4, 4, 4, 5, 5, 7, 9]), precision: 10);
        Assert.Equal(2.0f, GenericMath.StandardDeviation([2.0f, 4, 4, 4, 5, 5, 7, 9]), precision: 5);
    }

    [Theory]
    [InlineData("10,20,30", 60)]
    [InlineData(" 1 , 2 ,, 3 ", 6)]
    [InlineData("", 0)]
    [InlineData("-5,5", 0)]
    public void Span_SumCsv(string line, int expected) =>
        Assert.Equal(expected, SpanExercises.SumCsv(line));

    [Theory]
    [InlineData("hello world", 2)]
    [InlineData("  spaced   out\ttext\n", 3)]
    [InlineData("", 0)]
    public void Span_CountWords(string text, int expected) =>
        Assert.Equal(expected, SpanExercises.CountWords(text));

    [Fact]
    public void Span_ParsingDoesNotAllocate()
    {
        const string line = "12, 34, 56, 78, 90";
        SpanExercises.SumCsv(line); // warm up so JIT work isn't counted
        SpanExercises.CountWords(line);

        var before = GC.GetAllocatedBytesForCurrentThread();
        SpanExercises.SumCsv(line);
        SpanExercises.CountWords(line);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }

    [Theory]
    [InlineData("hello wORLD", "Hello World")]
    [InlineData("  two  spaces", "  Two  Spaces")]
    [InlineData("", "")]
    public void Span_TitleCase(string input, string expected) =>
        Assert.Equal(expected, SpanExercises.TitleCase(input));

    [Theory]
    [InlineData(2, new[] { 4, 5, 1, 2, 3 })]
    [InlineData(5, new[] { 1, 2, 3, 4, 5 })]
    [InlineData(7, new[] { 4, 5, 1, 2, 3 })]
    [InlineData(-1, new[] { 2, 3, 4, 5, 1 })]
    public void Span_RotateRight(int k, int[] expected)
    {
        int[] items = [1, 2, 3, 4, 5];

        SpanExercises.RotateRight(items.AsSpan(), k);

        Assert.Equal(expected, items);
    }

    [Fact]
    public void Span_RotateSlice_LeavesRestOfArrayAlone()
    {
        int[] items = [0, 1, 2, 3, 9];

        SpanExercises.RotateRight(items.AsSpan(1, 3), 1);

        Assert.Equal([0, 3, 1, 2, 9], items);
    }
}
