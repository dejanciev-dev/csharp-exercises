using CSharpExercises.Basics;

namespace CSharpExercises.Tests;

public class BasicsTests
{
    [Theory]
    [InlineData(1, "1")]
    [InlineData(3, "Fizz")]
    [InlineData(5, "Buzz")]
    [InlineData(15, "FizzBuzz")]
    [InlineData(98, "98")]
    public void FizzBuzz_Convert(int number, string expected) =>
        Assert.Equal(expected, FizzBuzz.Convert(number));

    [Fact]
    public void FizzBuzz_Sequence_ProducesRequestedCount() =>
        Assert.Equal(["1", "2", "Fizz", "4", "Buzz"], FizzBuzz.Sequence(5));

    [Theory]
    [InlineData(-7, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(9, false)]
    [InlineData(97, true)]
    [InlineData(int.MaxValue, true)]
    public void IsPrime(int n, bool expected) =>
        Assert.Equal(expected, MathHelpers.IsPrime(n));

    [Theory]
    [InlineData(0, 1)]
    [InlineData(5, 120)]
    [InlineData(20, 2432902008176640000)]
    public void Factorial(int n, long expected) =>
        Assert.Equal(expected, MathHelpers.Factorial(n));

    [Fact]
    public void Factorial_RejectsOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MathHelpers.Factorial(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => MathHelpers.Factorial(21));
    }

    [Fact]
    public void Fibonacci_FirstTen() =>
        Assert.Equal([0L, 1, 1, 2, 3, 5, 8, 13, 21, 34], MathHelpers.Fibonacci().Take(10));

    [Theory]
    [InlineData(48, 18, 6)]
    [InlineData(-12, 8, 4)]
    [InlineData(7, 0, 7)]
    public void Gcd(int a, int b, int expected) =>
        Assert.Equal(expected, MathHelpers.Gcd(a, b));
}
