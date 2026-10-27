using CSharpExercises.StateMachines;

namespace CSharpExercises.Tests;

public class StateMachinesTests
{
    private enum Door { Closed, Open, Locked }
    private enum DoorAction { Open, Close, Lock, Unlock }

    private static StateMachine<Door, DoorAction> CreateDoor(Func<bool>? hasKey = null)
    {
        var door = new StateMachine<Door, DoorAction>(Door.Closed);
        door.Configure(Door.Closed)
                .Permit(DoorAction.Open, Door.Open)
                .Permit(DoorAction.Lock, Door.Locked)
            .Configure(Door.Open)
                .Permit(DoorAction.Close, Door.Closed)
            .Configure(Door.Locked)
                .PermitIf(DoorAction.Unlock, Door.Closed, hasKey ?? (() => true), "you need the key");
        return door;
    }

    [Fact]
    public void Machine_MovesBetweenConfiguredStates()
    {
        var door = CreateDoor();

        door.Fire(DoorAction.Open);
        door.Fire(DoorAction.Close);
        door.Fire(DoorAction.Lock);

        Assert.Equal(Door.Locked, door.State);
        Assert.Equal(
            [(Door.Closed, DoorAction.Open, Door.Open), (Door.Open, DoorAction.Close, Door.Closed), (Door.Closed, DoorAction.Lock, Door.Locked)],
            door.History);
    }

    [Fact]
    public void Machine_RejectsTriggersNotAllowedInTheCurrentState()
    {
        var door = CreateDoor();
        door.Fire(DoorAction.Open);

        var ex = Assert.Throws<InvalidOperationException>(() => door.Fire(DoorAction.Lock));

        Assert.Equal("'Lock' is not allowed in state 'Open'.", ex.Message);
        Assert.Equal(Door.Open, door.State);
    }

    [Fact]
    public void Machine_GuardsBlockTransitionsAndExplainWhy()
    {
        var hasKey = false;
        var door = CreateDoor(() => hasKey);
        door.Fire(DoorAction.Lock);

        Assert.False(door.CanFire(DoorAction.Unlock));
        var ex = Assert.Throws<InvalidOperationException>(() => door.Fire(DoorAction.Unlock));
        Assert.EndsWith("you need the key.", ex.Message);

        hasKey = true;
        door.Fire(DoorAction.Unlock);
        Assert.Equal(Door.Closed, door.State);
    }

    [Fact]
    public void Machine_ListsPermittedTriggers()
    {
        var door = CreateDoor(() => false);

        Assert.Equal([DoorAction.Open, DoorAction.Lock], door.PermittedTriggers);

        door.Fire(DoorAction.Lock);
        Assert.Empty(door.PermittedTriggers); // Unlock is configured, but its guard fails
    }

    [Fact]
    public void Machine_RunsExitThenEntryActionsAndRaisesTheEvent()
    {
        List<string> log = [];
        var machine = new StateMachine<string, string>("idle");
        machine.Configure("idle").OnExit(() => log.Add("exit idle")).Permit("start", "running").Permit("ping", "idle")
               .Configure("running").OnEntry(() => log.Add("enter running"));
        machine.Transitioned += (from, trigger, to) => log.Add($"{from} --{trigger}--> {to}");

        machine.Fire("ping"); // same state: internal, no exit or entry
        machine.Fire("start");

        Assert.Equal(["idle --ping--> idle", "exit idle", "enter running", "idle --start--> running"], log);
    }

    [Fact]
    public void Workflow_FromDraftToPublished()
    {
        var doc = new DocumentWorkflow("ana") { Title = "Q3 report" };

        doc.Submit();
        doc.Approve("ben");
        Assert.Equal(DocumentState.InReview, doc.State);
        doc.Approve("cleo");
        Assert.Equal(DocumentState.Approved, doc.State);
        doc.Publish();

        Assert.Equal(DocumentState.Published, doc.State);
        Assert.Equal(1, doc.PublishedVersion);
        Assert.Equal(["ben", "cleo"], doc.ApprovedBy);
    }

    [Fact]
    public void Workflow_SubmitNeedsATitle()
    {
        var doc = new DocumentWorkflow("ana");

        var ex = Assert.Throws<InvalidOperationException>(doc.Submit);

        Assert.Contains("needs a title", ex.Message);
        Assert.DoesNotContain(DocumentAction.Submit, doc.AvailableActions);
    }

    [Fact]
    public void Workflow_AuthorsCannotApproveTheirOwnWork()
    {
        var doc = new DocumentWorkflow("ana") { Title = "T" };
        doc.Submit();

        Assert.Throws<InvalidOperationException>(() => doc.Approve("ANA"));
    }

    [Fact]
    public void Workflow_EachReviewerCountsOnce()
    {
        var doc = new DocumentWorkflow("ana") { Title = "T" };
        doc.Submit();
        doc.Approve("ben");

        Assert.Throws<InvalidOperationException>(() => doc.Approve("ben"));
        Assert.Equal(DocumentState.InReview, doc.State);
    }

    [Fact]
    public void Workflow_RequestingChangesStartsTheReviewOver()
    {
        var doc = new DocumentWorkflow("ana") { Title = "T" };
        doc.Submit();
        doc.Approve("ben");

        doc.RequestChanges();
        doc.Submit();

        Assert.Equal(DocumentState.InReview, doc.State);
        Assert.Empty(doc.ApprovedBy);
        doc.Approve("ben"); // allowed again in the new round
    }

    [Fact]
    public void Workflow_ArchiveAndRestoreLeadToANewVersion()
    {
        var doc = new DocumentWorkflow("ana") { Title = "T" };
        void Release()
        {
            doc.Submit();
            doc.Approve("ben");
            doc.Approve("cleo");
            doc.Publish();
        }

        Release();
        doc.Archive();
        Assert.Equal([DocumentAction.Restore], doc.AvailableActions);
        doc.Restore();
        Release();

        Assert.Equal(2, doc.PublishedVersion);
        Assert.Equal(2 * 4 + 2, doc.TransitionCount); // two releases of 4 steps, plus archive and restore
    }

    private static readonly VendingState Stocked = VendingState.Empty(new Dictionary<string, Product>
    {
        ["A1"] = new("Water", 100, 5),
        ["B2"] = new("Chocolate", 135, 0),
    });

    [Fact]
    public void Vending_SellsAndGivesChange()
    {
        var (state, effects) = VendingMachine.Run(Stocked, [new CoinInserted(100), new CoinInserted(50), new ProductSelected("A1")]);

        Assert.Equal([new Show("Credit: 1.00"), new Show("Credit: 1.50"), new Dispense("A1"), new ReturnCoins(50), new Show("Enjoy your Water")], effects);
        Assert.Equal(0, state.CreditCents);
        Assert.Equal(4, state.Products["A1"].Stock);
    }

    [Fact]
    public void Vending_NeverMutatesThePreviousState()
    {
        var (after, _) = VendingMachine.Run(Stocked, [new CoinInserted(100), new ProductSelected("A1")]);

        Assert.Equal(5, Stocked.Products["A1"].Stock);
        Assert.Equal(4, after.Products["A1"].Stock);
    }

    [Theory]
    [InlineData(50, "A1", "Insert 0.50 more")]
    [InlineData(200, "B2", "Chocolate is sold out")]
    [InlineData(200, "Z9", "Unknown product Z9")]
    public void Vending_ExplainsWhyNothingWasSold(int credit, string code, string message)
    {
        var (state, effects) = VendingMachine.Run(Stocked, [new CoinInserted(credit), new ProductSelected(code)]);

        Assert.Equal(new Show(message), effects[^1]);
        Assert.Equal(credit, state.CreditCents); // the money stays in the machine
    }

    [Fact]
    public void Vending_ReturnsUnacceptedCoinsAndCancelledCredit()
    {
        var (state, effects) = VendingMachine.Run(Stocked, [new CoinInserted(3), new CoinInserted(20), new CoinInserted(20), new CancelPressed(), new CancelPressed()]);

        Assert.Equal(
            [new ReturnCoins(3), new Show("Coin not accepted"), new Show("Credit: 0.20"), new Show("Credit: 0.40"), new ReturnCoins(40), new Show("Cancelled")],
            effects);
        Assert.Equal(0, state.CreditCents);
    }

    [Fact]
    public void Vending_RestockMakesASoldOutProductAvailable()
    {
        var (_, effects) = VendingMachine.Run(Stocked, [new Restocked("B2", 10), new CoinInserted(200), new ProductSelected("B2")]);

        Assert.Contains(new Dispense("B2"), effects);
        Assert.Contains(new ReturnCoins(65), effects);
    }

    [Fact]
    public void Vending_IgnoresInvalidRestocks()
    {
        var (state, effects) = VendingMachine.Handle(Stocked, new Restocked("A1", -3));

        Assert.Same(Stocked, state);
        Assert.Equal([new Show("Ignored")], effects);
    }
}
