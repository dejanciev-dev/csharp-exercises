using CSharpExercises.Patterns;

namespace CSharpExercises.Tests;

public class ExtraPatternsTests
{
    [Fact]
    public void Memento_RestoresWithoutExposingState()
    {
        var canvas = new DrawingCanvas();
        var checkpoints = new Checkpoints(capacity: 2);
        var now = DateTimeOffset.UnixEpoch;

        canvas.AddLayer("background");
        checkpoints.Add("start", canvas.Save(now));
        canvas.AddLayer("sketch");
        canvas.SetBrush("ink");
        canvas.SetZoom(5000); // clamped to 800
        checkpoints.Add("sketched", canvas.Save(now));

        canvas.Restore(checkpoints.Get("start"));

        Assert.Equal(["background"], canvas.Layers);
        Assert.Equal(("pencil", 100), (canvas.Brush, canvas.Zoom));

        checkpoints.Add("third", canvas.Save(now));
        Assert.Equal(["sketched", "third"], checkpoints.Names); // oldest dropped
        Assert.Throws<KeyNotFoundException>(() => checkpoints.Get("start"));
    }

    [Fact]
    public void State_BehaviourChangesWithTheState()
    {
        var connection = new Connection();

        Assert.Throws<InvalidOperationException>(() => connection.Send("x")); // closed
        connection.Open();
        Assert.Equal("Opening", connection.State);
        Assert.Throws<InvalidOperationException>(() => connection.Send("x")); // not yet acknowledged
        connection.Acknowledge();
        connection.Send("hello");
        connection.Close();
        connection.Close(); // harmless when already closed

        Assert.Equal("Closed", connection.State);
        Assert.Equal(["SYN sent", "connected", "sent hello", "FIN sent"], connection.Log);
    }

    [Fact]
    public void Flyweight_SharesStyleObjects()
    {
        var styles = new StyleFactory();
        var document = new StyledDocument(styles);
        for (var i = 0; i < 1000; i++)
        {
            document.Append("Hello ", "Serif", 12);
            document.Append("world", "Serif", 12, bold: true);
        }

        Assert.Equal(11_000, document.Length);
        Assert.Equal(2, styles.DistinctStyles);
        Assert.Equal(2, document.DistinctStyleObjects); // 11,000 characters, two style objects
        Assert.Same(styles.Get("Serif", 12, false, "black"), styles.Get("Serif", 12, false, "black"));
    }

    [Fact]
    public void Prototype_DeepVersusShallowCopies()
    {
        var original = new ReportTemplate { Title = "Monthly", Sections = ["Summary", "Details"], Settings = { ["paper"] = "A4" } };

        var shallow = original.ShallowCopy();
        shallow.Sections.Add("Appendix");
        Assert.Equal(3, original.Sections.Count); // shared list: the original changed too

        var deep = original.DeepCopy();
        deep.Sections.Add("Glossary");
        deep.Settings["paper"] = "Letter";
        Assert.Equal(3, original.Sections.Count);
        Assert.Equal("A4", original.Settings["paper"]);
    }

    [Fact]
    public void Prototype_RegistryHandsOutCopies()
    {
        var registry = new TemplateRegistry();
        registry.Register("invoice", new ReportTemplate { Title = "Invoice", Sections = ["Lines", "Totals"] });

        var a = registry.Create("INVOICE");
        a.Sections.Clear();

        Assert.Equal(["Lines", "Totals"], registry.Create("invoice").Sections);
        Assert.Throws<KeyNotFoundException>(() => registry.Create("memo"));
    }

    [Fact]
    public void NullObject_RemovesNullChecks()
    {
        var log = new MemoryAuditLog();
        Assert.Equal(70, new TransferService(log).Transfer(100, 30));
        Assert.Equal(["transfer 30"], log.Entries);

        Assert.Equal(70, new TransferService().Transfer(100, 30)); // no log, no NullReferenceException

        var shoppers = new Dictionary<string, Shopper> { ["c1"] = new("Ada", 500) };
        Assert.Equal("Ada", Shoppers.FindOrGuest(shoppers, "c1").Name);
        Assert.Equal(0, Shoppers.FindOrGuest(shoppers, null).Credit);
        Assert.Same(Shoppers.Guest, Shoppers.FindOrGuest(shoppers, "missing"));
    }

    [Fact]
    public void Bridge_FiguresWorkWithEveryBackEnd()
    {
        IDrawingApi[] apis = [new SvgApi(), new CanvasApi()];
        var drawn = apis.Select(api => new Figure[] { new Dot(api, 1, 2), new Ring(api, 5, 5, 2.5), new Triangle(api, (0, 0), (4, 0), (0, 3)) })
            .Select(figures => figures.Select(f => f.Draw()).ToArray())
            .ToArray();

        Assert.Equal("<circle cx=\"5\" cy=\"5\" r=\"2.5\"/>", drawn[0][1]);
        Assert.Equal("ctx.arc(5, 5, 2.5, 0, 2 * Math.PI);", drawn[1][1]);
        Assert.Equal(3, drawn[0][2].Split("<line").Length - 1);
        Assert.Equal(3, drawn[1][2].Split("moveTo").Length - 1);
    }
}
