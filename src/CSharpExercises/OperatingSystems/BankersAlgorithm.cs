namespace CSharpExercises.OperatingSystems;

/// <summary>
/// Dijkstra's banker's algorithm avoids deadlock: grant a resource request only if, afterwards, there is still some
/// order in which every process can get its maximum and finish (a "safe state"). The safety check pretends to run
/// any process whose remaining need fits in what's available, then reclaims its allocation, until all finish or
/// none can.
/// </summary>
public sealed class BankersAlgorithm
{
    private readonly int[] _available;
    private readonly int[][] _maximum;
    private readonly int[][] _allocated;

    public BankersAlgorithm(int[] available, int[][] maximum, int[][] allocated)
    {
        _available = [.. available];
        _maximum = maximum.Select(r => r.ToArray()).ToArray();
        _allocated = allocated.Select(r => r.ToArray()).ToArray();
    }

    public IReadOnlyList<int> Available => _available;

    /// <summary>An order in which all processes can finish, or null if the state is unsafe.</summary>
    public IReadOnlyList<int>? SafeSequence()
    {
        var work = _available.ToArray();
        var finished = new bool[_maximum.Length];
        var order = new List<int>();

        bool progress;
        do
        {
            progress = false;
            for (var p = 0; p < _maximum.Length; p++)
            {
                if (finished[p] || !Need(p).Zip(work).All(x => x.First <= x.Second)) continue;
                for (var r = 0; r < work.Length; r++) work[r] += _allocated[p][r];
                finished[p] = true;
                order.Add(p);
                progress = true;
            }
        } while (progress);

        return order.Count == _maximum.Length ? order : null;
    }

    /// <summary>Grants the request only if it keeps the system safe; otherwise the process has to wait.</summary>
    public bool TryRequest(int process, int[] request)
    {
        if (request.Zip(Need(process)).Any(x => x.First > x.Second)) throw new InvalidOperationException("The request exceeds the declared maximum.");
        if (request.Zip(_available).Any(x => x.First > x.Second)) return false;

        Apply(process, request, +1);
        if (SafeSequence() is not null) return true;
        Apply(process, request, -1); // roll back
        return false;
    }

    public void Release(int process, int[] amounts) => Apply(process, amounts, -1);

    private int[] Need(int p) => _maximum[p].Zip(_allocated[p], (max, alloc) => max - alloc).ToArray();

    private void Apply(int process, int[] amounts, int sign)
    {
        for (var r = 0; r < amounts.Length; r++)
        {
            _allocated[process][r] += sign * amounts[r];
            _available[r] -= sign * amounts[r];
        }
    }
}
