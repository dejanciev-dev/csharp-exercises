namespace CSharpExercises.Fuzzy;

/// <summary>
/// A BK-tree finds all words within edit distance d of a query without comparing against every word. Children are
/// keyed by their distance to the parent; by the triangle inequality only children whose key is within d of the
/// query's distance to the parent can contain matches, so most of the tree is skipped.
/// </summary>
public sealed class BkTree(Func<string, string, int> distance)
{
    private sealed class Node(string word)
    {
        public string Word { get; } = word;
        public Dictionary<int, Node> Children { get; } = [];
    }

    private Node? _root;

    public BkTree() : this(DamerauLevenshtein.Distance) { }

    public int Count { get; private set; }

    /// <summary>Number of distance calls made by the last search, to show how much is pruned.</summary>
    public int LastComparisons { get; private set; }

    public void Add(string word)
    {
        if (_root is null)
        {
            _root = new Node(word);
            Count++;
            return;
        }

        var node = _root;
        while (true)
        {
            var d = distance(word, node.Word);
            if (d == 0) return; // already present
            if (!node.Children.TryGetValue(d, out var child))
            {
                node.Children[d] = new Node(word);
                Count++;
                return;
            }
            node = child;
        }
    }

    public IReadOnlyList<(string Word, int Distance)> Search(string query, int maxDistance)
    {
        var results = new List<(string, int)>();
        LastComparisons = 0;
        if (_root is null) return results;

        var pending = new Queue<Node>([_root]);
        while (pending.TryDequeue(out var node))
        {
            var d = distance(query, node.Word);
            LastComparisons++;
            if (d <= maxDistance) results.Add((node.Word, d));

            foreach (var (key, child) in node.Children)
                if (key >= d - maxDistance && key <= d + maxDistance) pending.Enqueue(child);
        }
        return results.OrderBy(r => r.Item2).ThenBy(r => r.Item1, StringComparer.Ordinal).ToList();
    }
}
