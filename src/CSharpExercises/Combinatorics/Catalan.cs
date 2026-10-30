using System.Numerics;

namespace CSharpExercises.Combinatorics;

/// <summary>
/// Catalan numbers 1, 1, 2, 5, 14, 42… count a surprising number of things: balanced bracket strings, binary tree
/// shapes, ways to triangulate a polygon, mountain ranges that never dip below the start. The recurrence
/// C(n+1) = Σ C(i)·C(n-i) mirrors "the first bracket pair encloses i pairs and is followed by n - i".
/// </summary>
public static class Catalan
{
    public static BigInteger Number(int n) => Binomial.Choose(2 * n, n) / (n + 1);

    /// <summary>All balanced strings of n bracket pairs, built by never closing more than has been opened.</summary>
    public static IEnumerable<string> BalancedBrackets(int n)
    {
        var buffer = new char[2 * n];

        IEnumerable<string> Build(int position, int open, int close)
        {
            if (position == 2 * n) { yield return new string(buffer); yield break; }
            if (open < n)
            {
                buffer[position] = '(';
                foreach (var s in Build(position + 1, open + 1, close)) yield return s;
            }
            if (close < open)
            {
                buffer[position] = ')';
                foreach (var s in Build(position + 1, open, close + 1)) yield return s;
            }
        }

        return Build(0, 0, 0);
    }

    /// <summary>Every binary tree shape with n nodes, written as nested parentheses: "(L)R" style strings.</summary>
    public static IReadOnlyList<string> TreeShapes(int n)
    {
        var memo = new Dictionary<int, List<string>> { [0] = ["."] };

        List<string> Shapes(int count)
        {
            if (memo.TryGetValue(count, out var cached)) return cached;
            var result = new List<string>();
            for (var left = 0; left < count; left++)
                foreach (var l in Shapes(left))
                    foreach (var r in Shapes(count - 1 - left))
                        result.Add($"({l} {r})");
            return memo[count] = result;
        }

        return Shapes(n);
    }
}
