using System.Numerics;
using CSharpExercises.LinearAlgebra;

namespace CSharpExercises.Tests;

public class LinearAlgebraTests
{
    private static readonly Matrix A = Matrix.FromRows([[1, 2, 3], [4, 5, 6]]);

    [Fact]
    public void Matrix_MultiplyAndTranspose()
    {
        var product = A * A.Transpose();

        Assert.Equal(Matrix.FromRows([[14, 32], [32, 77]]), product);
        Assert.Equal((3, 2), (A.Transpose().Rows, A.Transpose().Cols));
        Assert.Equal([14.0, 32], A.Multiply([1, 2, 3]));
        Assert.Equal(A, A.Transpose().Transpose());
        Assert.Throws<ArgumentException>(() => A * A);
    }

    [Fact]
    public void Matrix_IdentityIsNeutral()
    {
        Assert.Equal(A, Matrix.Identity(2) * A);
        Assert.Equal(A, A * Matrix.Identity(3));
        Assert.Equal(Matrix.FromRows([[2, 4, 6], [8, 10, 12]]), A + A);
    }

    [Fact]
    public void Gauss_SolvesALinearSystem()
    {
        // 2x + y - z = 8, -3x - y + 2z = -11, -2x + y + 2z = -3  →  x = 2, y = 3, z = -1
        var m = Matrix.FromRows([[2, 1, -1], [-3, -1, 2], [-2, 1, 2]]);

        var x = GaussianElimination.Solve(m, [8, -11, -3]);

        Assert.NotNull(x);
        Assert.Equal(2, x[0], 9);
        Assert.Equal(3, x[1], 9);
        Assert.Equal(-1, x[2], 9);
    }

    [Fact]
    public void Gauss_NeedsPivotingWhenTheFirstEntryIsZero()
    {
        var m = Matrix.FromRows([[0, 1], [1, 0]]);
        Assert.Equal([5.0, 7], GaussianElimination.Solve(m, [7, 5])!);
        Assert.Null(GaussianElimination.Solve(Matrix.FromRows([[1, 2], [2, 4]]), [1, 2]));
    }

    [Fact]
    public void Gauss_DeterminantAndInverse()
    {
        var m = Matrix.FromRows([[4, 7], [2, 6]]);

        Assert.Equal(10, GaussianElimination.Determinant(m), 9);
        Assert.Equal(-1, GaussianElimination.Determinant(Matrix.FromRows([[0, 1], [1, 0]])), 9);
        Assert.Equal(0, GaussianElimination.Determinant(Matrix.FromRows([[1, 2], [2, 4]])));

        var inverse = GaussianElimination.Inverse(m)!;
        Assert.True(inverse.ApproximatelyEquals(Matrix.FromRows([[0.6, -0.7], [-0.2, 0.4]])));
        Assert.True((m * inverse).ApproximatelyEquals(Matrix.Identity(2)));
        Assert.Null(GaussianElimination.Inverse(Matrix.FromRows([[1, 2], [2, 4]])));
    }

    [Fact]
    public void MatrixPower_FibonacciInLogTime()
    {
        Assert.Equal(new BigInteger[] { 0, 1, 1, 2, 3, 5, 8, 13 }, Enumerable.Range(0, 8).Select(n => MatrixPower.Fibonacci(n)));
        Assert.Equal(BigInteger.Parse("354224848179261915075"), MatrixPower.Fibonacci(100));
        Assert.Equal(new BigInteger[] { 0, 0, 1, 1, 2, 4, 7, 13, 24 }, Enumerable.Range(0, 9).Select(n => MatrixPower.Tribonacci(n)));
    }

    [Fact]
    public void MatrixPower_CountsWalks()
    {
        // A triangle: walks of length k from a vertex back to itself.
        var triangle = new bool[3, 3];
        for (var i = 0; i < 3; i++)
            for (var j = 0; j < 3; j++) triangle[i, j] = i != j;

        Assert.Equal(0, MatrixPower.CountWalks(triangle, 0, 0, 1));
        Assert.Equal(2, MatrixPower.CountWalks(triangle, 0, 0, 2));
        Assert.Equal(2, MatrixPower.CountWalks(triangle, 0, 0, 3));
        Assert.Equal(6, MatrixPower.CountWalks(triangle, 0, 0, 4));
    }

    [Fact]
    public void Grid_RotateAndSpiral()
    {
        int[,] grid = { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 } };

        Assert.Equal([1, 2, 3, 6, 9, 8, 7, 4, 5], GridTransforms.Spiral(grid));

        GridTransforms.RotateClockwise(grid);
        Assert.Equal(new[,] { { 7, 4, 1 }, { 8, 5, 2 }, { 9, 6, 3 } }, grid);

        int[,] wide = { { 1, 2, 3, 4 }, { 5, 6, 7, 8 } };
        Assert.Equal([1, 2, 3, 4, 8, 7, 6, 5], GridTransforms.Spiral(wide));
    }

    [Fact]
    public void Grid_SearchSortedAndZeroing()
    {
        int[,] sorted = { { 1, 4, 7 }, { 2, 5, 8 }, { 3, 6, 9 } };
        Assert.Equal((1, 1), GridTransforms.FindInSorted(sorted, 5));
        Assert.Null(GridTransforms.FindInSorted(sorted, 10));

        int[,] grid = { { 1, 1, 1 }, { 1, 0, 1 }, { 1, 1, 1 } };
        GridTransforms.ZeroRowsAndColumns(grid);
        Assert.Equal(new[,] { { 1, 0, 1 }, { 0, 0, 0 }, { 1, 0, 1 } }, grid);
    }

    [Fact]
    public void Transform_ComposesAndInverts()
    {
        var t = Transform2D.Translate(10, 0) * Transform2D.Rotate(90);
        var (x, y) = t.Apply(1, 0); // rotate (1, 0) to (0, 1), then move right

        Assert.Equal(10, x, 9);
        Assert.Equal(1, y, 9);

        var (bx, by) = t.Inverse()!.Value.Apply(x, y);
        Assert.Equal(1, bx, 9);
        Assert.Equal(0, by, 9);
        Assert.Null(Transform2D.Scale(0, 1).Inverse());
    }

    [Fact]
    public void Transform_RotatesAroundAPoint()
    {
        var (x, y) = Transform2D.RotateAround(180, 5, 5).Apply(6, 5);
        Assert.Equal(4, x, 9);
        Assert.Equal(5, y, 9);
        Assert.Equal(6, Transform2D.Scale(2, 3).Determinant); // areas grow by the determinant
    }

    [Fact]
    public void PageRank_RewardsIncomingLinks()
    {
        var links = new Dictionary<string, string[]>
        {
            ["home"] = ["about", "blog"],
            ["about"] = ["home"],
            ["blog"] = ["home", "about"],
            ["orphan"] = ["home"],
            ["deadend"] = [],
        };

        var rank = PageRank.Compute(links);

        Assert.Equal(1, rank.Values.Sum(), 9);
        Assert.Equal("home", rank.MaxBy(p => p.Value).Key);
        Assert.True(rank["orphan"] < rank["blog"]);
        Assert.Equal(rank["orphan"], rank["deadend"], 9); // neither has incoming links
    }
}
