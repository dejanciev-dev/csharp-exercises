namespace CSharpExercises.Planning;

public sealed record ProjectTask(string Name, int Duration, params string[] DependsOn);

public sealed record TaskSchedule(string Name, int EarliestStart, int EarliestFinish, int LatestStart, int LatestFinish)
{
    /// <summary>How long the task can slip without delaying the project.</summary>
    public int Slack => LatestStart - EarliestStart;
    public bool IsCritical => Slack == 0;
}

/// <summary>
/// The critical path method for project planning: a forward pass in dependency order gives each task's earliest start
/// (after all its prerequisites finish), a backward pass gives the latest start that still meets the project end. Tasks
/// with zero slack form the critical path; speeding up anything else doesn't shorten the project. Cycles are reported.
/// </summary>
public static class CriticalPath
{
    public static (int ProjectLength, IReadOnlyList<TaskSchedule> Schedule, IReadOnlyList<string> Path) Plan(IEnumerable<ProjectTask> tasks)
    {
        var byName = tasks.ToDictionary(t => t.Name);
        if (byName.Values.SelectMany(t => t.DependsOn).FirstOrDefault(d => !byName.ContainsKey(d)) is { } missing)
            throw new ArgumentException($"Unknown dependency '{missing}'.");

        var order = TopologicalOrder(byName);
        var earliestFinish = new Dictionary<string, int>();
        foreach (var name in order)
            earliestFinish[name] = byName[name].DependsOn.Select(d => earliestFinish[d]).DefaultIfEmpty(0).Max() + byName[name].Duration;

        var projectLength = earliestFinish.Values.DefaultIfEmpty(0).Max();
        var dependents = order.ToDictionary(n => n, n => byName.Values.Where(t => t.DependsOn.Contains(n)).Select(t => t.Name).ToList());
        var latestStart = new Dictionary<string, int>();
        foreach (var name in Enumerable.Reverse(order))
        {
            var latestFinish = dependents[name].Select(d => latestStart[d]).DefaultIfEmpty(projectLength).Min();
            latestStart[name] = latestFinish - byName[name].Duration;
        }

        var schedule = order.Select(n => new TaskSchedule(
            n, earliestFinish[n] - byName[n].Duration, earliestFinish[n], latestStart[n], latestStart[n] + byName[n].Duration)).ToList();

        // Follow critical tasks from a critical start to the end.
        var path = new List<string>();
        var current = schedule.Where(s => s.IsCritical && s.EarliestStart == 0).Select(s => s.Name).FirstOrDefault();
        while (current is not null)
        {
            path.Add(current);
            var finish = earliestFinish[current];
            current = dependents[current].FirstOrDefault(d => schedule.First(s => s.Name == d) is { IsCritical: true } s && s.EarliestStart == finish);
        }
        return (projectLength, schedule, path);
    }

    /// <summary>Kahn's algorithm; leftover tasks mean a dependency cycle.</summary>
    private static List<string> TopologicalOrder(Dictionary<string, ProjectTask> tasks)
    {
        var remaining = tasks.ToDictionary(t => t.Key, t => t.Value.DependsOn.Length);
        var ready = new Queue<string>(remaining.Where(r => r.Value == 0).Select(r => r.Key).Order());
        var order = new List<string>();
        while (ready.TryDequeue(out var name))
        {
            order.Add(name);
            foreach (var t in tasks.Values.Where(t => t.DependsOn.Contains(name)).OrderBy(t => t.Name))
                if (--remaining[t.Name] == 0) ready.Enqueue(t.Name);
        }
        if (order.Count != tasks.Count)
            throw new InvalidOperationException($"Dependency cycle among: {string.Join(", ", tasks.Keys.Except(order).Order())}.");
        return order;
    }
}
