namespace CSharpExercises.OperatingSystems;

public sealed record Process(string Name, int Arrival, int Burst);

public sealed record ScheduleResult(IReadOnlyList<(string Name, int Start, int End)> Timeline, IReadOnlyDictionary<string, int> Waiting)
{
    public double AverageWaiting => Waiting.Values.Average();

    /// <summary>A text Gantt chart: "|A:0-3|B:3-5|".</summary>
    public string Gantt => "|" + string.Concat(Timeline.Select(t => $"{t.Name}:{t.Start}-{t.End}|"));
}

/// <summary>
/// Classic CPU scheduling policies on one core. First-come-first-served is fair but a long job makes everyone wait
/// (the convoy effect); shortest-job-first minimises average waiting but can starve long jobs; round-robin with a
/// time slice keeps response times short at the cost of context switches.
/// </summary>
public static class CpuScheduling
{
    public static ScheduleResult FirstComeFirstServed(IEnumerable<Process> processes)
    {
        var timeline = new List<(string, int, int)>();
        var time = 0;
        foreach (var p in processes.OrderBy(p => p.Arrival))
        {
            time = Math.Max(time, p.Arrival);
            timeline.Add((p.Name, time, time + p.Burst));
            time += p.Burst;
        }
        return Result(processes, timeline);
    }

    /// <summary>Non-preemptive SJF: when the CPU frees up, run the shortest job that has arrived.</summary>
    public static ScheduleResult ShortestJobFirst(IEnumerable<Process> processes)
    {
        var pending = processes.OrderBy(p => p.Arrival).ToList();
        var ready = new PriorityQueue<Process, (int Burst, int Arrival)>();
        var timeline = new List<(string, int, int)>();
        var time = 0;

        while (pending.Count > 0 || ready.Count > 0)
        {
            while (pending.Count > 0 && pending[0].Arrival <= time)
            {
                ready.Enqueue(pending[0], (pending[0].Burst, pending[0].Arrival));
                pending.RemoveAt(0);
            }
            if (ready.Count == 0)
            {
                time = pending[0].Arrival; // idle until the next arrival
                continue;
            }
            var p = ready.Dequeue();
            timeline.Add((p.Name, time, time + p.Burst));
            time += p.Burst;
        }
        return Result(processes, timeline);
    }

    public static ScheduleResult RoundRobin(IEnumerable<Process> processes, int quantum)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quantum, 1);
        var pending = new Queue<Process>(processes.OrderBy(p => p.Arrival));
        var ready = new Queue<(Process Process, int Remaining)>();
        var timeline = new List<(string Name, int Start, int End)>();
        var time = 0;

        void Admit()
        {
            while (pending.TryPeek(out var next) && next.Arrival <= time) ready.Enqueue((pending.Dequeue(), next.Burst));
        }

        Admit();
        while (ready.Count > 0 || pending.Count > 0)
        {
            if (ready.Count == 0)
            {
                time = pending.Peek().Arrival;
                Admit();
                continue;
            }

            var (p, remaining) = ready.Dequeue();
            var slice = Math.Min(quantum, remaining);
            if (timeline.Count > 0 && timeline[^1].Name == p.Name && timeline[^1].End == time)
                timeline[^1] = (p.Name, timeline[^1].Start, time + slice); // merge consecutive slices of the same process
            else
                timeline.Add((p.Name, time, time + slice));
            time += slice;

            Admit(); // newcomers queue before the preempted process, as in most textbook versions
            if (remaining > slice) ready.Enqueue((p, remaining - slice));
        }
        return Result(processes, timeline);
    }

    /// <summary>Waiting time = finish - arrival - burst.</summary>
    private static ScheduleResult Result(IEnumerable<Process> processes, List<(string Name, int Start, int End)> timeline)
    {
        var waiting = processes.ToDictionary(p => p.Name, p => timeline.Where(t => t.Name == p.Name).Max(t => t.End) - p.Arrival - p.Burst);
        return new ScheduleResult(timeline, waiting);
    }
}
