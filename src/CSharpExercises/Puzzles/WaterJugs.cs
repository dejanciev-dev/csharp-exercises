namespace CSharpExercises.Puzzles;

/// <summary>
/// The water jug puzzle (measure exactly 4 litres with a 3 and a 5 litre jug): a state-space search where each state is
/// the amount in each jug and the moves are fill, empty and pour. BFS gives the fewest steps. Number theory tells in
/// advance whether it's possible: the target must be a multiple of gcd(a, b) and fit in a jug.
/// </summary>
public static class WaterJugs
{
    public static bool IsPossible(int a, int b, int target) => target <= Math.Max(a, b) && target % Gcd(a, b) == 0;

    public static IReadOnlyList<(string Action, int A, int B)>? Solve(int capacityA, int capacityB, int target)
    {
        var start = (0, 0);
        var previous = new Dictionary<(int, int), ((int, int) State, string Action)>();
        var seen = new HashSet<(int, int)> { start };
        var queue = new Queue<(int A, int B)>([start]);

        while (queue.TryDequeue(out var state))
        {
            if (state.A == target || state.B == target)
            {
                var steps = new List<(string, int, int)>();
                for (var s = state; previous.TryGetValue(s, out var p); s = p.State) steps.Add((p.Action, s.A, s.B));
                steps.Reverse();
                return steps;
            }

            var (a, b) = state;
            var pourAB = Math.Min(a, capacityB - b);
            var pourBA = Math.Min(b, capacityA - a);
            (string, (int, int))[] moves =
            [
                ("fill A", (capacityA, b)), ("fill B", (a, capacityB)),
                ("empty A", (0, b)), ("empty B", (a, 0)),
                ("pour A→B", (a - pourAB, b + pourAB)), ("pour B→A", (a + pourBA, b - pourBA)),
            ];
            foreach (var (action, next) in moves)
            {
                if (!seen.Add(next)) continue;
                previous[next] = (state, action);
                queue.Enqueue(next);
            }
        }
        return null;
    }

    private static int Gcd(int a, int b) => b == 0 ? a : Gcd(b, a % b);
}
