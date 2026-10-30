namespace CSharpExercises.NumericalMethods;

/// <summary>
/// Simulated annealing for problems too big to solve exactly: make a random small change, always accept improvements,
/// and accept worse solutions with probability e^(-Δ/T). The temperature T falls over time, so early on the search
/// roams freely and escapes local minima, and later it settles. Shown on a 100-city travelling salesman tour, where it
/// beats the greedy nearest-neighbour route; 2-opt reversal is the move.
/// </summary>
public static class SimulatedAnnealing
{
    public static (int[] Tour, double Length) SolveTsp(IReadOnlyList<(double X, double Y)> cities, Random random,
        double startTemperature = 100, double cooling = 0.9995, int iterations = 200_000)
    {
        var tour = NearestNeighbourTour(cities);
        var length = TourLength(cities, tour);
        var best = (Tour: tour.ToArray(), Length: length);
        var temperature = startTemperature;

        for (var step = 0; step < iterations; step++)
        {
            // 2-opt: reverse the segment between i and j, which replaces two edges with two others.
            var i = random.Next(1, tour.Length - 1);
            var j = random.Next(i + 1, tour.Length);
            var (a, b) = (tour[i - 1], tour[i]);
            var (c, d) = (tour[j], tour[(j + 1) % tour.Length]);
            var delta = Distance(cities[a], cities[c]) + Distance(cities[b], cities[d]) - Distance(cities[a], cities[b]) - Distance(cities[c], cities[d]);

            if (delta < 0 || random.NextDouble() < Math.Exp(-delta / temperature))
            {
                Array.Reverse(tour, i, j - i + 1);
                length += delta;
                if (length < best.Length) best = (tour.ToArray(), length);
            }
            temperature *= cooling;
        }
        return best;
    }

    /// <summary>The greedy baseline: always go to the closest unvisited city.</summary>
    public static int[] NearestNeighbourTour(IReadOnlyList<(double X, double Y)> cities)
    {
        var visited = new bool[cities.Count];
        var tour = new int[cities.Count];
        visited[0] = true;
        for (var k = 1; k < cities.Count; k++)
        {
            var from = cities[tour[k - 1]];
            var next = Enumerable.Range(0, cities.Count).Where(c => !visited[c]).MinBy(c => Distance(from, cities[c]));
            tour[k] = next;
            visited[next] = true;
        }
        return tour;
    }

    public static double TourLength(IReadOnlyList<(double X, double Y)> cities, IReadOnlyList<int> tour) =>
        Enumerable.Range(0, tour.Count).Sum(k => Distance(cities[tour[k]], cities[tour[(k + 1) % tour.Count]]));

    private static double Distance((double X, double Y) p, (double X, double Y) q) => Math.Sqrt((p.X - q.X) * (p.X - q.X) + (p.Y - q.Y) * (p.Y - q.Y));
}
