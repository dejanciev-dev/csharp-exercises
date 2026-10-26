using CSharpExercises.Oop;

namespace CSharpExercises.Tests;

public class OopTests
{
    [Fact]
    public void Shapes_ComputeAreaPolymorphically()
    {
        Shape[] shapes = [new Circle(1), new Rectangle(2, 3), new Square(4)];

        Assert.Equal(Math.PI + 6 + 16, shapes.Sum(s => s.Area), precision: 10);
        Assert.Equal(16, shapes[2].Perimeter);
    }

    [Fact]
    public void Shapes_RejectNonPositiveDimensions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Circle(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Rectangle(-1, 2));
    }

    [Fact]
    public void BankAccount_DepositAndWithdraw_TrackTransactions()
    {
        var account = new BankAccount("Dejan");
        account.Deposit(100);
        account.Withdraw(30);

        Assert.Equal(70, account.Balance);
        Assert.Equal([100m, -30m], account.Transactions.Select(t => t.Amount));
    }

    [Fact]
    public void BankAccount_CannotOverdraw()
    {
        var account = new BankAccount("Dejan");
        account.Deposit(50);

        Assert.Throws<InvalidOperationException>(() => account.Withdraw(51));
        Assert.Equal(50, account.Balance);
    }

    [Fact]
    public void BankAccount_RejectsNonPositiveAmounts() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new BankAccount("Dejan").Deposit(0));

    [Fact]
    public void BankAccount_Transfer_MovesMoney()
    {
        var from = new BankAccount("A");
        var to = new BankAccount("B");
        from.Deposit(200);

        from.TransferTo(to, 75);

        Assert.Equal(125, from.Balance);
        Assert.Equal(75, to.Balance);
    }
}
