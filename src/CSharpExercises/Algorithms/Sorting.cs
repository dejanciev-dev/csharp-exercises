namespace CSharpExercises.Algorithms;

/// <summary>
/// Classic sorting algorithms. Each returns a new sorted array and leaves the input untouched.
/// </summary>
public static class Sorting
{
    public static T[] BubbleSort<T>(T[] input) where T : IComparable<T>
    {
        var a = (T[])input.Clone();
        for (var i = 0; i < a.Length - 1; i++)
        {
            var swapped = false;
            for (var j = 0; j < a.Length - 1 - i; j++)
            {
                if (a[j].CompareTo(a[j + 1]) > 0)
                {
                    (a[j], a[j + 1]) = (a[j + 1], a[j]);
                    swapped = true;
                }
            }

            if (!swapped) break;
        }

        return a;
    }

    public static T[] MergeSort<T>(T[] input) where T : IComparable<T>
    {
        if (input.Length <= 1) return (T[])input.Clone();

        var mid = input.Length / 2;
        var left = MergeSort(input[..mid]);
        var right = MergeSort(input[mid..]);

        var result = new T[input.Length];
        int i = 0, l = 0, r = 0;
        while (l < left.Length && r < right.Length)
        {
            result[i++] = left[l].CompareTo(right[r]) <= 0 ? left[l++] : right[r++];
        }

        while (l < left.Length) result[i++] = left[l++];
        while (r < right.Length) result[i++] = right[r++];
        return result;
    }

    public static T[] QuickSort<T>(T[] input) where T : IComparable<T>
    {
        var a = (T[])input.Clone();
        QuickSort(a, 0, a.Length - 1);
        return a;
    }

    private static void QuickSort<T>(T[] a, int low, int high) where T : IComparable<T>
    {
        if (low >= high) return;

        var pivot = a[high];
        var p = low;
        for (var j = low; j < high; j++)
        {
            if (a[j].CompareTo(pivot) < 0)
            {
                (a[p], a[j]) = (a[j], a[p]);
                p++;
            }
        }

        (a[p], a[high]) = (a[high], a[p]);
        QuickSort(a, low, p - 1);
        QuickSort(a, p + 1, high);
    }
}
