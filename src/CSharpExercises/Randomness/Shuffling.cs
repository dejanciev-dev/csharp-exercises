namespace CSharpExercises.Randomness;

/// <summary>
/// Fisher–Yates: walk from the end, swapping each position with a random one at or before it. Every one of the n!
/// orders is equally likely. The tempting "swap each position with any position" version is subtly biased,
/// because n^n equally likely swap sequences can't divide evenly into n! orders.
/// </summary>
public static class Shuffling
{
    public static void Shuffle<T>(Span<T> items, Random random)
    {
        for (var i = items.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }

    /// <summary>The broken version, kept to show the bias in a test.</summary>
    public static void NaiveShuffle<T>(Span<T> items, Random random)
    {
        for (var i = 0; i < items.Length; i++)
        {
            var j = random.Next(items.Length);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }

    /// <summary>A random k-subset in O(k): a Fisher–Yates that stops after k steps, using a dictionary for swapped slots.</summary>
    public static int[] SampleWithoutReplacement(int populationSize, int k, Random random)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(k, populationSize);
        var swapped = new Dictionary<int, int>();
        var result = new int[k];
        for (var i = 0; i < k; i++)
        {
            var j = random.Next(i, populationSize);
            var atJ = swapped.GetValueOrDefault(j, j);
            swapped[j] = swapped.GetValueOrDefault(i, i);
            result[i] = atJ;
        }
        return result;
    }
}
