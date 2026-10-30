namespace CSharpExercises.Puzzles;

/// <summary>
/// The Josephus problem: n people in a circle, every k-th one is eliminated; who is left? Simulating with a list costs
/// O(n²) (or O(n) with a linked list), but the recurrence J(n) = (J(n-1) + k) mod n gives the survivor in O(n) with no
/// data structure, and for k = 2 there's a closed form: rotate n's binary representation left by one bit.
/// </summary>
public static class Josephus
{
    /// <summary>The elimination order, people numbered from 1.</summary>
    public static IReadOnlyList<int> EliminationOrder(int n, int k)
    {
        var circle = new LinkedList<int>(Enumerable.Range(1, n));
        var order = new List<int>();
        var current = circle.First!;
        while (circle.Count > 0)
        {
            for (var i = 1; i < k; i++) current = current.Next ?? circle.First!;
            var next = current.Next ?? circle.First;
            order.Add(current.Value);
            circle.Remove(current);
            if (circle.Count > 0) current = next!;
        }
        return order;
    }

    public static int Survivor(int n, int k)
    {
        var position = 0; // with one person, position 0 survives
        for (var size = 2; size <= n; size++) position = (position + k) % size;
        return position + 1;
    }

    /// <summary>For k = 2: n = 2^m + l gives 2l + 1, i.e. move the leading 1 bit to the end.</summary>
    public static int SurvivorOfTwo(int n)
    {
        var highest = 1 << (31 - System.Numerics.BitOperations.LeadingZeroCount((uint)n));
        return 2 * (n - highest) + 1;
    }
}
