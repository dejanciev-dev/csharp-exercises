namespace CSharpExercises.MachineLearning;

/// <summary>
/// k-means clustering: alternate between assigning each point to its nearest centre and moving each centre to the mean
/// of its points, until nothing changes. The result depends on the starting centres, so k-means++ picks them spread
/// out (each new centre chosen with probability proportional to its squared distance from the nearest existing one).
/// The elbow of inertia (total squared distance) against k suggests how many clusters there are.
/// </summary>
public static class KMeans
{
    public sealed record Result(double[][] Centers, int[] Assignments, double Inertia, int Iterations);

    public static Result Fit(IReadOnlyList<double[]> points, int k, Random random, int maxIterations = 300)
    {
        if (k < 1 || k > points.Count) throw new ArgumentOutOfRangeException(nameof(k));
        var centers = PlusPlusInit(points, k, random);
        var assignments = new int[points.Count];

        for (var iteration = 1; iteration <= maxIterations; iteration++)
        {
            var changed = false;
            for (var i = 0; i < points.Count; i++)
            {
                var nearest = Nearest(centers, points[i]);
                changed |= nearest != assignments[i];
                assignments[i] = nearest;
            }

            for (var c = 0; c < k; c++)
            {
                var members = points.Where((_, i) => assignments[i] == c).ToList();
                if (members.Count == 0) continue; // keep an empty cluster's centre where it was
                centers[c] = Enumerable.Range(0, points[0].Length).Select(d => members.Average(p => p[d])).ToArray();
            }

            if (!changed && iteration > 1) return new Result(centers, assignments, Inertia(points, centers, assignments), iteration);
        }
        return new Result(centers, assignments, Inertia(points, centers, assignments), maxIterations);
    }

    public static double[][] PlusPlusInit(IReadOnlyList<double[]> points, int k, Random random)
    {
        var centers = new List<double[]> { points[random.Next(points.Count)] };
        while (centers.Count < k)
        {
            var weights = points.Select(p => centers.Min(c => SquaredDistance(p, c))).ToArray();
            var target = random.NextDouble() * weights.Sum();
            var index = 0;
            for (var cumulative = weights[0]; cumulative < target; cumulative += weights[++index]) { }
            centers.Add(points[index]);
        }
        return centers.Select(c => c.ToArray()).ToArray();
    }

    public static int Nearest(double[][] centers, double[] point) =>
        Enumerable.Range(0, centers.Length).MinBy(c => SquaredDistance(centers[c], point));

    public static double SquaredDistance(double[] a, double[] b) => a.Zip(b, (x, y) => (x - y) * (x - y)).Sum();

    private static double Inertia(IReadOnlyList<double[]> points, double[][] centers, int[] assignments) =>
        points.Select((p, i) => SquaredDistance(p, centers[assignments[i]])).Sum();
}
