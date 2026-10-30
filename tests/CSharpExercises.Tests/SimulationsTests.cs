using CSharpExercises.Simulations;

namespace CSharpExercises.Tests;

public class SimulationsTests
{
    [Fact]
    public void Life_BlinkerOscillates()
    {
        var life = GameOfLife.FromPattern("###");

        life.Step();
        Assert.Equal("#\n#\n#", life.Render());
        life.Step();
        Assert.Equal("###", life.Render());
    }

    [Fact]
    public void Life_GliderTravelsDiagonally()
    {
        var life = GameOfLife.FromPattern(".#.", "..#", "###");
        var shape = life.Normalised();
        var start = life.Alive.Min();

        life.Run(4);

        Assert.Equal(shape, life.Normalised());          // same shape…
        Assert.Equal((start.X + 1, start.Y + 1), life.Alive.Min()); // …one cell down and right
        Assert.Equal(4, life.Generation);
    }

    [Fact]
    public void Life_BlockIsStillAndLonelyCellsDie()
    {
        var block = GameOfLife.FromPattern("##", "##");
        block.Step();
        Assert.Equal(4, block.Alive.Count);

        var lonely = GameOfLife.FromPattern("#...#");
        lonely.Step();
        Assert.Empty(lonely.Alive);
    }

    [Fact]
    public void Automaton_Rule90DrawsSierpinski()
    {
        var lines = ElementaryAutomaton.Draw(90, 3);

        Assert.Equal(["   #", "  # #", " #   #", "# # # #"], lines);
    }

    [Fact]
    public void Automaton_Rule30CentreColumn()
    {
        var bits = ElementaryAutomaton.Rule30Bits(16).Select(b => b ? '1' : '0');
        Assert.Equal("1101110011000101", string.Concat(bits)); // OEIS A051023
        Assert.Equal([true, true, true, false, false], ElementaryAutomaton.Step([false, true, false, false, false], 30));
    }

    [Fact]
    public void TicTacToe_TakesTheWinAndBlocks()
    {
        Assert.Equal((2, 1), TicTacToe.BestMove("XX.OO...."));  // X to move wins at 2
        Assert.Equal(5, TicTacToe.BestMove("XX.OO.X..").Square); // O wins at 5 rather than blocking
        Assert.Equal(2, TicTacToe.BestMove("XX..O....").Square); // O must block
    }

    [Fact]
    public void TicTacToe_PerfectPlayIsADraw()
    {
        var final = TicTacToe.SelfPlay();

        Assert.True(TicTacToe.IsDraw(final));
        Assert.Equal(0, TicTacToe.BestMove(".........").Outcome);
        Assert.Throws<ArgumentException>(() => TicTacToe.BestMove("OO......."));
        Assert.Throws<InvalidOperationException>(() => TicTacToe.BestMove("XXXOO...."));
    }

    [Fact]
    public void Maze_IsPerfectAndSolvable()
    {
        var maze = new Maze(15, 20, new Random(1));

        Assert.Equal(15 * 20 - 1, maze.PassageCount());
        var path = maze.Solve();
        Assert.Equal((0, 0), path[0]);
        Assert.Equal((14, 19), path[^1]);
        Assert.All(path.Zip(path.Skip(1)), step =>
            Assert.True(maze.CanMove(step.First.Row, step.First.Col, step.Second.Row - step.First.Row, step.Second.Col - step.First.Col)));
    }

    [Fact]
    public void Maze_IsReproducibleFromTheSeed()
    {
        Assert.Equal(new Maze(5, 5, new Random(7)).Render(), new Maze(5, 5, new Random(7)).Render());
        Assert.NotEqual(new Maze(5, 5, new Random(7)).Render(), new Maze(5, 5, new Random(8)).Render());
    }

    [Fact]
    public void FloodFill_PaintsOneRegion()
    {
        var image = FloodFill.ToGrid(["aab", "abb", "bba"]);

        Assert.Equal(5, FloodFill.Fill(image, 0, 2, 'c'));
        Assert.Equal('c', image[2, 0]);
        Assert.Equal('a', image[2, 2]); // diagonal neighbours aren't connected
    }

    [Fact]
    public void FloodFill_CountsIslands()
    {
        string[] map =
        [
            "##...#",
            "##..##",
            "......",
            "..###.",
            "#.#...",
        ];

        Assert.Equal((4, 4), FloodFill.Islands(map));
    }

    [Fact]
    public void FloodFill_CapturesSurroundedRegions()
    {
        string[] board = ["XXXX", "XOOX", "XXOX", "XOXX"];
        Assert.Equal(["XXXX", "XXXX", "XXXX", "XOXX"], FloodFill.CaptureSurrounded(board));
    }

    [Fact]
    public void Queue_MatchesTheoryAndExplodesNearFullLoad()
    {
        var moderate = QueueSimulation.Run(1, arrivalsPerMinute: 0.5, minutesPerService: 1, customers: 200_000, new Random(1));
        var busy = QueueSimulation.Run(1, arrivalsPerMinute: 0.95, minutesPerService: 1, customers: 200_000, new Random(1));
        var twoTellers = QueueSimulation.Run(2, arrivalsPerMinute: 0.95, minutesPerService: 1, customers: 200_000, new Random(1));

        Assert.InRange(moderate.AverageWait, 0.95, 1.05); // theory: 1 minute
        Assert.Equal(1, QueueSimulation.TheoreticalWaitSingleServer(0.5, 1), 9);
        Assert.InRange(moderate.Utilisation, 0.49, 0.51);

        Assert.True(busy.AverageWait > 10 * moderate.AverageWait); // 50% → 95% busy: about 19× the wait
        Assert.True(twoTellers.AverageWait < 0.5);                 // a second teller fixes it
    }
}
