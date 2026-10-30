using CSharpExercises.Planning;

namespace CSharpExercises.Tests;

public class PlanningTests
{
    [Fact]
    public void CriticalPath_FindsSlackAndTheCriticalChain()
    {
        ProjectTask[] tasks =
        [
            new("design", 5),
            new("backend", 10, "design"),
            new("frontend", 6, "design"),
            new("docs", 3, "design"),
            new("integration", 4, "backend", "frontend"),
            new("release", 1, "integration", "docs"),
        ];

        var (length, schedule, path) = CriticalPath.Plan(tasks);
        var byName = schedule.ToDictionary(s => s.Name);

        Assert.Equal(20, length);
        Assert.Equal(["design", "backend", "integration", "release"], path);
        Assert.Equal(4, byName["frontend"].Slack);  // can start up to 4 days late
        Assert.Equal(11, byName["docs"].Slack);
        Assert.Equal((5, 9), (byName["frontend"].EarliestStart, byName["frontend"].LatestStart));
    }

    [Fact]
    public void CriticalPath_RejectsCyclesAndUnknownTasks()
    {
        Assert.Throws<InvalidOperationException>(() => CriticalPath.Plan([new("a", 1, "b"), new("b", 1, "a")]));
        Assert.Throws<ArgumentException>(() => CriticalPath.Plan([new("a", 1, "ghost")]));
    }

    [Fact]
    public void BinPacking_HeuristicsVersusOptimum()
    {
        int[] items = [4, 8, 1, 4, 2, 1, 7, 3, 6, 4];

        var nextFit = BinPacking.NextFit(items, 10);
        var firstFit = BinPacking.FirstFit(items, 10);
        var decreasing = BinPacking.FirstFitDecreasing(items, 10);

        Assert.Equal(4, BinPacking.LowerBound(items, 10));
        Assert.Equal(4, BinPacking.Optimal(items, 10));
        Assert.True(nextFit.Count >= firstFit.Count);
        Assert.Equal(4, decreasing.Count);
        Assert.All(decreasing, bin => Assert.True(bin.Sum() <= 10));
        Assert.Equal(items.Order(), decreasing.SelectMany(b => b).Order());
        Assert.Throws<ArgumentException>(() => BinPacking.FirstFit([11], 10));
    }

    [Fact]
    public void BinPacking_DecreasingIsNearOptimalOnRandomInputs()
    {
        var random = new Random(1);
        for (var trial = 0; trial < 30; trial++)
        {
            var items = Enumerable.Range(0, 12).Select(_ => random.Next(1, 11)).ToArray();
            var optimal = BinPacking.Optimal(items, 10);
            Assert.InRange(BinPacking.FirstFitDecreasing(items, 10).Count, optimal, optimal * 11 / 9 + 1);
            Assert.True(optimal >= BinPacking.LowerBound(items, 10));
        }
    }

    [Fact]
    public void Csp_ColoursTheMapOfAustralia()
    {
        string[] colours = ["red", "green", "blue"];
        var solver = new ConstraintSolver<string, string>();
        foreach (var region in new[] { "WA", "NT", "SA", "Q", "NSW", "V", "T" }) solver.Variable(region, colours);
        (string, string)[] borders = [("WA", "NT"), ("WA", "SA"), ("NT", "SA"), ("NT", "Q"), ("SA", "Q"), ("SA", "NSW"), ("SA", "V"), ("Q", "NSW"), ("NSW", "V")];
        foreach (var (a, b) in borders) solver.AllDifferent(a, b);

        var solution = solver.Solve()!;

        Assert.Equal(7, solution.Count);
        Assert.All(borders, b => Assert.NotEqual(solution[b.Item1], solution[b.Item2]));
        Assert.Equal(0, solver.Backtracks); // MRV + forward checking never takes a wrong turn here
    }

    [Fact]
    public void Csp_SchedulesExamsAndDetectsImpossibility()
    {
        // Exams sharing students can't be in the same slot; "math" must come before "physics".
        var solver = new ConstraintSolver<string, int>();
        foreach (var exam in new[] { "math", "physics", "chemistry", "biology" }) solver.Variable(exam, [1, 2, 3]);
        solver.AllDifferent("math", "physics").AllDifferent("physics", "chemistry").AllDifferent("chemistry", "biology").AllDifferent("math", "chemistry")
              .Constraint("math", "physics", (m, p) => m < p);

        var schedule = solver.Solve()!;
        Assert.True(schedule["math"] < schedule["physics"]);

        var impossible = new ConstraintSolver<string, int>().Variable("a", [1, 2]).Variable("b", [1, 2]).Variable("c", [1, 2])
            .AllDifferent("a", "b").AllDifferent("b", "c").AllDifferent("a", "c");
        Assert.Null(impossible.Solve()); // three mutually different values from two
    }

    [Fact]
    public void Hungarian_MatchesBruteForce()
    {
        var random = new Random(2);
        for (var trial = 0; trial < 50; trial++)
        {
            var n = random.Next(1, 8);
            var cost = new long[n, n];
            for (var i = 0; i < n; i++)
                for (var j = 0; j < n; j++) cost[i, j] = random.Next(0, 100);

            var (jobs, total) = Assignment.Hungarian(cost);

            Assert.Equal(Assignment.BruteForce(cost), total);
            Assert.Equal(n, jobs.Distinct().Count());
        }
    }

    [Fact]
    public void Hungarian_KnownExampleAndMaximisation()
    {
        long[,] cost = { { 9, 2, 7, 8 }, { 6, 4, 3, 7 }, { 5, 8, 1, 8 }, { 7, 6, 9, 4 } };

        var (jobs, total) = Assignment.Hungarian(cost);

        Assert.Equal(13, total);
        Assert.Equal([1, 0, 2, 3], jobs);
        Assert.Equal(Assignment.Hungarian(cost).Cost, -Assignment.Maximize(Negate(cost)).Value);
    }

    private static long[,] Negate(long[,] m)
    {
        var n = m.GetLength(0);
        var r = new long[n, n];
        for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++) r[i, j] = -m[i, j];
        return r;
    }
}
