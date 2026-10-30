namespace CSharpExercises.MachineLearning;

/// <summary>
/// k-nearest neighbours: no training at all, just remember the examples and classify a new point by majority vote of the
/// k closest ones (closer neighbours can count more). Features on different scales would let one dominate the
/// distance, so they're standardised to mean 0 and standard deviation 1 first, using the training data's statistics.
/// </summary>
public sealed class NearestNeighbors<TLabel> where TLabel : notnull
{
    private readonly List<(double[] Features, TLabel Label)> _examples;
    private readonly double[] _mean;
    private readonly double[] _std;
    private readonly int _k;
    private readonly bool _weighted;

    public NearestNeighbors(IEnumerable<(double[] Features, TLabel Label)> examples, int k = 3, bool distanceWeighted = false)
    {
        var raw = examples.ToList();
        var dims = raw[0].Features.Length;
        _mean = Enumerable.Range(0, dims).Select(d => raw.Average(e => e.Features[d])).ToArray();
        _std = Enumerable.Range(0, dims).Select(d => Math.Sqrt(raw.Average(e => Math.Pow(e.Features[d] - _mean[d], 2))) is var s && s > 0 ? s : 1).ToArray();
        _examples = raw.Select(e => (Scale(e.Features), e.Label)).ToList();
        (_k, _weighted) = (k, distanceWeighted);
    }

    public TLabel Predict(double[] features)
    {
        var scaled = Scale(features);
        return _examples
            .Select(e => (e.Label, Distance: Math.Sqrt(KMeans.SquaredDistance(e.Features, scaled))))
            .OrderBy(e => e.Distance)
            .Take(_k)
            .GroupBy(e => e.Label)
            .MaxBy(g => _weighted ? g.Sum(e => 1 / (e.Distance + 1e-9)) : g.Count())!
            .Key;
    }

    /// <summary>The share of examples predicted correctly.</summary>
    public double Accuracy(IEnumerable<(double[] Features, TLabel Label)> test) =>
        test.Average(t => EqualityComparer<TLabel>.Default.Equals(Predict(t.Features), t.Label) ? 1.0 : 0.0);

    private double[] Scale(double[] features) => features.Select((x, d) => (x - _mean[d]) / _std[d]).ToArray();
}
