namespace CSharpExercises.Collections;

/// <summary>
/// A self-balancing binary search tree. The plain <see cref="BinarySearchTree{T}"/> degrades to a linked list when
/// values arrive in sorted order; an AVL tree keeps every node's subtrees within one level of each other by rotating
/// after inserts and deletes, guaranteeing O(log n) height. Each node also stores its subtree size, which makes
/// "k-th smallest" and "rank of x" O(log n) too.
/// </summary>
public sealed class AvlTree<T>(IComparer<T>? comparer = null)
{
    private sealed class Node(T value)
    {
        public T Value = value;
        public Node? Left, Right;
        public int Height = 1;
        public int Size = 1;
    }

    private readonly IComparer<T> _comparer = comparer ?? Comparer<T>.Default;
    private Node? _root;

    public int Count => SizeOf(_root);
    public int Height => HeightOf(_root);

    public bool Add(T value)
    {
        var added = false;
        _root = Insert(_root, value, ref added);
        return added;
    }

    public bool Remove(T value)
    {
        var removed = false;
        _root = Delete(_root, value, ref removed);
        return removed;
    }

    public bool Contains(T value)
    {
        for (var node = _root; node is not null;)
        {
            var cmp = _comparer.Compare(value, node.Value);
            if (cmp == 0) return true;
            node = cmp < 0 ? node.Left : node.Right;
        }
        return false;
    }

    /// <summary>The k-th smallest value (0-based), using subtree sizes to choose a side.</summary>
    public T ElementAt(int k)
    {
        if (k < 0 || k >= Count) throw new ArgumentOutOfRangeException(nameof(k));
        var node = _root!;
        while (true)
        {
            var leftSize = SizeOf(node.Left);
            if (k == leftSize) return node.Value;
            if (k < leftSize) node = node.Left!;
            else
            {
                k -= leftSize + 1;
                node = node.Right!;
            }
        }
    }

    /// <summary>How many values are smaller than <paramref name="value"/>.</summary>
    public int Rank(T value)
    {
        var rank = 0;
        for (var node = _root; node is not null;)
        {
            if (_comparer.Compare(value, node.Value) <= 0) node = node.Left;
            else
            {
                rank += SizeOf(node.Left) + 1;
                node = node.Right;
            }
        }
        return rank;
    }

    public IEnumerable<T> InOrder()
    {
        var stack = new System.Collections.Generic.Stack<Node>();
        for (var node = _root; node is not null || stack.Count > 0;)
        {
            for (; node is not null; node = node.Left) stack.Push(node);
            node = stack.Pop();
            yield return node.Value;
            node = node.Right;
        }
    }

    /// <summary>Checks the AVL and ordering invariants everywhere, for tests.</summary>
    public bool IsBalanced() => Check(_root, out _);

    private Node Insert(Node? node, T value, ref bool added)
    {
        if (node is null)
        {
            added = true;
            return new Node(value);
        }
        var cmp = _comparer.Compare(value, node.Value);
        if (cmp < 0) node.Left = Insert(node.Left, value, ref added);
        else if (cmp > 0) node.Right = Insert(node.Right, value, ref added);
        else return node;
        return Rebalance(node);
    }

    private Node? Delete(Node? node, T value, ref bool removed)
    {
        if (node is null) return null;
        var cmp = _comparer.Compare(value, node.Value);
        if (cmp < 0) node.Left = Delete(node.Left, value, ref removed);
        else if (cmp > 0) node.Right = Delete(node.Right, value, ref removed);
        else
        {
            removed = true;
            if (node.Left is null) return node.Right;
            if (node.Right is null) return node.Left;

            // Two children: take the in-order successor's value, then delete the successor.
            var successor = node.Right;
            while (successor.Left is not null) successor = successor.Left;
            node.Value = successor.Value;
            var ignored = false;
            node.Right = Delete(node.Right, successor.Value, ref ignored);
        }
        return Rebalance(node);
    }

    private static Node Rebalance(Node node)
    {
        Update(node);
        var balance = HeightOf(node.Left) - HeightOf(node.Right);

        if (balance > 1)
        {
            if (HeightOf(node.Left!.Left) < HeightOf(node.Left.Right)) node.Left = RotateLeft(node.Left); // left-right case
            return RotateRight(node);
        }
        if (balance < -1)
        {
            if (HeightOf(node.Right!.Right) < HeightOf(node.Right.Left)) node.Right = RotateRight(node.Right); // right-left case
            return RotateLeft(node);
        }
        return node;
    }

    //     node            left
    //     /  \            /  \
    //   left  C   →      A   node
    //   /  \                 /  \
    //  A    B               B    C
    private static Node RotateRight(Node node)
    {
        var left = node.Left!;
        node.Left = left.Right;
        left.Right = node;
        Update(node);
        Update(left);
        return left;
    }

    private static Node RotateLeft(Node node)
    {
        var right = node.Right!;
        node.Right = right.Left;
        right.Left = node;
        Update(node);
        Update(right);
        return right;
    }

    private static void Update(Node node)
    {
        node.Height = 1 + Math.Max(HeightOf(node.Left), HeightOf(node.Right));
        node.Size = 1 + SizeOf(node.Left) + SizeOf(node.Right);
    }

    private static int HeightOf(Node? node) => node?.Height ?? 0;
    private static int SizeOf(Node? node) => node?.Size ?? 0;

    private bool Check(Node? node, out int height)
    {
        height = 0;
        if (node is null) return true;
        if (!Check(node.Left, out var lh) || !Check(node.Right, out var rh)) return false;
        height = 1 + Math.Max(lh, rh);
        return Math.Abs(lh - rh) <= 1 && node.Height == height
            && (node.Left is null || _comparer.Compare(node.Left.Value, node.Value) < 0)
            && (node.Right is null || _comparer.Compare(node.Right.Value, node.Value) > 0);
    }
}
