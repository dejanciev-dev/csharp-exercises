namespace CSharpExercises.Basics;

/// <summary>
/// Classic FizzBuzz: multiples of 3 become "Fizz", multiples of 5 "Buzz", both "FizzBuzz".
/// </summary>
public static class FizzBuzz
{
    public static string Convert(int number) => (number % 3, number % 5) switch
    {
        (0, 0) => "FizzBuzz",
        (0, _) => "Fizz",
        (_, 0) => "Buzz",
        _ => number.ToString()
    };

    public static IEnumerable<string> Sequence(int count) =>
        Enumerable.Range(1, count).Select(Convert);
}
