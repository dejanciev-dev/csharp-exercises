namespace CSharpExercises.Puzzles;

/// <summary>
/// Lewis Carroll's word ladder: turn COLD into WARM one letter at a time, every step a real word. Breadth-first search
/// finds a shortest ladder. Instead of comparing each word with every other, words are bucketed by wildcard patterns
/// ("c*ld", "co*d"…), so the neighbours of a word are found by looking up its patterns.
/// </summary>
public sealed class WordLadder
{
    private readonly Dictionary<string, List<string>> _byPattern = new();

    public WordLadder(IEnumerable<string> dictionary)
    {
        foreach (var word in dictionary.Select(w => w.ToLowerInvariant()).Distinct())
            foreach (var pattern in Patterns(word))
            {
                if (!_byPattern.TryGetValue(pattern, out var words)) _byPattern[pattern] = words = [];
                words.Add(word);
            }
    }

    public IReadOnlyList<string>? ShortestLadder(string start, string end)
    {
        (start, end) = (start.ToLowerInvariant(), end.ToLowerInvariant());
        if (start.Length != end.Length) return null;

        var previous = new Dictionary<string, string?> { [start] = null };
        var queue = new Queue<string>([start]);
        while (queue.TryDequeue(out var word))
        {
            if (word == end)
            {
                var ladder = new List<string>();
                for (string? w = end; w is not null; w = previous[w]) ladder.Add(w);
                ladder.Reverse();
                return ladder;
            }
            foreach (var next in Patterns(word).SelectMany(p => _byPattern.GetValueOrDefault(p) ?? []))
                if (previous.TryAdd(next, word)) queue.Enqueue(next);
        }
        return null;
    }

    private static IEnumerable<string> Patterns(string word) =>
        Enumerable.Range(0, word.Length).Select(i => word[..i] + "*" + word[(i + 1)..]);
}
