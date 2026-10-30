namespace CSharpExercises.LinearAlgebra;

/// <summary>
/// PageRank by power iteration: a random surfer follows a link with probability d, or jumps to a random page
/// otherwise. A page's rank is the long-run share of time spent on it: the dominant eigenvector of the link matrix,
/// found by multiplying a starting vector repeatedly until it stops changing.
/// </summary>
public static class PageRank
{
    public static IReadOnlyDictionary<string, double> Compute(
        IReadOnlyDictionary<string, string[]> links, double damping = 0.85, double tolerance = 1e-10, int maxIterations = 1000)
    {
        var pages = links.Keys.Concat(links.Values.SelectMany(v => v)).Distinct().Order().ToArray();
        var index = pages.Select((p, i) => (p, i)).ToDictionary(x => x.p, x => x.i);
        var n = pages.Length;
        var outLinks = pages.Select(p => links.TryGetValue(p, out var targets) ? targets.Distinct().Select(t => index[t]).ToArray() : []).ToArray();

        var rank = Enumerable.Repeat(1.0 / n, n).ToArray();
        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            var next = new double[n];

            // Dead ends (no outgoing links) would leak rank; spread theirs evenly over all pages instead.
            var danglingRank = Enumerable.Range(0, n).Where(i => outLinks[i].Length == 0).Sum(i => rank[i]);
            var baseline = (1 - damping) / n + damping * danglingRank / n;
            Array.Fill(next, baseline);

            for (var i = 0; i < n; i++)
                foreach (var target in outLinks[i])
                    next[target] += damping * rank[i] / outLinks[i].Length;

            var change = next.Zip(rank, (a, b) => Math.Abs(a - b)).Sum();
            rank = next;
            if (change < tolerance) break;
        }

        return pages.Select((p, i) => (p, rank[i])).ToDictionary(x => x.p, x => x.Item2);
    }
}
