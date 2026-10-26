namespace CSharpExercises.Algorithms;

public static class Searching
{
    /// <summary>Returns the index of <paramref name="target"/> in a sorted array, or -1.</summary>
    public static int BinarySearch<T>(T[] sorted, T target) where T : IComparable<T>
    {
        int low = 0, high = sorted.Length - 1;
        while (low <= high)
        {
            var mid = low + (high - low) / 2;
            var cmp = sorted[mid].CompareTo(target);
            if (cmp == 0) return mid;
            if (cmp < 0) low = mid + 1;
            else high = mid - 1;
        }

        return -1;
    }

    /// <summary>Finds the indices of two numbers that add up to <paramref name="target"/> in O(n).</summary>
    public static (int, int)? TwoSum(int[] numbers, int target)
    {
        var seen = new Dictionary<int, int>();
        for (var i = 0; i < numbers.Length; i++)
        {
            if (seen.TryGetValue(target - numbers[i], out var j)) return (j, i);
            seen[numbers[i]] = i;
        }

        return null;
    }
}
