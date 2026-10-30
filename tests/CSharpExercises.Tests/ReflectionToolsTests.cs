using CSharpExercises.Reflection;

namespace CSharpExercises.Tests;

public class ReflectionToolsTests
{
    private sealed class TreeNode
    {
        public string Name { get; set; } = "";
        public TreeNode? Parent { get; set; }
        public List<TreeNode> Children { get; } = [];
        private int _secret = 7;
        public int Secret => _secret;
        public void SetSecret(int value) => _secret = value;
    }

    private sealed record Address(string City, string Zip);
    private sealed record Person(string Name, int Age, Address Address, List<string> Tags, DateTime UpdatedAt);

    [Fact]
    public void Dumper_PrintsNestedObjectsAndCycles()
    {
        var root = new TreeNode { Name = "root" };
        var child = new TreeNode { Name = "child", Parent = root };
        root.Children.Add(child);

        var dump = ObjectDumper.Dump(root);

        Assert.Equal(
            """
            TreeNode
              Name: "root"
              Parent: null
              Children: List<TreeNode> (1 items)
                [0]: TreeNode
                  Name: "child"
                  Parent: <cycle: TreeNode>
                  Children: List<TreeNode> (0 items)
                  Secret: 7
              Secret: 7
            """.ReplaceLineEndings("\n"), dump);
        Assert.Equal("42", ObjectDumper.Dump(42));
        Assert.Contains("{…}", ObjectDumper.Dump(root, maxDepth: 1));
    }

    [Fact]
    public void Cloner_CopiesDeeplyIncludingPrivateStateAndCycles()
    {
        var root = new TreeNode { Name = "root" };
        root.SetSecret(99);
        var child = new TreeNode { Name = "child", Parent = root };
        root.Children.Add(child);

        var copy = DeepCloner.Clone(root);
        copy.Children[0].Name = "changed";

        Assert.NotSame(root, copy);
        Assert.Equal("child", root.Children[0].Name);       // the original is untouched
        Assert.Same(copy, copy.Children[0].Parent);         // the cycle points at the clone, not the original
        Assert.Equal(99, copy.Secret);                      // private fields copied
        Assert.Equal(new[,] { { 1, 2 }, { 3, 4 } }, DeepCloner.Clone(new[,] { { 1, 2 }, { 3, 4 } }));
    }

    [Fact]
    public void Diff_ReportsChangedPaths()
    {
        var before = new Person("Ada", 36, new Address("London", "W1"), ["math", "poetry"], DateTime.UnixEpoch);
        var after = before with { Age = 37, Address = before.Address with { City = "Paris" }, Tags = ["math", "engines", "music"], UpdatedAt = DateTime.UtcNow };

        var changes = ObjectDiff.Compare(before, after, ignore: "UpdatedAt");

        Assert.Equal(["Age: 36 → 37", "Address.City: London → Paris", "Tags[1]: poetry → engines", "Tags[2]: (none) → music"], ObjectDiff.Describe(changes));
        Assert.Empty(ObjectDiff.Compare(before, before with { Tags = ["math", "poetry"] }, "UpdatedAt")); // equal contents, new list
    }

    private sealed class Order
    {
        public int Id { get; set; }
        public string Customer { get; set; } = "";
        public decimal Net { get; set; }
        public decimal Tax { get; set; }
        public string Internal { get; set; } = "secret";
    }

    private sealed class OrderDto
    {
        public long Id { get; set; }          // int → long is converted
        public string Customer { get; set; } = "";
        public decimal Total { get; set; }    // computed by a rule
        public string? Status { get; set; }   // nobody fills this
    }

    [Fact]
    public void Mapper_CopiesByNameAndAppliesRules()
    {
        var mapper = new ObjectMapper<Order, OrderDto>().ForMember(d => d.Total, s => s.Net + s.Tax);

        var dto = mapper.Map(new Order { Id = 5, Customer = "Ada", Net = 100, Tax = 19 });

        Assert.Equal((5L, "Ada", 119m), (dto.Id, dto.Customer, dto.Total));
        Assert.Equal(["Status"], mapper.UnmappedMembers());
        Assert.Empty(mapper.Ignore(d => d.Status).UnmappedMembers());
    }

    [Fact]
    public void Mapper_IsFastAfterCompilation()
    {
        var mapper = new ObjectMapper<Order, OrderDto>().ForMember(d => d.Total, s => s.Net + s.Tax);
        var order = new Order { Id = 1, Customer = "x", Net = 1, Tax = 1 };
        mapper.Map(order); // compile once

        var watch = System.Diagnostics.Stopwatch.StartNew();
        for (var i = 0; i < 100_000; i++) mapper.Map(order);

        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(2));
    }
}
