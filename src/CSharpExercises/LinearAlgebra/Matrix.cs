using System.Text;

namespace CSharpExercises.LinearAlgebra;

/// <summary>
/// An immutable dense matrix of doubles with the basic operations. Stored row-major in one flat array, which keeps
/// rows contiguous in memory; multiplication uses the i-k-j loop order so the innermost loop walks rows, not columns.
/// </summary>
public sealed class Matrix : IEquatable<Matrix>
{
    private readonly double[] _data;

    public Matrix(int rows, int cols, double[]? data = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rows, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(cols, 1);
        if (data is not null && data.Length != rows * cols) throw new ArgumentException("Wrong number of values.", nameof(data));
        Rows = rows;
        Cols = cols;
        _data = data is null ? new double[rows * cols] : [.. data];
    }

    public static Matrix FromRows(double[][] rows) =>
        new(rows.Length, rows[0].Length, rows.All(r => r.Length == rows[0].Length)
            ? rows.SelectMany(r => r).ToArray()
            : throw new ArgumentException("Rows have different lengths.", nameof(rows)));

    public static Matrix Identity(int n)
    {
        var data = new double[n * n];
        for (var i = 0; i < n; i++) data[i * n + i] = 1;
        return new Matrix(n, n, data);
    }

    public int Rows { get; }
    public int Cols { get; }

    public double this[int row, int col] => _data[row * Cols + col];

    public Matrix Transpose()
    {
        var result = new double[_data.Length];
        for (var r = 0; r < Rows; r++)
            for (var c = 0; c < Cols; c++)
                result[c * Rows + r] = this[r, c];
        return new Matrix(Cols, Rows, result);
    }

    public static Matrix operator +(Matrix a, Matrix b)
    {
        if (a.Rows != b.Rows || a.Cols != b.Cols) throw new ArgumentException("Matrices must have the same shape.");
        return new Matrix(a.Rows, a.Cols, a._data.Zip(b._data, (x, y) => x + y).ToArray());
    }

    public static Matrix operator *(Matrix a, double k) => new(a.Rows, a.Cols, a._data.Select(x => x * k).ToArray());

    public static Matrix operator *(Matrix a, Matrix b)
    {
        if (a.Cols != b.Rows) throw new ArgumentException($"Can't multiply {a.Rows}×{a.Cols} by {b.Rows}×{b.Cols}.");
        var result = new double[a.Rows * b.Cols];
        for (var i = 0; i < a.Rows; i++)
            for (var k = 0; k < a.Cols; k++)
            {
                var aik = a[i, k];
                for (var j = 0; j < b.Cols; j++) result[i * b.Cols + j] += aik * b[k, j];
            }
        return new Matrix(a.Rows, b.Cols, result);
    }

    public double[] Multiply(double[] vector)
    {
        if (vector.Length != Cols) throw new ArgumentException("Vector length must match the column count.", nameof(vector));
        var result = new double[Rows];
        for (var r = 0; r < Rows; r++)
            for (var c = 0; c < Cols; c++)
                result[r] += this[r, c] * vector[c];
        return result;
    }

    public bool ApproximatelyEquals(Matrix other, double tolerance = 1e-9) =>
        Rows == other.Rows && Cols == other.Cols && _data.Zip(other._data).All(p => Math.Abs(p.First - p.Second) <= tolerance);

    public bool Equals(Matrix? other) => other is not null && Rows == other.Rows && _data.AsSpan().SequenceEqual(other._data);
    public override bool Equals(object? obj) => Equals(obj as Matrix);
    public override int GetHashCode() => HashCode.Combine(Rows, Cols, _data.Length > 0 ? _data[0] : 0);

    public override string ToString()
    {
        var sb = new StringBuilder();
        for (var r = 0; r < Rows; r++)
            sb.AppendLine(string.Join(' ', Enumerable.Range(0, Cols).Select(c => this[r, c].ToString("0.###", System.Globalization.CultureInfo.InvariantCulture))));
        return sb.ToString();
    }

    internal double[] CopyData() => [.. _data];
}
