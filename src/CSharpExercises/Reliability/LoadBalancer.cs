namespace CSharpExercises.Reliability;

/// <summary>
/// Smooth weighted round-robin, as used by nginx: with weights 5, 1, 1 it yields a a b a c a a rather than
/// a a a a a b c, so the heavy server never gets a long burst. Each pick, every server's score grows by its weight;
/// the highest score wins and pays back the total.
/// </summary>
public sealed class WeightedRoundRobin<T> where T : notnull
{
    private sealed class Server(T item, int weight)
    {
        public T Item { get; } = item;
        public int Weight { get; } = weight;
        public int Current { get; set; }
        public bool Healthy { get; set; } = true;
    }

    private readonly List<Server> _servers;
    private readonly Lock _lock = new();

    public WeightedRoundRobin(IEnumerable<(T Item, int Weight)> servers)
    {
        _servers = servers.Select(s => s.Weight > 0 ? new Server(s.Item, s.Weight) : throw new ArgumentException("Weights must be positive.", nameof(servers))).ToList();
        if (_servers.Count == 0) throw new ArgumentException("Need at least one server.", nameof(servers));
    }

    public T Next()
    {
        lock (_lock)
        {
            var healthy = _servers.Where(s => s.Healthy).ToList();
            if (healthy.Count == 0) throw new InvalidOperationException("No healthy servers.");

            var total = 0;
            Server? best = null;
            foreach (var server in healthy)
            {
                server.Current += server.Weight;
                total += server.Weight;
                if (best is null || server.Current > best.Current) best = server;
            }
            best!.Current -= total;
            return best.Item;
        }
    }

    /// <summary>Takes a server out of rotation (failed health check) or puts it back.</summary>
    public void SetHealthy(T item, bool healthy)
    {
        lock (_lock)
        {
            var server = _servers.Single(s => EqualityComparer<T>.Default.Equals(s.Item, item));
            server.Healthy = healthy;
            server.Current = 0;
        }
    }
}

/// <summary>
/// "Power of two choices": pick two servers at random and use the less loaded one. Almost as good as always picking
/// the least loaded server, without having to scan or lock all of them.
/// </summary>
public sealed class TwoChoicesBalancer(int serverCount, Random random)
{
    private readonly int[] _load = new int[serverCount];

    public IReadOnlyList<int> Load => _load;

    public int Assign()
    {
        var a = random.Next(_load.Length);
        var b = random.Next(_load.Length);
        var pick = _load[a] <= _load[b] ? a : b;
        _load[pick]++;
        return pick;
    }

    public int AssignRandom()
    {
        var pick = random.Next(_load.Length);
        _load[pick]++;
        return pick;
    }
}
