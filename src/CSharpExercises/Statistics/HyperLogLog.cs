using System.Numerics;
using CSharpExercises.Hashing;

namespace CSharpExercises.Statistics;

/// <summary>
/// HyperLogLog estimates how many distinct items a stream contains using a few kilobytes, however large the stream.
/// If hashes are random, seeing a hash that starts with k zero bits suggests about 2^k distinct values. One such
/// guess is noisy, so the first bits of the hash pick one of m registers and the estimate averages them.
/// Standard error is about 1.04/√m: 1.6% with the default 4,096 registers.
/// </summary>
public sealed class HyperLogLog
{
    private readonly byte[] _registers;
    private readonly int _precision;

    public HyperLogLog(int precision = 12)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(precision, 4);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(precision, 18);
        _precision = precision;
        _registers = new byte[1 << precision];
    }

    public void Add(string item) => AddHash(StableHash.Of(item));

    public void AddHash(ulong hash)
    {
        var index = (int)(hash >> (64 - _precision));
        var rest = hash << _precision;
        var rank = (byte)(rest == 0 ? 64 - _precision + 1 : BitOperations.LeadingZeroCount(rest) + 1);
        if (rank > _registers[index]) _registers[index] = rank;
    }

    public long Estimate()
    {
        var m = _registers.Length;
        var alpha = 0.7213 / (1 + 1.079 / m);
        var sum = 0.0;
        var zeros = 0;
        foreach (var r in _registers)
        {
            sum += Math.Pow(2, -r);
            if (r == 0) zeros++;
        }

        var estimate = alpha * m * m / sum;

        // For small counts many registers are still empty; counting them (linear counting) is more accurate.
        if (estimate <= 2.5 * m && zeros > 0) estimate = m * Math.Log((double)m / zeros);
        return (long)Math.Round(estimate);
    }

    /// <summary>Union of two sketches is the register-wise maximum: count distinct users across two servers.</summary>
    public void Merge(HyperLogLog other)
    {
        if (other._precision != _precision) throw new ArgumentException("Precisions differ.", nameof(other));
        for (var i = 0; i < _registers.Length; i++) _registers[i] = Math.Max(_registers[i], other._registers[i]);
    }

    public int SizeInBytes => _registers.Length;
}
