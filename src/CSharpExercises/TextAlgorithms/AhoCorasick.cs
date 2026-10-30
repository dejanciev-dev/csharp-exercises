namespace CSharpExercises.TextAlgorithms;

/// <summary>
/// Aho–Corasick searches for many patterns at once in a single pass over the text: a trie of the patterns plus
/// "failure links" that say where to continue after a mismatch, like KMP generalised to a set of words. Used by
/// spam filters, antivirus signatures and grep -F.
/// </summary>
public sealed class AhoCorasick
{
    private sealed class State
    {
        public readonly Dictionary<char, State> Next = [];
        public State Fail = null!;
        public readonly List<string> Outputs = [];
    }

    private readonly State _root = new();

    public AhoCorasick(IEnumerable<string> patterns)
    {
        foreach (var pattern in patterns.Where(p => p.Length > 0))
        {
            var state = _root;
            foreach (var c in pattern)
            {
                if (!state.Next.TryGetValue(c, out var next)) state.Next[c] = next = new State();
                state = next;
            }
            state.Outputs.Add(pattern);
        }

        // Breadth-first, so a state's failure target (always shallower) is finished before the state itself.
        _root.Fail = _root;
        var queue = new Queue<State>();
        foreach (var child in _root.Next.Values)
        {
            child.Fail = _root;
            queue.Enqueue(child);
        }

        while (queue.TryDequeue(out var state))
        {
            foreach (var (c, child) in state.Next)
            {
                var fail = state.Fail;
                while (fail != _root && !fail.Next.ContainsKey(c)) fail = fail.Fail;
                child.Fail = fail.Next.TryGetValue(c, out var target) && target != child ? target : _root;
                child.Outputs.AddRange(child.Fail.Outputs); // "she" also reports "he"
                queue.Enqueue(child);
            }
        }
    }

    public IEnumerable<(int Index, string Pattern)> FindAll(string text)
    {
        var state = _root;
        for (var i = 0; i < text.Length; i++)
        {
            while (state != _root && !state.Next.ContainsKey(text[i])) state = state.Fail;
            state = state.Next.GetValueOrDefault(text[i], _root);

            foreach (var pattern in state.Outputs) yield return (i - pattern.Length + 1, pattern);
        }
    }

    /// <summary>Replaces every listed word with asterisks, longest match winning where matches overlap.</summary>
    public string Censor(string text)
    {
        var masked = text.ToCharArray();
        foreach (var (index, pattern) in FindAll(text))
            Array.Fill(masked, '*', index, pattern.Length);
        return new string(masked);
    }
}
