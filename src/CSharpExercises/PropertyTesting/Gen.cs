namespace CSharpExercises.PropertyTesting;

/// <summary>
/// A generator of random test inputs, plus a way to shrink a value into simpler candidates. The size parameter grows
/// during a run, so early cases are small and later ones larger. Generators compose: lists of generated items,
/// choices between generators, mapped values.
/// </summary>
public sealed class Gen<T>(Func<Random, int, T> generate, Func<T, IEnumerable<T>>? shrink = null)
{
    public T Generate(Random random, int size) => generate(random, size);

    public IEnumerable<T> Shrink(T value) => shrink?.Invoke(value) ?? [];

    /// <summary>Maps generated values. Shrinking is lost, since there's no way back from TResult to T.</summary>
    public Gen<TResult> Select<TResult>(Func<T, TResult> map) => new((r, s) => map(generate(r, s)));

    public Gen<T> Where(Func<T, bool> predicate, int maxTries = 100) => new((r, s) =>
    {
        for (var i = 0; i < maxTries; i++)
        {
            var value = generate(r, s);
            if (predicate(value)) return value;
        }
        throw new InvalidOperationException("The filter rejected too many values; generate valid values directly instead.");
    }, v => Shrink(v).Where(predicate));
}

public static class Gen
{
    /// <summary>Integers within ±size (clamped to the range); shrinks toward zero by halving the distance.</summary>
    public static Gen<int> Int(int min = int.MinValue, int max = int.MaxValue) => new(
        (r, size) => r.Next(Math.Max(min, -size), Math.Min(max, size) + 1),
        value => ShrinkInt(value).Where(v => v >= min && v <= max));

    /// <summary>Lists of up to size items; shrinks by dropping chunks, then single items, then shrinking items.</summary>
    public static Gen<List<T>> ListOf<T>(Gen<T> item) => new(
        (r, size) => Enumerable.Range(0, r.Next(size + 1)).Select(_ => item.Generate(r, size)).ToList(),
        list => ShrinkList(list, item));

    public static Gen<string> String(string alphabet = "abc") => new(
        (r, size) => new string(Enumerable.Range(0, r.Next(size + 1)).Select(_ => alphabet[r.Next(alphabet.Length)]).ToArray()),
        s => Enumerable.Range(0, s.Length).Select(i => s.Remove(i, 1)));

    public static Gen<T> OneOf<T>(params T[] values) => new((r, _) => values[r.Next(values.Length)], v =>
    {
        var index = Array.IndexOf(values, v);
        return values.Take(Math.Max(index, 0)); // earlier values count as simpler
    });

    public static Gen<(TA, TB)> Zip<TA, TB>(Gen<TA> a, Gen<TB> b) => new(
        (r, s) => (a.Generate(r, s), b.Generate(r, s)),
        pair => a.Shrink(pair.Item1).Select(x => (x, pair.Item2)).Concat(b.Shrink(pair.Item2).Select(y => (pair.Item1, y))));

    private static IEnumerable<int> ShrinkInt(int value)
    {
        if (value == 0) yield break;
        yield return 0;
        if (value < 0 && value != int.MinValue) yield return -value; // prefer positive
        for (var distance = value / 2; distance != 0; distance /= 2) yield return value - distance;
    }

    private static IEnumerable<List<T>> ShrinkList<T>(List<T> list, Gen<T> item)
    {
        for (var chunk = list.Count; chunk > 0; chunk /= 2)
            for (var start = 0; start + chunk <= list.Count; start += chunk)
                yield return [.. list[..start], .. list[(start + chunk)..]];

        for (var i = 0; i < list.Count; i++)
            foreach (var smaller in item.Shrink(list[i]))
                yield return [.. list[..i], smaller, .. list[(i + 1)..]];
    }
}
