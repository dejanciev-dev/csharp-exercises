using CSharpExercises.Algorithms;

namespace CSharpExercises.Tests;

public class BacktrackingTests
{
    private const string Easy = """
        53. .7. ...
        6.. 195 ...
        .98 ... .6.
        8.. .6. ..3
        4.. 8.3 ..1
        7.. .2. ..6
        .6. ... 28.
        ... 419 ..5
        ... .8. .79
        """;

    // Published by Arto Inkala as one of the hardest sudokus for humans.
    private const string Hard = """
        8.. ... ...
        ..3 6.. ...
        .7. .9. 2..
        .5. ..7 ...
        ... .45 7..
        ... 1.. .3.
        ..1 ... .68
        ..8 5.. .1.
        .9. ... 4..
        """;

    private static void AssertValidSolution(string puzzle, int[,]? grid)
    {
        Assert.NotNull(grid);
        var givens = puzzle.Where(ch => ch is '.' or (>= '1' and <= '9')).ToArray();

        for (var i = 0; i < 9; i++)
        {
            Assert.Equal(Enumerable.Range(1, 9), Enumerable.Range(0, 9).Select(c => grid[i, c]).Order());
            Assert.Equal(Enumerable.Range(1, 9), Enumerable.Range(0, 9).Select(r => grid[r, i]).Order());
            Assert.Equal(Enumerable.Range(1, 9), Enumerable.Range(0, 9).Select(k => grid[i / 3 * 3 + k / 3, i % 3 * 3 + k % 3]).Order());
        }

        for (var cell = 0; cell < 81; cell++)
        {
            if (givens[cell] != '.') Assert.Equal(givens[cell] - '0', grid[cell / 9, cell % 9]);
        }
    }

    [Fact]
    public void Permutations_OfDistinctItems()
    {
        var result = Backtracking.Permutations([1, 2, 3]);

        Assert.Equal(6, result.Count);
        Assert.Equal([1, 2, 3], result[0]);
        Assert.Equal([3, 2, 1], result[^1]);
    }

    [Fact]
    public void Permutations_SkipDuplicateOrderings()
    {
        var result = Backtracking.Permutations(['a', 'a', 'b']).Select(p => new string(p));

        Assert.Equal(["aab", "aba", "baa"], result);
    }

    [Fact]
    public void Permutations_CountMatchesTheFormula()
    {
        // "MISSISSIPPI" has 11! / (4! * 4! * 2!) distinct arrangements.
        Assert.Equal(34_650, Backtracking.Permutations("MISSISSIPPI".ToArray()).Count);
    }

    [Fact]
    public void Subsets_ListsAllTwoToTheN()
    {
        var subsets = Backtracking.Subsets(["a", "b", "c"]).Select(s => string.Concat(s));

        Assert.Equal(["", "a", "ab", "abc", "ac", "b", "bc", "c"], subsets);
        Assert.Equal(1024, Backtracking.Subsets(Enumerable.Range(0, 10).ToArray()).Count);
    }

    [Fact]
    public void CombinationSum_ReusesCandidates()
    {
        var result = Backtracking.CombinationSum([2, 3, 6, 7], 7);

        Assert.Collection(result,
            c => Assert.Equal([2, 2, 3], c),
            c => Assert.Equal([7], c));
    }

    [Fact]
    public void CombinationSum_NoSolution()
    {
        Assert.Empty(Backtracking.CombinationSum([4, 6], 9));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 0)]
    [InlineData(3, 0)]
    [InlineData(4, 2)]
    [InlineData(6, 4)]
    [InlineData(8, 92)]
    public void NQueens_CountsSolutions(int n, int expected)
    {
        Assert.Equal(expected, Backtracking.NQueens(n).Count);
    }

    [Fact]
    public void NQueens_SolutionsAreValid()
    {
        foreach (var queens in Backtracking.NQueens(8))
        {
            for (var a = 0; a < 8; a++)
            for (var b = a + 1; b < 8; b++)
            {
                Assert.NotEqual(queens[a], queens[b]);
                Assert.NotEqual(b - a, Math.Abs(queens[b] - queens[a]));
            }
        }
    }

    [Fact]
    public void NQueens_Draw()
    {
        Assert.Equal(".Q..\n...Q\nQ...\n..Q.", Backtracking.Draw(Backtracking.NQueens(4)[0]));
    }

    [Fact]
    public void Sudoku_SolvesAnEasyPuzzle()
    {
        AssertValidSolution(Easy, Sudoku.Solve(Easy));
    }

    [Fact]
    public void Sudoku_SolvesAHardPuzzle()
    {
        AssertValidSolution(Hard, Sudoku.Solve(Hard));
    }

    [Fact]
    public void Sudoku_EmptyGridHasASolution()
    {
        var empty = new string('.', 81);

        AssertValidSolution(empty, Sudoku.Solve(empty));
    }

    [Fact]
    public void Sudoku_RepeatedGivenIsUnsolvable()
    {
        Assert.Null(Sudoku.Solve("55" + new string('.', 79)));
    }

    [Fact]
    public void Sudoku_ConsistentGivensCanStillBeUnsolvable()
    {
        // The last cell of row 1 can only be 9, but column 9 already has a 9.
        var puzzle = "12345678." + "........9" + new string('.', 63);

        Assert.Null(Sudoku.Solve(puzzle));
    }

    [Fact]
    public void Sudoku_RejectsMalformedInput()
    {
        Assert.Throws<FormatException>(() => Sudoku.Solve("123"));
        Assert.Throws<FormatException>(() => Sudoku.Solve("x" + new string('.', 80)));
    }

    [Fact]
    public void Sudoku_FormatDrawsTheBoxes()
    {
        var lines = Sudoku.Format(Sudoku.Solve(Easy)!).Split(Environment.NewLine);

        Assert.Equal("5 3 4 | 6 7 8 | 9 1 2", lines[0]);
        Assert.Equal("------+-------+------", lines[3]);
    }
}
