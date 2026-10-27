using System.Numerics;
using CSharpExercises.Basics;
using CSharpExercises.Numerics;

namespace CSharpExercises.Tests;

public class NumericsTests
{
    [Fact]
    public void Double_CannotRepresentMostDecimalFractions()
    {
        Assert.NotEqual(0.3, 0.1 + 0.2);
        Assert.Equal(0.30000000000000004, 0.1 + 0.2);
        Assert.Equal(0.3m, 0.1m + 0.2m); // decimal stores base-10 digits, so money adds up
    }

    [Fact]
    public void Double_SpecialValues()
    {
        var nan = double.NaN;
        var alsoNan = Math.Sqrt(-1);

        Assert.False(nan == alsoNan);  // NaN is not equal to anything, other NaNs included
        Assert.True(nan.Equals(alsoNan)); // ...except through Equals, so it works as a dictionary key
        Assert.True(0.0 == -0.0);
        Assert.Equal(double.PositiveInfinity, 1.0 / 0.0);
    }

    [Theory]
    [InlineData(0.1 + 0.2, 0.3, true)]
    [InlineData(1e-15, 0.0, true)]                 // near zero the absolute tolerance applies
    [InlineData(1_000_000.0, 1_000_000.001, false)]
    [InlineData(double.NaN, double.NaN, false)]
    [InlineData(double.PositiveInfinity, double.PositiveInfinity, true)]
    public void NearlyEqual(double a, double b, bool expected)
    {
        Assert.Equal(expected, FloatingPoint.NearlyEqual(a, b));
    }

    [Fact]
    public void KahanSum_KeepsTheErrorThatNaiveSummingAccumulates()
    {
        var tenths = Enumerable.Repeat(0.1, 10_000_000);

        var naiveError = Math.Abs(FloatingPoint.NaiveSum(tenths) - 1_000_000);
        var kahanError = Math.Abs(FloatingPoint.KahanSum(tenths) - 1_000_000);

        Assert.True(naiveError > 1e-5, $"naive error {naiveError}");
        Assert.True(kahanError < 1e-8, $"Kahan error {kahanError}");
    }

    [Fact]
    public void UlpDistance_CountsRepresentableValuesBetween()
    {
        Assert.Equal(1, FloatingPoint.UlpDistance(1.0, Math.BitIncrement(1.0)));
        Assert.Equal(0, FloatingPoint.UlpDistance(0.0, -0.0));
        Assert.Equal(2, FloatingPoint.UlpDistance(-double.Epsilon, double.Epsilon));
        Assert.Equal(1, FloatingPoint.UlpDistance(0.1 + 0.2, 0.3)); // as close as two different doubles can be
    }

    [Fact]
    public void Rational_IsExact()
    {
        var tenth = new Rational(1, 10);

        Assert.Equal(new Rational(3, 10), tenth + tenth + tenth);
        Assert.Equal(Rational.One, new Rational(1, 3) * 3);
    }

    [Fact]
    public void Rational_IsAlwaysInLowestTermsWithAPositiveDenominator()
    {
        var value = new Rational(6, -8);

        Assert.Equal(-3, value.Numerator);
        Assert.Equal(4, value.Denominator);
        Assert.Equal("-3/4", value.ToString());
        Assert.Equal(new Rational(-3, 4).GetHashCode(), value.GetHashCode());
    }

    [Fact]
    public void Rational_HarmonicNumber()
    {
        var sum = Rational.Zero;
        for (var k = 1; k <= 20; k++) sum += new Rational(1, k);

        Assert.Equal(new Rational(BigInteger.Parse("55835135"), BigInteger.Parse("15519504")), sum);
        Assert.Equal(3.597739657143682, (double)sum, precision: 14);
    }

    [Fact]
    public void Rational_DefaultIsZero()
    {
        Assert.Equal(Rational.Zero, default);
        Assert.Equal(new Rational(1, 2), default(Rational) + new Rational(1, 2));
    }

    [Theory]
    [InlineData("3/4", 3, 4)]
    [InlineData("-6/8", -3, 4)]
    [InlineData("0.375", 3, 8)]
    [InlineData("-2.5", -5, 2)]
    [InlineData("42", 42, 1)]
    public void Rational_Parse(string text, int numerator, int denominator)
    {
        Assert.Equal(new Rational(numerator, denominator), Rational.Parse(text));
    }

    [Fact]
    public void Rational_ComparesAndRejectsZeroDenominators()
    {
        Assert.True(new Rational(1, 3) < new Rational(1, 2));
        Assert.True(new Rational(-1, 2) < Rational.Zero);
        Assert.Equal([new Rational(-1, 2), new Rational(1, 3), new Rational(1, 2)], new[] { new Rational(1, 2), new Rational(-1, 2), new Rational(1, 3) }.Order());
        Assert.Throws<DivideByZeroException>(() => new Rational(1, 0));
        Assert.Throws<DivideByZeroException>(() => Rational.One / Rational.Zero);
    }

    [Fact]
    public void ModPow_AgreesWithBigInteger()
    {
        var random = new Random(3);
        for (var i = 0; i < 1_000; i++)
        {
            var b = (ulong)random.NextInt64();
            var e = (ulong)random.NextInt64();
            var m = (ulong)random.NextInt64(1, long.MaxValue);

            Assert.Equal((ulong)BigInteger.ModPow(b, e, m), NumberTheory.ModPow(b, e, m));
        }

        Assert.Equal(24ul, NumberTheory.ModPow(2, 10, 1000));
    }

    [Theory]
    [InlineData(3, 11, 4)]
    [InlineData(10, 17, 12)]
    [InlineData(-3, 11, 7)]
    public void ModInverse(long a, long m, long expected)
    {
        Assert.Equal(expected, NumberTheory.ModInverse(a, m));
        Assert.Equal(1, ((a * expected) % m + m) % m);
    }

    [Fact]
    public void ModInverse_DoesNotExistForSharedFactors()
    {
        var ex = Assert.Throws<ArgumentException>(() => NumberTheory.ModInverse(6, 9));
        Assert.Contains("factor 3", ex.Message);
    }

    [Theory]
    [InlineData(2ul, true)]
    [InlineData(1_000_000_007ul, true)]
    [InlineData(2_305_843_009_213_693_951ul, true)]      // 2^61 - 1, a Mersenne prime
    [InlineData(18_446_744_073_709_551_557ul, true)]     // the largest prime below 2^64
    [InlineData(0ul, false)]
    [InlineData(1ul, false)]
    [InlineData(561ul, false)]                           // Carmichael number: fools the simple Fermat test
    [InlineData(3_215_031_751ul, false)]                 // fools Miller–Rabin with bases 2, 3, 5 and 7
    [InlineData(18_446_744_073_709_551_615ul, false)]    // 2^64 - 1
    public void IsPrime(ulong n, bool expected)
    {
        Assert.Equal(expected, NumberTheory.IsPrime(n));
    }

    [Fact]
    public void IsPrime_AgreesWithTrialDivision()
    {
        for (var n = 0; n < 20_000; n++)
            Assert.Equal(MathHelpers.IsPrime(n), NumberTheory.IsPrime((ulong)n));
    }

    [Fact]
    public void Fibonacci_AgreesWithTheIterativeSequence()
    {
        var expected = MathHelpers.Fibonacci().Take(90).ToList();

        Assert.Equal(expected, Enumerable.Range(0, 90).Select(n => (long)NumberTheory.Fibonacci(n)));
    }

    [Fact]
    public void Fibonacci_HandlesHugeIndexes()
    {
        Assert.Equal(BigInteger.Parse("354224848179261915075"), NumberTheory.Fibonacci(100));
        Assert.Equal(2_090, NumberTheory.Fibonacci(10_000).ToString().Length);
    }
}
