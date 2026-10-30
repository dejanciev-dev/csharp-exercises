using System.Collections.Immutable;
using CSharpExercises.FunctionalProgramming;

namespace CSharpExercises.Tests;

public class FunctionalProgrammingTests
{
    private sealed record Address(string Street, string City);
    private sealed record Person(string Name, Address Address);

    [Fact]
    public void Option_MapsBindsAndMatches()
    {
        var ports = new Dictionary<string, string> { ["http"] = "80", ["bad"] = "eighty" };

        var http = ports.Find("http").Bind(Option.ParseInt);
        var bad = ports.Find("bad").Bind(Option.ParseInt);
        var missing = ports.Find("ftp").Bind(Option.ParseInt);

        Assert.Equal(Option.Some(80), http);
        Assert.True(bad.IsNone);
        Assert.Equal("none", missing.Match(p => p.ToString(), () => "none"));
        Assert.Equal(443, missing.GetValueOrDefault(443));
        Assert.Equal(Option<int>.None, default);
    }

    [Fact]
    public void Option_SupportsQuerySyntaxAndFiltering()
    {
        var sum = from a in Option.ParseInt("2") from b in Option.ParseInt("40") select a + b;
        var failed = from a in Option.ParseInt("2") from b in Option.ParseInt("x") select a + b;

        Assert.Equal(Option.Some(42), sum);
        Assert.True(failed.IsNone);
        Assert.True(Option.Some(5).Where(x => x > 10).IsNone);
        Assert.Equal([1, 3], new[] { Option.Some(1), Option<int>.None, Option.Some(3) }.Values());
        Assert.Equal("Some(4)", new[] { 1, 4, 9 }.FirstOrNone(x => x % 2 == 0).ToString());

        Option<string> fromNull = (string?)null!;
        Assert.True(fromNull.IsNone);
        Assert.Throws<ArgumentNullException>(() => Option.Some<string>(null!));
    }

    [Fact]
    public void Trampoline_RecursesWithoutGrowingTheStack()
    {
        Assert.Equal(500_000_500_000, Trampoline.Sum(1_000_000));
        Assert.True(Trampoline.IsEven(1_000_000));
        Assert.False(Trampoline.IsEven(999_999));
        Assert.Equal(System.Numerics.BigInteger.Parse("2432902008176640000"), Trampoline.Factorial(20));
        Assert.Equal(5050, Trampoline.SumRecursive(100)); // fine when shallow; a million levels would crash the process
    }

    [Fact]
    public void Lens_UpdatesNestedRecords()
    {
        var address = Lens.Of<Person, Address>(p => p.Address, (p, a) => p with { Address = a });
        var city = Lens.Of<Address, string>(a => a.City, (a, c) => a with { City = c });
        var personCity = address.Then(city);

        var ada = new Person("Ada", new Address("St James's Square", "London"));
        var moved = personCity.Set(ada, "Paris");

        Assert.Equal("Paris", personCity.Get(moved));
        Assert.Equal("London", ada.Address.City); // the original is untouched
        Assert.Equal("St James's Square", moved.Address.Street);
        Assert.Equal("LONDON", personCity.Modify(ada, c => c.ToUpperInvariant()).Address.City);
    }

    [Fact]
    public void Lens_FocusesOnListElements()
    {
        IReadOnlyList<int> list = [1, 2, 3];
        Assert.Equal([1, 20, 3], Lens.At<int>(1).Modify(list, x => x * 10));
        Assert.Equal([1, 2, 3], list);
    }

    [Fact]
    public void Zipper_MovesAndEditsImmutably()
    {
        var start = Zipper<char>.FromList("abcd");

        var edited = start.Right().Right().Replace('C').InsertAfter('!');

        Assert.Equal("abC!d", string.Concat(edited.ToList()));
        Assert.Equal('!', edited.Focus);
        Assert.Equal(3, edited.Index);
        Assert.Equal("abcd", string.Concat(start.ToList()));
        Assert.Equal("abCd", string.Concat(edited.Remove()!.ToList()));
        Assert.Throws<InvalidOperationException>(() => start.Left());
        Assert.Null(Zipper<int>.FromList([1]).Remove());
    }

    [Fact]
    public void Monoid_FoldsAndSummarises()
    {
        Assert.Equal(new Sum(10), Monoid.Concat([new Sum(1), new Sum(2), new Sum(3), new Sum(4)]));
        Assert.Equal(new Max(int.MinValue), Monoid.Concat<Max>([]));

        var summary = Monoid.Concat(new[] { 3.0, 1, 4, 1, 5 }.Select(Summary.Of));
        Assert.Equal((5L, 1.0, 5.0), (summary.Count, summary.Min, summary.Max));
        Assert.Equal(2.8, summary.Mean, 12);
    }

    [Fact]
    public void Monoid_ParallelResultMatchesSequential()
    {
        var random = new Random(1);
        var values = Enumerable.Range(0, 10_000).Select(_ => BestRun.Of(random.Next(-100, 100))).ToArray();

        var sequential = Monoid.Concat(values);
        var parallel = Monoid.ParallelConcat(values, chunks: 8);

        Assert.Equal(sequential, parallel);
        Assert.Equal(6, Monoid.Concat(new long[] { -2, 1, -3, 4, -1, 2, 1, -5, 4 }.Select(BestRun.Of)).Best); // same as Kadane
    }

    [Theory]
    [InlineData("1 + 2 * 3", 7)]
    [InlineData("(1 + 2) * 3", 9)]
    [InlineData("10 - 4 - 3", 3)]
    [InlineData("100 / 10 / 5", 2)]
    [InlineData(" ( ( 42 ) ) ", 42)]
    public void ParserCombinators_Calculator(string input, double expected) =>
        Assert.Equal(expected, Parsers.Calculator.ParseAll(input));

    [Fact]
    public void ParserCombinators_ReportErrors()
    {
        var error = Assert.Throws<FormatException>(() => Parsers.Calculator.ParseAll("1 + * 2"));
        Assert.Contains("position", error.Message);
        Assert.Throws<FormatException>(() => Parsers.Calculator.ParseAll("(1 + 2"));

        var csv = from first in Parsers.Number from rest in (from _ in Parsers.Char(',') from n in Parsers.Number select n).Many() select (IReadOnlyList<double>)[first, .. rest];
        Assert.Equal([1.0, 22, 333], csv.ParseAll("1, 22 ,333"));
    }
}
