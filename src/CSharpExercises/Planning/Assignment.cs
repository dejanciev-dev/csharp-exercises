namespace CSharpExercises.Planning;

/// <summary>
/// The assignment problem: n workers, n jobs, a cost for each pairing; give every worker one job at minimum total cost.
/// Trying all n! assignments is hopeless beyond about 10; the Hungarian algorithm (here the O(n³) version with
/// potentials, after Kuhn and Munkres) solves it exactly by growing a matching along shortest augmenting paths while
/// keeping reduced costs non-negative. Used for dispatching drivers, matching students to projects, tracking objects.
/// </summary>
public static class Assignment
{
    /// <returns>For each worker (row), the job (column) assigned, and the total cost.</returns>
    public static (int[] JobForWorker, long Cost) Hungarian(long[,] cost)
    {
        var n = cost.GetLength(0);
        if (cost.GetLength(1) != n) throw new ArgumentException("The cost matrix must be square (pad with zeros if needed).");

        // 1-based arrays as in the classic formulation; index 0 is a virtual "unmatched" worker.
        var u = new long[n + 1];       // worker potentials
        var v = new long[n + 1];       // job potentials
        var matchedWorker = new int[n + 1]; // matchedWorker[job] = worker
        var way = new int[n + 1];

        for (var worker = 1; worker <= n; worker++)
        {
            matchedWorker[0] = worker;
            var job0 = 0;
            var minReduced = Enumerable.Repeat(long.MaxValue, n + 1).ToArray();
            var used = new bool[n + 1];
            do
            {
                used[job0] = true;
                var w = matchedWorker[job0];
                var delta = long.MaxValue;
                var job1 = 0;
                for (var job = 1; job <= n; job++)
                {
                    if (used[job]) continue;
                    var reduced = cost[w - 1, job - 1] - u[w] - v[job];
                    if (reduced < minReduced[job]) (minReduced[job], way[job]) = (reduced, job0);
                    if (minReduced[job] < delta) (delta, job1) = (minReduced[job], job);
                }
                for (var job = 0; job <= n; job++)
                {
                    if (used[job]) { u[matchedWorker[job]] += delta; v[job] -= delta; }
                    else minReduced[job] -= delta;
                }
                job0 = job1;
            } while (matchedWorker[job0] != 0);

            // Flip the augmenting path.
            do
            {
                var previous = way[job0];
                matchedWorker[job0] = matchedWorker[previous];
                job0 = previous;
            } while (job0 != 0);
        }

        var jobForWorker = new int[n];
        for (var job = 1; job <= n; job++) jobForWorker[matchedWorker[job] - 1] = job - 1;
        long total = 0;
        for (var w = 0; w < n; w++) total += cost[w, jobForWorker[w]];
        return (jobForWorker, total);
    }

    /// <summary>Maximising (profit, preference) is minimising the negated matrix.</summary>
    public static (int[] JobForWorker, long Value) Maximize(long[,] value)
    {
        var n = value.GetLength(0);
        var negated = new long[n, n];
        for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++) negated[i, j] = -value[i, j];
        var (jobs, cost) = Hungarian(negated);
        return (jobs, -cost);
    }

    /// <summary>All n! assignments, to check the algorithm on small inputs.</summary>
    public static long BruteForce(long[,] cost)
    {
        var n = cost.GetLength(0);
        var best = long.MaxValue;
        void Try(int worker, bool[] taken, long sum)
        {
            if (sum >= best) return;
            if (worker == n) { best = sum; return; }
            for (var job = 0; job < n; job++)
            {
                if (taken[job]) continue;
                taken[job] = true;
                Try(worker + 1, taken, sum + cost[worker, job]);
                taken[job] = false;
            }
        }
        Try(0, new bool[n], 0);
        return best;
    }
}
