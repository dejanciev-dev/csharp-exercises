using CSharpExercises.PropertyTesting;

namespace CSharpExercises.Tests;

public class PropertyTestingTests
{
    private static readonly Gen<List<int>> Lists = Gen.ListOf(Gen.Int());

    [Fact]
    public void TrueProperties_Pass()
    {
        Assert.IsType<PropertyResult.Passed>(Property.Check(Lists, xs => Enumerable.Reverse(Enumerable.Reverse(xs)).SequenceEqual(xs), seed: 1));
        Assert.IsType<PropertyResult.Passed>(Property.Check(Lists, xs => xs.Order().Order().SequenceEqual(xs.Order()), seed: 2)); // sorting is idempotent
        Property.Assert(Gen.Zip(Gen.Int(), Gen.Int()), p => (long)p.Item1 + p.Item2 == (long)p.Item2 + p.Item1, seed: 3);
    }

    [Fact]
    public void FalseProperty_IsShrunkToAMinimalCounterexample()
    {
        // "The sum of a list is never above 10": fails, and shrinks to a list summing to exactly 11. Greedy shrinking
        // stops at a local minimum such as [5, 6], where every simpler candidate passes, like real shrinkers do.
        var result = Assert.IsType<PropertyResult.Falsified>(Property.Check(Lists, xs => xs.Sum(x => (long)x) <= 10, seed: 4));

        var shrunk = (List<int>)result.Shrunk!;
        Assert.Equal(11, shrunk.Sum());
        Assert.InRange(shrunk.Count, 1, 2);
        Assert.True(result.ShrinkSteps > 0);
    }

    [Fact]
    public void BuggyCode_IsFoundAndReportedSimply()
    {
        // A "remove duplicates" that only removes adjacent ones: correct for sorted input, wrong otherwise.
        static List<int> Dedupe(List<int> xs) => xs.Where((x, i) => i == 0 || xs[i - 1] != x).ToList();

        var result = Assert.IsType<PropertyResult.Falsified>(
            Property.Check(Gen.ListOf(Gen.Int(0, 3)), xs => Dedupe(xs).Count == xs.Distinct().Count(), seed: 5));

        var shrunk = (List<int>)result.Shrunk!;
        Assert.Equal(3, shrunk.Count);           // e.g. [0, 1, 0]: the smallest shape of the bug
        Assert.Equal(shrunk[0], shrunk[2]);
        Assert.NotEqual(shrunk[0], shrunk[1]);
    }

    [Fact]
    public void Exceptions_CountAsFailuresAndAreReported()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Property.Assert(Lists, xs => xs.Max() >= xs.Min(), seed: 6));

        Assert.Contains("Property failed for []", error.Message); // Max of an empty list throws
        Assert.Contains("InvalidOperationException", error.Message);
        Assert.Contains("seed 6", error.Message);
    }

    [Fact]
    public void SameSeed_ReproducesTheSameFailure()
    {
        var first = Property.Check(Gen.Int(), x => x < 30, seed: 7);
        var second = Property.Check(Gen.Int(), x => x < 30, seed: 7);

        Assert.Equal(first, second);
        Assert.Equal(30, ((PropertyResult.Falsified)first).Shrunk); // halving toward zero finds the boundary
    }

    [Fact]
    public void Generators_ShrinkSensibly()
    {
        Assert.Equal([0, 50, 75, 88, 94, 97, 99], Gen.Int().Shrink(100));
        Assert.Equal([0, 7, -4, -6], Gen.Int().Shrink(-7)); // zero, the positive twin, then closer to the original
        Assert.Equal(["bc", "ac", "ab"], Gen.String().Shrink("abc"));
        Assert.Equal(["red"], Gen.OneOf("red", "green", "blue").Shrink("green"));
        Assert.All(Enumerable.Range(0, 100).Select(_ => Gen.Int(5, 9).Generate(new Random(8), 100)), v => Assert.InRange(v, 5, 9));
        Assert.Throws<InvalidOperationException>(() => Gen.Int(0, 10).Where(x => x > 100).Generate(new Random(9), 10));
    }
}
