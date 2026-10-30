namespace CSharpExercises.LinearAlgebra;

/// <summary>
/// Solving Ax = b, determinants and inverses with Gaussian elimination. Partial pivoting (swap the row with the
/// largest value into the pivot position) avoids dividing by tiny numbers, which would amplify rounding errors.
/// </summary>
public static class GaussianElimination
{
    private const double Epsilon = 1e-12;

    /// <summary>Returns null when the system has no unique solution (A is singular).</summary>
    public static double[]? Solve(Matrix a, double[] b)
    {
        var n = a.Rows;
        if (a.Cols != n || b.Length != n) throw new ArgumentException("Need a square matrix and a matching vector.");

        // The augmented matrix [A | b].
        var m = new double[n, n + 1];
        for (var r = 0; r < n; r++)
        {
            for (var c = 0; c < n; c++) m[r, c] = a[r, c];
            m[r, n] = b[r];
        }

        for (var col = 0; col < n; col++)
        {
            var pivot = PivotRow(m, col, n);
            if (Math.Abs(m[pivot, col]) < Epsilon) return null;
            SwapRows(m, col, pivot);

            for (var r = col + 1; r < n; r++)
            {
                var factor = m[r, col] / m[col, col];
                for (var c = col; c <= n; c++) m[r, c] -= factor * m[col, c];
            }
        }

        // Back substitution, from the last row up.
        var x = new double[n];
        for (var r = n - 1; r >= 0; r--)
        {
            var sum = m[r, n];
            for (var c = r + 1; c < n; c++) sum -= m[r, c] * x[c];
            x[r] = sum / m[r, r];
        }
        return x;
    }

    /// <summary>The product of the pivots, with a sign flip for every row swap.</summary>
    public static double Determinant(Matrix a)
    {
        var n = a.Rows;
        if (a.Cols != n) throw new ArgumentException("Need a square matrix.", nameof(a));
        var m = ToArray(a);
        var det = 1.0;

        for (var col = 0; col < n; col++)
        {
            var pivot = PivotRow(m, col, n);
            if (Math.Abs(m[pivot, col]) < Epsilon) return 0;
            if (pivot != col)
            {
                SwapRows(m, col, pivot);
                det = -det;
            }
            det *= m[col, col];
            for (var r = col + 1; r < n; r++)
            {
                var factor = m[r, col] / m[col, col];
                for (var c = col; c < n; c++) m[r, c] -= factor * m[col, c];
            }
        }
        return det;
    }

    /// <summary>Gauss–Jordan: reduce [A | I] to [I | A⁻¹].</summary>
    public static Matrix? Inverse(Matrix a)
    {
        var n = a.Rows;
        if (a.Cols != n) throw new ArgumentException("Need a square matrix.", nameof(a));
        var m = new double[n, 2 * n];
        for (var r = 0; r < n; r++)
        {
            for (var c = 0; c < n; c++) m[r, c] = a[r, c];
            m[r, n + r] = 1;
        }

        for (var col = 0; col < n; col++)
        {
            var pivot = PivotRow(m, col, n);
            if (Math.Abs(m[pivot, col]) < Epsilon) return null;
            SwapRows(m, col, pivot);

            var scale = m[col, col];
            for (var c = 0; c < 2 * n; c++) m[col, c] /= scale;

            for (var r = 0; r < n; r++)
            {
                if (r == col || m[r, col] == 0) continue;
                var factor = m[r, col];
                for (var c = 0; c < 2 * n; c++) m[r, c] -= factor * m[col, c];
            }
        }

        var result = new double[n * n];
        for (var r = 0; r < n; r++)
            for (var c = 0; c < n; c++) result[r * n + c] = m[r, n + c];
        return new Matrix(n, n, result);
    }

    private static int PivotRow(double[,] m, int col, int rows)
    {
        var best = col;
        for (var r = col + 1; r < rows; r++)
            if (Math.Abs(m[r, col]) > Math.Abs(m[best, col])) best = r;
        return best;
    }

    private static void SwapRows(double[,] m, int a, int b)
    {
        if (a == b) return;
        for (var c = 0; c < m.GetLength(1); c++) (m[a, c], m[b, c]) = (m[b, c], m[a, c]);
    }

    private static double[,] ToArray(Matrix a)
    {
        var m = new double[a.Rows, a.Cols];
        for (var r = 0; r < a.Rows; r++)
            for (var c = 0; c < a.Cols; c++) m[r, c] = a[r, c];
        return m;
    }
}
