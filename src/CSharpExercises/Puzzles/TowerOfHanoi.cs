namespace CSharpExercises.Puzzles;

/// <summary>
/// The Tower of Hanoi: move n disks from one peg to another, never putting a larger disk on a smaller one. The
/// recursive solution (move n-1 out of the way, move the largest, move n-1 back on top) takes 2^n - 1 moves, which is
/// optimal. The iterative version shows the same sequence follows a simple pattern: the smallest disk cycles every
/// other move, and the move in between is the only legal one not involving it.
/// </summary>
public static class TowerOfHanoi
{
    public static IReadOnlyList<(int Disk, char From, char To)> Solve(int disks, char from = 'A', char via = 'B', char to = 'C')
    {
        var moves = new List<(int, char, char)>();

        void Move(int n, char source, char spare, char target)
        {
            if (n == 0) return;
            Move(n - 1, source, target, spare);
            moves.Add((n, source, target));
            Move(n - 1, spare, source, target);
        }

        Move(disks, from, via, to);
        return moves;
    }

    public static IReadOnlyList<(int Disk, char From, char To)> SolveIteratively(int disks)
    {
        var pegs = new Dictionary<char, List<int>> { ['A'] = Enumerable.Range(1, disks).Reverse().ToList(), ['B'] = [], ['C'] = [] };
        // The smallest disk cycles A → C → B for an odd count and A → B → C for an even one.
        var cycle = disks % 2 == 1 ? "ACB" : "ABC";
        var moves = new List<(int, char, char)>();
        var smallestAt = 0;

        for (var i = 0; i < (1 << disks) - 1; i++)
        {
            if (i % 2 == 0)
            {
                var (from, to) = (cycle[smallestAt], cycle[(smallestAt + 1) % 3]);
                Transfer(from, to);
                smallestAt = (smallestAt + 1) % 3;
            }
            else
            {
                // The one legal move between the two pegs that don't hold the smallest disk.
                var others = "ABC".Where(p => p != cycle[smallestAt]).ToArray();
                var (a, b) = (others[0], others[1]);
                if (pegs[a].Count == 0 || (pegs[b].Count > 0 && pegs[b][^1] < pegs[a][^1])) Transfer(b, a);
                else Transfer(a, b);
            }
        }
        return moves;

        void Transfer(char from, char to)
        {
            var disk = pegs[from][^1];
            pegs[from].RemoveAt(pegs[from].Count - 1);
            pegs[to].Add(disk);
            moves.Add((disk, from, to));
        }
    }

    /// <summary>Replays moves and checks every rule; returns true if all disks end on peg C.</summary>
    public static bool IsValid(int disks, IEnumerable<(int Disk, char From, char To)> moves)
    {
        var pegs = new Dictionary<char, Stack<int>> { ['A'] = new(Enumerable.Range(1, disks).Reverse()), ['B'] = new(), ['C'] = new() };
        foreach (var (disk, from, to) in moves)
        {
            if (!pegs[from].TryPeek(out var top) || top != disk) return false;
            if (pegs[to].TryPeek(out var below) && below < disk) return false;
            pegs[to].Push(pegs[from].Pop());
        }
        return pegs['C'].Count == disks;
    }
}
