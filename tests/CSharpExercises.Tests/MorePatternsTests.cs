using CSharpExercises.Patterns;
using Microsoft.Extensions.Time.Testing;

namespace CSharpExercises.Tests;

public class MorePatternsTests
{
    [Fact]
    public void Command_UndoAndRedo()
    {
        var calc = new Calculator();
        var history = new CommandHistory();

        history.Execute(new AddCommand(calc, 10));
        history.Execute(new MultiplyCommand(calc, 0));
        Assert.Equal(0, calc.Value);

        history.Undo();
        Assert.Equal(10, calc.Value); // multiply by zero restored from the saved value
        history.Redo();
        Assert.Equal(0, calc.Value);

        history.Undo();
        history.Execute(new AddCommand(calc, 5)); // a new action clears redo
        Assert.False(history.CanRedo);
        Assert.Equal(["add 10", "add 5"], history.Log);
    }

    [Fact]
    public void Command_MacroUndoesAsOneStep()
    {
        var calc = new Calculator { Value = 2 };
        var history = new CommandHistory();
        var macro = new MacroCommand("double then add one", new MultiplyCommand(calc, 2), new AddCommand(calc, 1));

        history.Execute(macro);
        Assert.Equal(5, calc.Value);
        history.Undo();
        Assert.Equal(2, calc.Value);
        Assert.Throws<InvalidOperationException>(() => history.Undo());
    }

    [Theory]
    [InlineData(120, "general", true, "team lead")]
    [InlineData(1_200, "general", true, "manager")]
    [InlineData(20_000, "general", true, "director")]
    [InlineData(100_000, "general", false, "nobody")]
    [InlineData(10, "Gambling", false, "policy")]
    public void Chain_PassesUntilSomeoneDecides(decimal amount, string category, bool approved, string by)
    {
        var decision = ApprovalChain.CreateDefault().Handle(new Expense("item", amount, category));

        Assert.Equal((approved, by), (decision.Approved, decision.By));
    }

    private static readonly Section Doc = new([
        new Heading(1, "Guide"),
        new Paragraph("Read this <carefully>."),
        new Section([new Heading(2, "Steps"), new BulletList(["install it", "run it"])]),
    ]);

    [Fact]
    public void Visitor_RendersAndCounts()
    {
        Assert.Equal(
            "<section><h1>Guide</h1><p>Read this &lt;carefully&gt;.</p><section><h2>Steps</h2><ul><li>install it</li><li>run it</li></ul></section></section>",
            Doc.Accept(new HtmlRenderer()));
        Assert.Equal("# Guide\n\nRead this <carefully>.\n\n## Steps\n\n- install it\n- run it\n", Doc.Accept(new MarkdownRenderer()));
        Assert.Equal(9, Doc.Accept(new WordCounter()));
        Assert.Equal("Guide\n  Steps\n", Doc.Accept(new TableOfContents()).ToString());
    }

    [Fact]
    public void Composite_TreatsFilesAndFoldersAlike()
    {
        var root = new FolderEntry("project").Add(
            new FileEntry("README.md", 1_200),
            new FolderEntry("src").Add(new FileEntry("app.cs", 4_000), new FileEntry("util.cs", 800)),
            new FolderEntry("empty"));

        Assert.Equal(6_000, root.Size);
        Assert.Equal(["/project/src/app.cs"], root.Find(e => e is FileEntry && e.Size > 1_000 && e.Name.EndsWith(".cs")));
        Assert.Equal(
            "project\n├── README.md (1200 B)\n├── src\n│   ├── app.cs (4000 B)\n│   └── util.cs (800 B)\n└── empty\n",
            root.Render());
        Assert.Throws<InvalidOperationException>(() => root.Add(new FileEntry("README.md", 1)));
    }

    [Fact]
    public async Task Adapter_TranslatesTheLegacyApi()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero));
        var legacy = new LegacyThermometer(tenthsFahrenheit: 986); // 98.6 °F

        var (celsius, at) = await new LegacyThermometerAdapter(legacy, time).ReadAsync();

        Assert.Equal(37, celsius);
        Assert.Equal(time.GetUtcNow(), at);
        await Assert.ThrowsAsync<IOException>(() => new LegacyThermometerAdapter(new LegacyThermometer(int.MinValue), time).ReadAsync());

        var average = await TemperatureMonitor.AverageAsync([new LegacyThermometerAdapter(legacy, time), new DelegateTemperatureSource(() => 21, time)]);
        Assert.Equal(29, average);
    }

    [Fact]
    public void Proxy_LazyAndSecured()
    {
        var before = DocumentStore.Instances;
        var lazy = new LazyDocumentStore(() => new DocumentStore());
        Assert.False(lazy.IsCreated);
        Assert.Equal(before, DocumentStore.Instances);

        var role = "viewer";
        var secured = new SecuredDocumentStore(lazy, () => role);
        Assert.Throws<UnauthorizedAccessException>(() => secured.Save("a", "text"));

        role = "editor";
        secured.Save("a", "text");
        Assert.Equal("text", secured.Load("a"));
        Assert.True(lazy.IsCreated);
    }

    [Fact]
    public void Proxy_GeneratedAtRunTime()
    {
        var log = new List<string>();
        var store = LoggingProxy<IDocumentStore>.Create(new DocumentStore(), log);

        store.Save("x", "hello");
        Assert.Equal("hello", store.Load("x"));
        Assert.Throws<KeyNotFoundException>(() => store.Load("missing")); // the original exception, not a wrapper

        Assert.Equal(["Save(x, hello)", "Load(x)", "Load(missing)", "Load threw KeyNotFoundException"], log);
    }

    [Fact]
    public void TemplateMethod_SharesTheStepsAcrossFormats()
    {
        SalesRow[] rows = [new("North", 10, 1500m), new("South", 4, 2500.5m)];

        Assert.Equal("region,units,revenue\nSouth,4,2500.50\nNorth,10,1500.00\n", new CsvReportExporter().Export("Q3", rows));
        Assert.Equal(
            "## Q3\n\n| Region | Units | Revenue |\n|---|---:|---:|\n| South | 4 | 2500.50 |\n| North | 10 | 1500.00 |\n| **Total** | **14** | **4000.50** |\n\n_Sorted by revenue._\n",
            new MarkdownReportExporter().Export("Q3", rows));
    }

    [Fact]
    public void Mediator_RoutesMessages()
    {
        var room = new ChatRoom();
        var ana = room.Join("Ana");
        var ben = room.Join("Ben");
        var cy = room.Join("Cy");

        ana.Say("hi all");
        ben.Say("@cy psst");
        cy.Mute("Ana");
        ana.Say("anyone?");
        room.Leave(ben);

        Assert.Equal(["system: Ben joined", "system: Cy joined", "system: Ben left"], ana.Inbox);
        Assert.Equal(["system: Cy joined", "Ana: hi all", "Ana: anyone?"], ben.Inbox);
        Assert.Equal(["Ana: hi all", "Ben: (private) psst", "system: Ben left"], cy.Inbox);
        Assert.Throws<InvalidOperationException>(() => ana.Say("@nobody hello"));
    }
}
