using System.Text;

namespace CSharpExercises.Collections;

/// <summary>
/// Prefix tree: each node is one character, and words that share a prefix share the path to it.
/// Looking up a prefix costs O(prefix length) no matter how many words are stored, which makes it the classic
/// structure behind autocomplete.
/// </summary>
public sealed class Trie
{
    private sealed class Node
    {
        public readonly SortedDictionary<char, Node> Children = [];
        public bool IsWord;
        public int Frequency;
        public int WordsBelow; // words in this subtree, so CountWithPrefix needs no traversal
    }

    private readonly Node _root = new();

    public int Count => _root.WordsBelow;

    /// <summary>Adds a word, or bumps its frequency if it is already there. Returns true if it was new.</summary>
    public bool Add(string word, int frequency = 1)
    {
        ArgumentException.ThrowIfNullOrEmpty(word);

        var isNew = !Contains(word);
        var node = _root;
        if (isNew) node.WordsBelow++;

        foreach (var c in word)
        {
            if (!node.Children.TryGetValue(c, out var child)) node.Children[c] = child = new Node();
            node = child;
            if (isNew) node.WordsBelow++;
        }

        node.IsWord = true;
        node.Frequency += frequency;
        return isNew;
    }

    public bool Contains(string word) => Find(word) is { IsWord: true };

    public int CountWithPrefix(string prefix) => Find(prefix)?.WordsBelow ?? 0;

    /// <summary>
    /// Removes a word and prunes the nodes no other word needs, so the trie does not keep growing with dead branches.
    /// </summary>
    public bool Remove(string word)
    {
        if (!Contains(word)) return false;

        var node = _root;
        node.WordsBelow--;
        foreach (var c in word)
        {
            var child = node.Children[c];
            if (--child.WordsBelow == 0)
            {
                node.Children.Remove(c); // nothing else lives below here
                return true;
            }
            node = child;
        }

        node.IsWord = false;
        node.Frequency = 0;
        return true;
    }

    /// <summary>All words starting with <paramref name="prefix"/>, in alphabetical order.</summary>
    public IEnumerable<string> WithPrefix(string prefix) =>
        Find(prefix) is { } start ? Collect(start, prefix).Select(w => w.Word) : [];

    /// <summary>The <paramref name="count"/> most frequent words starting with <paramref name="prefix"/>; ties are alphabetical.</summary>
    public IReadOnlyList<string> Suggest(string prefix, int count) =>
        Find(prefix) is { } start
            ? [.. Collect(start, prefix).OrderByDescending(w => w.Frequency).ThenBy(w => w.Word, StringComparer.Ordinal).Take(count).Select(w => w.Word)]
            : [];

    private Node? Find(string prefix)
    {
        var node = _root;
        foreach (var c in prefix)
        {
            if (!node.Children.TryGetValue(c, out node)) return null;
        }
        return node;
    }

    // Depth-first with an explicit stack, so a very long word cannot overflow the call stack.
    // One shared StringBuilder holds the current path: each entry remembers how long the path is up to its parent,
    // and popping it trims the builder back to that length before adding its own character.
    // (Building `text + c` at every level instead would copy the whole prefix each time: O(n²) for one long word.)
    private static IEnumerable<(string Word, int Frequency)> Collect(Node start, string prefix)
    {
        var path = new StringBuilder(prefix);

        // Fully qualified: this namespace has its own hand-written Stack<T>.
        var stack = new System.Collections.Generic.Stack<(Node Node, int ParentLength, char? Char)>([(start, prefix.Length, null)]);
        while (stack.Count > 0)
        {
            var (node, parentLength, c) = stack.Pop();
            path.Length = parentLength;
            if (c is { } ch) path.Append(ch);

            if (node.IsWord) yield return (path.ToString(), node.Frequency);

            // Push in reverse so the smallest character is popped first, keeping the output alphabetical.
            foreach (var (childChar, child) in node.Children.Reverse()) stack.Push((child, path.Length, childChar));
        }
    }
}
