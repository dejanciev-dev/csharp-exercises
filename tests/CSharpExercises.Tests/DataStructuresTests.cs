using CSharpExercises.Collections;

namespace CSharpExercises.Tests;

public class DataStructuresTests
{
    [Fact]
    public void LinkedList_AddFirstAndLast_KeepOrder()
    {
        var list = new SinglyLinkedList<int>();
        list.AddLast(2);
        list.AddLast(3);
        list.AddFirst(1);

        Assert.Equal([1, 2, 3], list);
        Assert.Equal(3, list.Count);
        Assert.True(list.Contains(2));
    }

    [Fact]
    public void LinkedList_Remove_HandlesHeadMiddleAndTail()
    {
        var list = new SinglyLinkedList<string>();
        foreach (var s in new[] { "a", "b", "c", "d" }) list.AddLast(s);

        Assert.True(list.Remove("a"));
        Assert.True(list.Remove("c"));
        Assert.True(list.Remove("d"));
        Assert.False(list.Remove("x"));

        list.AddLast("e"); // tail must still be correct after removing the old tail
        Assert.Equal(["b", "e"], list);
    }

    [Fact]
    public void LinkedList_Reverse_InPlace()
    {
        var list = new SinglyLinkedList<int>();
        foreach (var n in Enumerable.Range(1, 5)) list.AddLast(n);

        list.Reverse();
        list.AddLast(0); // tail must point at the old head

        Assert.Equal([5, 4, 3, 2, 1, 0], list);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 3)]
    [InlineData(6, 4)]
    public void LinkedList_Middle(int count, int expected)
    {
        var list = new SinglyLinkedList<int>();
        foreach (var n in Enumerable.Range(1, count)) list.AddLast(n);

        Assert.Equal(expected, list.Middle());
    }

    [Fact]
    public void LinkedList_MiddleOfEmpty_Throws() =>
        Assert.Throws<InvalidOperationException>(() => new SinglyLinkedList<int>().Middle());

    private static BinarySearchTree<int> BuildTree(params int[] values)
    {
        var tree = new BinarySearchTree<int>();
        foreach (var v in values) tree.Add(v);
        return tree;
    }

    [Fact]
    public void Bst_InOrder_IsSorted_AndIgnoresDuplicates()
    {
        var tree = BuildTree(50, 30, 70, 20, 40, 60, 80);

        Assert.False(tree.Add(40));
        Assert.Equal([20, 30, 40, 50, 60, 70, 80], tree.InOrder());
        Assert.Equal(7, tree.Count);
        Assert.Equal(3, tree.Height);
        Assert.Equal(20, tree.Min);
        Assert.Equal(80, tree.Max);
    }

    [Theory]
    [InlineData(20)] // leaf
    [InlineData(30)] // two children
    [InlineData(50)] // root with two children
    public void Bst_Remove_KeepsTreeOrdered(int value)
    {
        var tree = BuildTree(50, 30, 70, 20, 40, 60, 80);

        Assert.True(tree.Remove(value));
        Assert.False(tree.Contains(value));
        Assert.Equal(new[] { 20, 30, 40, 50, 60, 70, 80 }.Where(v => v != value), tree.InOrder());
        Assert.Equal(6, tree.Count);
    }

    [Fact]
    public void Bst_Remove_NodeWithOneChild()
    {
        var tree = BuildTree(10, 5, 3);

        Assert.True(tree.Remove(5));
        Assert.False(tree.Remove(99));
        Assert.Equal([3, 10], tree.InOrder());
    }

    [Fact]
    public void Bst_SortedInsertsDegradeToALine() =>
        Assert.Equal(5, BuildTree(1, 2, 3, 4, 5).Height);

    [Fact]
    public void Bst_EmptyTree_MinThrows() =>
        Assert.Throws<InvalidOperationException>(() => new BinarySearchTree<int>().Min);

    [Fact]
    public void Heap_PopsInAscendingOrder()
    {
        var values = new[] { 9, 4, 7, 1, 8, 2, 2, 6, 3, 5 };
        var heap = new MinHeap<int>();
        foreach (var v in values) heap.Push(v);

        var popped = new List<int>();
        while (!heap.IsEmpty) popped.Add(heap.Pop());

        Assert.Equal(values.Order(), popped);
    }

    [Fact]
    public void Heap_WithComparer_WorksAsPriorityQueue()
    {
        var byPriority = Comparer<(string Task, int Priority)>.Create((a, b) => a.Priority.CompareTo(b.Priority));
        var queue = new MinHeap<(string Task, int Priority)>(byPriority);
        queue.Push(("write docs", 3));
        queue.Push(("fix prod bug", 1));
        queue.Push(("code review", 2));

        Assert.Equal("fix prod bug", queue.Pop().Task);
        Assert.Equal("code review", queue.Peek().Task);
        Assert.Equal(2, queue.Count);
    }

    [Fact]
    public void Heap_PopOnEmpty_Throws() =>
        Assert.Throws<InvalidOperationException>(() => new MinHeap<int>().Pop());

    //   A — B — D
    //   |   |
    //   C — E    F (isolated)
    private static Graph<string> BuildGraph()
    {
        var graph = new Graph<string>();
        graph.AddEdge("A", "B");
        graph.AddEdge("A", "C");
        graph.AddEdge("B", "D");
        graph.AddEdge("B", "E");
        graph.AddEdge("C", "E");
        graph.AddVertex("F");
        return graph;
    }

    [Fact]
    public void Graph_BreadthFirst_VisitsByDistance() =>
        Assert.Equal(["A", "B", "C", "D", "E"], BuildGraph().BreadthFirst("A"));

    [Fact]
    public void Graph_DepthFirst_FollowsBranchFirst() =>
        Assert.Equal(["A", "B", "D", "E", "C"], BuildGraph().DepthFirst("A"));

    [Fact]
    public void Graph_UndirectedEdgesGoBothWays() =>
        Assert.Equal(["A", "D", "E"], BuildGraph().Neighbors("B"));

    [Fact]
    public void Graph_ShortestPath()
    {
        var graph = BuildGraph();

        Assert.Equal(["D", "B", "E"], graph.ShortestPath("D", "E"));
        Assert.Equal(["A"], graph.ShortestPath("A", "A"));
        Assert.Null(graph.ShortestPath("A", "F"));
        Assert.Null(graph.ShortestPath("A", "missing"));
    }

    [Fact]
    public void Graph_TopologicalSort_RespectsDependencies()
    {
        var build = new Graph<string>(directed: true);
        build.AddEdge("restore", "compile");
        build.AddEdge("compile", "test");
        build.AddEdge("compile", "pack");
        build.AddEdge("test", "publish");
        build.AddEdge("pack", "publish");

        var order = build.TopologicalSort().ToList();

        Assert.Equal(5, order.Count);
        Assert.Equal("restore", order[0]);
        Assert.Equal("publish", order[^1]);
        Assert.True(order.IndexOf("compile") < order.IndexOf("test"));
    }

    [Fact]
    public void Graph_TopologicalSort_DetectsCycle()
    {
        var graph = new Graph<int>(directed: true);
        graph.AddEdge(1, 2);
        graph.AddEdge(2, 3);
        graph.AddEdge(3, 1);

        Assert.Throws<InvalidOperationException>(() => graph.TopologicalSort());
    }

    [Fact]
    public void Graph_DirectedEdgesGoOneWay()
    {
        var graph = new Graph<int>(directed: true);
        graph.AddEdge(1, 2);

        Assert.Equal([1, 2], graph.BreadthFirst(1));
        Assert.Equal([2], graph.BreadthFirst(2));
    }
}
