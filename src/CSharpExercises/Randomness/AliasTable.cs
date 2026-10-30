namespace CSharpExercises.Randomness;

/// <summary>
/// Vose's alias method: after O(n) setup, draw from any discrete distribution in O(1). Each of n equal columns holds
/// at most two outcomes: its own share and a top-up "alias" taken from an outcome with more than its share. Loot
/// tables, A/B traffic splits and Markov text generators all need fast weighted draws.
/// </summary>
public sealed class AliasTable<T>
{
    private readonly T[] _outcomes;
    private readonly double[] _probability;
    private readonly int[] _alias;

    public AliasTable(IReadOnlyList<(T Outcome, double Weight)> weighted)
    {
        var n = weighted.Count;
        if (n == 0) throw new ArgumentException("Need at least one outcome.", nameof(weighted));
        if (weighted.Any(w => w.Weight < 0 || double.IsNaN(w.Weight))) throw new ArgumentException("Weights must be non-negative.", nameof(weighted));
        var total = weighted.Sum(w => w.Weight);
        if (total <= 0) throw new ArgumentException("Weights must not all be zero.", nameof(weighted));

        _outcomes = weighted.Select(w => w.Outcome).ToArray();
        _probability = new double[n];
        _alias = new int[n];

        // Scale so the average column is exactly 1.
        var scaled = weighted.Select(w => w.Weight * n / total).ToArray();
        var small = new Stack<int>();
        var large = new Stack<int>();
        for (var i = 0; i < n; i++) (scaled[i] < 1 ? small : large).Push(i);

        while (small.Count > 0 && large.Count > 0)
        {
            var s = small.Pop();
            var l = large.Pop();
            _probability[s] = scaled[s];
            _alias[s] = l;
            scaled[l] -= 1 - scaled[s]; // l fills the rest of column s
            (scaled[l] < 1 ? small : large).Push(l);
        }

        // Whatever is left is 1 up to rounding error.
        while (large.Count > 0) _probability[large.Pop()] = 1;
        while (small.Count > 0) _probability[small.Pop()] = 1;
    }

    public T Next(Random random)
    {
        var column = random.Next(_outcomes.Length);
        return random.NextDouble() < _probability[column] ? _outcomes[column] : _outcomes[_alias[column]];
    }
}
