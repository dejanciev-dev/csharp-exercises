using System.Numerics;

namespace CSharpExercises.Combinatorics;

/// <summary>
/// Working with permutations without generating all of them. NextPermutation steps to the following arrangement in
/// lexicographic order in place (C++'s std::next_permutation). Rank and Unrank map between a permutation and its
/// position using the factorial number system, so "the millionth permutation" takes microseconds.
/// </summary>
public static class Permutations
{
    /// <summary>Rearranges to the next larger order and returns true, or returns false (and sorts) at the last one.</summary>
    public static bool NextPermutation<T>(Span<T> items) where T : IComparable<T>
    {
        // 1. Find the rightmost i with items[i] < items[i + 1]: everything after it is descending.
        var i = items.Length - 2;
        while (i >= 0 && items[i].CompareTo(items[i + 1]) >= 0) i--;
        if (i < 0)
        {
            items.Reverse();
            return false;
        }

        // 2. Swap it with the smallest larger element to its right, 3. then make the tail ascending.
        var j = items.Length - 1;
        while (items[j].CompareTo(items[i]) <= 0) j--;
        (items[i], items[j]) = (items[j], items[i]);
        items[(i + 1)..].Reverse();
        return true;
    }

    /// <summary>All distinct orders, duplicates handled for free because NextPermutation skips equal arrangements.</summary>
    public static IEnumerable<T[]> All<T>(IEnumerable<T> items) where T : IComparable<T>
    {
        var current = items.Order().ToArray();
        do yield return [.. current];
        while (NextPermutation<T>(current));
    }

    /// <summary>Position of a permutation of 0..n-1 in lexicographic order (0-based).</summary>
    public static BigInteger Rank(IReadOnlyList<int> permutation)
    {
        var n = permutation.Count;
        var remaining = Enumerable.Range(0, n).ToList();
        BigInteger rank = 0;
        for (var i = 0; i < n; i++)
        {
            var index = remaining.IndexOf(permutation[i]);
            if (index < 0) throw new ArgumentException("Not a permutation of 0..n-1.", nameof(permutation));
            rank += index * Factorial(n - 1 - i);
            remaining.RemoveAt(index);
        }
        return rank;
    }

    public static int[] Unrank(int n, BigInteger rank)
    {
        if (rank < 0 || rank >= Factorial(n)) throw new ArgumentOutOfRangeException(nameof(rank));
        var remaining = Enumerable.Range(0, n).ToList();
        var result = new int[n];
        for (var i = 0; i < n; i++)
        {
            var block = Factorial(n - 1 - i);
            var index = (int)(rank / block);
            rank %= block;
            result[i] = remaining[index];
            remaining.RemoveAt(index);
        }
        return result;
    }

    /// <summary>Permutations where nothing stays in place (secret Santa): D(n) = (n - 1)(D(n-1) + D(n-2)).</summary>
    public static BigInteger Derangements(int n)
    {
        if (n == 0) return 1;
        BigInteger previous = 1, current = 0; // D(0), D(1)
        for (var i = 2; i <= n; i++) (previous, current) = (current, (i - 1) * (previous + current));
        return current;
    }

    public static BigInteger Factorial(int n)
    {
        BigInteger result = 1;
        for (var i = 2; i <= n; i++) result *= i;
        return result;
    }
}
