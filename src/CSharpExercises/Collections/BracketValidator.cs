namespace CSharpExercises.Collections;

/// <summary>
/// Checks whether every bracket in a string is matched and correctly nested.
/// </summary>
public static class BracketValidator
{
    private static readonly Dictionary<char, char> Pairs = new()
    {
        [')'] = '(',
        [']'] = '[',
        ['}'] = '{'
    };

    public static bool IsBalanced(string input)
    {
        var stack = new Stack<char>();
        foreach (var c in input)
        {
            if (Pairs.ContainsValue(c))
            {
                stack.Push(c);
            }
            else if (Pairs.TryGetValue(c, out var open))
            {
                if (stack.IsEmpty || stack.Pop() != open) return false;
            }
        }

        return stack.IsEmpty;
    }
}
