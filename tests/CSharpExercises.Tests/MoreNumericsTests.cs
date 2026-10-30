using System.Numerics;
using CSharpExercises.Numerics;

namespace CSharpExercises.Tests;

public class MoreNumericsTests
{
    [Fact]
    public void Fixed16_ArithmeticMatchesDoublesWithinPrecision()
    {
        var random = new Random(1);
        for (var i = 0; i < 1000; i++)
        {
            double a = random.NextDouble() * 200 - 100, b = random.NextDouble() * 200 - 100;
            var (fa, fb) = (Fixed16.FromDouble(a), Fixed16.FromDouble(b));
            Assert.InRange(Math.Abs(a + b - (fa + fb).ToDouble()), 0, 1e-4); // each operand is rounded to 1/65536
            Assert.InRange(Math.Abs(a * b - (fa * fb).ToDouble()), 0, 0.01);
            if (Math.Abs(b) > 1) Assert.InRange(Math.Abs(a / b - (fa / fb).ToDouble()), 0, 1e-3);
        }
    }

    [Fact]
    public void Fixed16_RepresentationAndLimits()
    {
        Assert.Equal(65536, Fixed16.FromInt(1).Raw);
        Assert.Equal("1.5", (Fixed16.FromInt(3) / Fixed16.FromInt(2)).ToString());
        Assert.Equal(-2, Fixed16.FromDouble(-1.5).Floor());
        Assert.Equal(Math.Sqrt(2), Fixed16.FromInt(2).Sqrt().ToDouble(), 4);
        Assert.Throws<OverflowException>(() => Fixed16.FromInt(40_000));
        Assert.Throws<DivideByZeroException>(() => Fixed16.FromInt(1) / Fixed16.FromInt(0));
        Assert.Equal(1.0 / 65536, Fixed16.Epsilon.ToDouble());
    }

    [Fact]
    public void ContinuedFractions_ApproximatePi()
    {
        var terms = ContinuedFractions.Expand(Math.PI, 5);
        var convergents = ContinuedFractions.Convergents(terms).ToList();

        Assert.Equal([3L, 7, 15, 1, 292], terms);
        Assert.Equal([(3L, 1L), (22L, 7L), (333L, 106L), (355L, 113L), (103993L, 33102L)], convergents);
        Assert.Equal((355L, 113L), ContinuedFractions.BestApproximation(Math.PI, 1000));
        Assert.Equal((311L, 99L), ContinuedFractions.BestApproximation(Math.PI, 100)); // a semiconvergent beats 22/7
    }

    [Fact]
    public void ContinuedFractions_DecimalsToFractions()
    {
        Assert.Equal((1L, 3L), ContinuedFractions.BestApproximation(0.333333, 100));
        Assert.Equal((1L, 6L), ContinuedFractions.FromRepeatingDecimal("1", "6"));     // 0.1666…
        Assert.Equal((1L, 7L), ContinuedFractions.FromRepeatingDecimal("", "142857"));
        Assert.Equal([1L, 2, 2, 2, 2], ContinuedFractions.Expand(Math.Sqrt(2), 5));   // √2 = [1; 2, 2, 2, …]
    }

    [Fact]
    public void BaseConversion_AnyBase()
    {
        Assert.Equal("FF", BaseConversion.ToBase(255, 16));
        Assert.Equal("-1010", BaseConversion.ToBase(-10, 2));
        Assert.Equal("ZZ", BaseConversion.ToBase(1295, 36));
        var big = BigInteger.Pow(2, 100);
        Assert.Equal(big, BaseConversion.FromBase(BaseConversion.ToBase(big, 7), 7));
        Assert.Throws<FormatException>(() => BaseConversion.FromBase("19", 8));
    }

    [Fact]
    public void BaseConversion_ExoticSystems()
    {
        Assert.Equal("11010", BaseConversion.ToNegabinary(6)); // 16 - 8 - 2
        Assert.Equal("1101", BaseConversion.ToNegabinary(-3));
        Assert.Equal("1T", BaseConversion.ToBalancedTernary(2));
        Assert.Equal("T1", BaseConversion.ToBalancedTernary(-2)); // negation swaps T and 1
        for (var n = -200L; n <= 200; n++)
        {
            Assert.Equal(n, BaseConversion.FromNegabinary(BaseConversion.ToNegabinary(n)));
            Assert.Equal(n, BaseConversion.FromBalancedTernary(BaseConversion.ToBalancedTernary(n)));
        }

        Assert.Equal(["A", "Z", "AA", "AZ", "BA", "ZZ", "AAA"], new[] { 1, 26, 27, 52, 53, 702, 703 }.Select(BaseConversion.ToColumnName));
        Assert.Equal(16384, BaseConversion.FromColumnName("XFD")); // Excel's last column
    }

    [Fact]
    public void Interval_TracksTolerances()
    {
        var length = Interval.Of(10, 0.1);
        var width = Interval.Of(5, 0.05);

        var area = length * width;
        Assert.Equal(49.005, area.Low, 6); // 9.9 × 4.95
        Assert.Equal(51.005, area.High, 6);
        var difference = length - width; // widths add even when subtracting
        Assert.Equal((4.85, 5.15), (Math.Round(difference.Low, 9), Math.Round(difference.High, 9)));
        Assert.True((length / width).Contains(2));
        Assert.Throws<DivideByZeroException>(() => length / new Interval(-1, 1));
    }

    [Fact]
    public void Interval_DependencyProblem()
    {
        var x = new Interval(-1, 2);

        Assert.Equal(new Interval(0, 4), x.Square());
        Assert.Equal(new Interval(-2, 4), x * x); // wider, because the two x's are treated as independent
        Assert.Equal(new Interval(-3, 3), x - x); // not [0, 0], for the same reason
        Assert.Equal("[1, 2]", new Interval(1, 4).Sqrt().ToString());
    }

    [Fact]
    public void Divisors_ListCountAndClassify()
    {
        Assert.Equal([1L, 2, 3, 4, 6, 12], Divisors.Of(12));
        Assert.Equal([1L, 7], Divisors.Of(7));
        Assert.Equal(6, Divisors.Count(12));
        Assert.Equal(Divisors.Of(720_720).Count, Divisors.Count(720_720));
        Assert.Equal(["perfect", "abundant", "deficient"], new long[] { 28, 12, 15 }.Select(Divisors.Classify));
    }

    [Fact]
    public void Divisors_PerfectAndAmicableNumbers()
    {
        Assert.Equal([6, 28, 496, 8128], Divisors.PerfectNumbers(10_000));
        Assert.Equal([(220, 284), (1184, 1210), (2620, 2924), (5020, 5564), (6232, 6368)], Divisors.AmicablePairs(10_000));
        Assert.Equal(31626, Divisors.AmicablePairs(10_000).Sum(p => p.A + p.B)); // Project Euler 21
    }
}
