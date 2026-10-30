namespace CSharpExercises.PropertyTesting;

public abstract record PropertyResult
{
    public sealed record Passed(int Runs) : PropertyResult;

    /// <summary>A failing input as first found, the simplest failing input after shrinking, and the seed to replay it.</summary>
    public sealed record Falsified(object? Original, object? Shrunk, int ShrinkSteps, int Seed, string? Error) : PropertyResult;
}

/// <summary>
/// Property-based testing in the style of QuickCheck, FsCheck and Hypothesis: instead of a few hand-picked examples,
/// state something that must hold for all inputs ("reversing twice gives the original"), and check it against hundreds
/// of generated ones. When it fails, shrink the input greedily to the smallest one that still fails, so the report
/// shows [0, -1] rather than a 40-element list of random numbers. An exception counts as a failure.
/// </summary>
public static class Property
{
    public static PropertyResult Check<T>(Gen<T> gen, Func<T, bool> property, int runs = 200, int? seed = null, int maxSize = 50)
    {
        var actualSeed = seed ?? Random.Shared.Next();
        var random = new Random(actualSeed);

        for (var run = 0; run < runs; run++)
        {
            var size = 1 + run * maxSize / runs; // start small, grow
            var value = gen.Generate(random, size);
            if (Holds(property, value, out var error)) continue;

            var (shrunk, steps, shrunkError) = Shrink(gen, property, value, error);
            return new PropertyResult.Falsified(value, shrunk, steps, actualSeed, shrunkError);
        }
        return new PropertyResult.Passed(runs);
    }

    /// <summary>Throws with a readable message if the property fails, for use inside ordinary unit tests.</summary>
    public static void Assert<T>(Gen<T> gen, Func<T, bool> property, int runs = 200, int? seed = null)
    {
        if (Check(gen, property, runs, seed) is PropertyResult.Falsified f)
            throw new InvalidOperationException($"Property failed for {Show(f.Shrunk)} (shrunk from {Show(f.Original)} in {f.ShrinkSteps} steps, seed {f.Seed}){(f.Error is null ? "" : $": {f.Error}")}.");
    }

    private static (T Value, int Steps, string? Error) Shrink<T>(Gen<T> gen, Func<T, bool> property, T failing, string? error)
    {
        var steps = 0;
        var improved = true;
        while (improved && steps < 1000)
        {
            improved = false;
            foreach (var candidate in gen.Shrink(failing))
            {
                if (Holds(property, candidate, out var candidateError)) continue;
                (failing, error) = (candidate, candidateError);
                steps++;
                improved = true;
                break; // restart from the smaller value
            }
        }
        return (failing, steps, error);
    }

    private static bool Holds<T>(Func<T, bool> property, T value, out string? error)
    {
        try
        {
            error = null;
            return property(value);
        }
        catch (Exception e)
        {
            error = $"{e.GetType().Name}: {e.Message}";
            return false;
        }
    }

    public static string Show(object? value) => value switch
    {
        null => "null",
        string s => $"\"{s}\"",
        System.Collections.IEnumerable items => $"[{string.Join(", ", items.Cast<object?>().Select(Show))}]",
        _ => value.ToString() ?? "",
    };
}
