using CSharpExercises.Puzzles;

namespace CSharpExercises.Tests;

public class PuzzlesTests
{
    [Fact]
    public void Hanoi_RecursiveAndIterativeAgree()
    {
        for (var n = 1; n <= 10; n++)
        {
            var recursive = TowerOfHanoi.Solve(n);
            Assert.Equal((1 << n) - 1, recursive.Count);
            Assert.True(TowerOfHanoi.IsValid(n, recursive));
            Assert.Equal(recursive, TowerOfHanoi.SolveIteratively(n));
        }
        Assert.False(TowerOfHanoi.IsValid(2, [(2, 'A', 'C')])); // disk 2 is under disk 1
    }

    [Fact]
    public void WordLadder_FindsAShortestLadder()
    {
        var ladder = new WordLadder(["cold", "cord", "card", "ward", "warm", "word", "worm", "corm", "wold", "hold"]);

        var path = ladder.ShortestLadder("COLD", "WARM")!;

        Assert.Equal(5, path.Count);
        Assert.Equal(("cold", "warm"), (path[0], path[^1]));
        Assert.All(path.Zip(path.Skip(1)), p => Assert.Equal(1, p.First.Zip(p.Second).Count(c => c.First != c.Second)));
        Assert.Null(ladder.ShortestLadder("cold", "heat"));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(8)]
    public void KnightsTour_FindsValidTours(int size)
    {
        var board = KnightsTour.Find(size)!;
        Assert.True(KnightsTour.IsValidTour(board));
    }

    [Fact]
    public void KnightsTour_ReportsImpossibleBoards()
    {
        Assert.Null(KnightsTour.Find(3));
        Assert.Null(KnightsTour.Find(4));
    }

    [Theory]
    [InlineData("123456708", 1)]
    [InlineData("123405786", 2)]
    [InlineData("867254301", 31)] // one of the hardest positions
    public void SlidingPuzzle_SolvesOptimally(string start, int moves)
    {
        var solution = SlidingPuzzle.Solve(start)!;

        Assert.Equal(moves, solution.Count);
        Assert.Equal(SlidingPuzzle.Goal, SlidingPuzzle.Apply(start, solution));
    }

    [Fact]
    public void SlidingPuzzle_DetectsUnsolvable()
    {
        Assert.False(SlidingPuzzle.IsSolvable("213456780")); // two tiles swapped
        Assert.Null(SlidingPuzzle.Solve("213456780"));
        Assert.Throws<ArgumentException>(() => SlidingPuzzle.Solve("112345678"));
    }

    [Fact]
    public void WaterJugs_MeasuresFourLitres()
    {
        var steps = WaterJugs.Solve(3, 5, 4)!;

        Assert.Equal(6, steps.Count);
        Assert.True(steps[^1].A == 4 || steps[^1].B == 4);
        Assert.True(WaterJugs.IsPossible(3, 5, 4));
        Assert.False(WaterJugs.IsPossible(6, 9, 4)); // only multiples of gcd 3
        Assert.Null(WaterJugs.Solve(6, 9, 4));
    }

    [Fact]
    public void Josephus_SimulationAndFormulasAgree()
    {
        Assert.Equal([3, 6, 2, 7, 5, 1, 4], Josephus.EliminationOrder(7, 3));
        Assert.Equal(4, Josephus.Survivor(7, 3));
        for (var n = 1; n <= 100; n++)
        {
            Assert.Equal(Josephus.EliminationOrder(n, 2)[^1], Josephus.Survivor(n, 2));
            Assert.Equal(Josephus.Survivor(n, 2), Josephus.SurvivorOfTwo(n));
        }
        Assert.Equal(Josephus.EliminationOrder(41, 3)[^1], Josephus.Survivor(41, 3)); // Josephus' own story: 31
        Assert.Equal(31, Josephus.Survivor(41, 3));
    }
}
