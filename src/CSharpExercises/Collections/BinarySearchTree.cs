namespace CSharpExercises.Collections;

/// <summary>
/// A binary search tree: every value in a node's left subtree is smaller than the node, every value in the right is larger.
/// Duplicates are ignored.
/// </summary>
public class BinarySearchTree<T> where T : IComparable<T>
{
    private sealed class Node(T value)
    {
        public T Value { get; set; } = value;
        public Node? Left { get; set; }
        public Node? Right { get; set; }
    }

    private Node? _root;

    public int Count { get; private set; }

    public int Height => GetHeight(_root);

    public T Min => Extreme(n => n.Left);

    public T Max => Extreme(n => n.Right);

    /// <summary>Returns false if the value was already in the tree.</summary>
    public bool Add(T value)
    {
        if (_root is null)
        {
            _root = new Node(value);
            Count++;
            return true;
        }

        var current = _root;
        while (true)
        {
            var cmp = value.CompareTo(current.Value);
            if (cmp == 0) return false;

            if (cmp < 0)
            {
                if (current.Left is null)
                {
                    current.Left = new Node(value);
                    break;
                }

                current = current.Left;
            }
            else
            {
                if (current.Right is null)
                {
                    current.Right = new Node(value);
                    break;
                }

                current = current.Right;
            }
        }

        Count++;
        return true;
    }

    public bool Contains(T value)
    {
        var current = _root;
        while (current is not null)
        {
            var cmp = value.CompareTo(current.Value);
            if (cmp == 0) return true;
            current = cmp < 0 ? current.Left : current.Right;
        }

        return false;
    }

    public bool Remove(T value)
    {
        _root = Remove(_root, value, out var removed);
        if (removed) Count--;
        return removed;
    }

    /// <summary>Visits values in sorted order, iteratively, using the hand-written <see cref="Stack{T}"/>.</summary>
    public IEnumerable<T> InOrder()
    {
        var stack = new Stack<Node>();
        var current = _root;
        while (current is not null || !stack.IsEmpty)
        {
            while (current is not null)
            {
                stack.Push(current);
                current = current.Left;
            }

            current = stack.Pop();
            yield return current.Value;
            current = current.Right;
        }
    }

    private static Node? Remove(Node? node, T value, out bool removed)
    {
        if (node is null)
        {
            removed = false;
            return null;
        }

        var cmp = value.CompareTo(node.Value);
        if (cmp < 0)
        {
            node.Left = Remove(node.Left, value, out removed);
            return node;
        }

        if (cmp > 0)
        {
            node.Right = Remove(node.Right, value, out removed);
            return node;
        }

        removed = true;
        if (node.Left is null) return node.Right;
        if (node.Right is null) return node.Left;

        // Two children: take the in-order successor (smallest value in the right subtree), then remove it from there.
        var successor = node.Right;
        while (successor.Left is not null) successor = successor.Left;

        node.Value = successor.Value;
        node.Right = Remove(node.Right, successor.Value, out _);
        return node;
    }

    private static int GetHeight(Node? node) =>
        node is null ? 0 : 1 + Math.Max(GetHeight(node.Left), GetHeight(node.Right));

    private T Extreme(Func<Node, Node?> next)
    {
        var current = _root ?? throw new InvalidOperationException("Tree is empty.");
        while (next(current) is { } child) current = child;
        return current.Value;
    }
}
