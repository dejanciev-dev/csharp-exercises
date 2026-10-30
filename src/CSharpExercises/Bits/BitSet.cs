using System.Collections;
using System.Numerics;

namespace CSharpExercises.Bits;

/// <summary>
/// A fixed-size set of small integers, one bit each, packed 64 to a <see cref="ulong"/>. Union and intersection
/// handle 64 elements per instruction, and a million flags take 125 KB instead of a million bools' 1 MB.
/// </summary>
public sealed class BitSet : IEnumerable<int>
{
    private readonly ulong[] _words;

    public BitSet(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        Capacity = capacity;
        _words = new ulong[(capacity + 63) / 64];
    }

    public int Capacity { get; }

    public bool this[int index]
    {
        get => (_words[WordOf(index)] & (1UL << index)) != 0; // shifts by a ulong only use the low 6 bits
        set
        {
            if (value) _words[WordOf(index)] |= 1UL << index;
            else _words[WordOf(index)] &= ~(1UL << index);
        }
    }

    public int Count => _words.Sum(BitOperations.PopCount);

    public void UnionWith(BitSet other) => Combine(other, (a, b) => a | b);
    public void IntersectWith(BitSet other) => Combine(other, (a, b) => a & b);
    public void ExceptWith(BitSet other) => Combine(other, (a, b) => a & ~b);

    /// <summary>Jumps straight to set bits with trailing-zero counts instead of testing all of them.</summary>
    public IEnumerator<int> GetEnumerator()
    {
        for (var w = 0; w < _words.Length; w++)
        {
            for (var word = _words[w]; word != 0; word &= word - 1)
                yield return w * 64 + BitOperations.TrailingZeroCount(word);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>All primes below <paramref name="limit"/> with a sieve of Eratosthenes stored one bit per number.</summary>
    public static BitSet PrimesBelow(int limit)
    {
        var composite = new BitSet(Math.Max(limit, 2));
        composite[0] = composite[1] = true;
        for (var i = 2; (long)i * i < limit; i++)
        {
            if (composite[i]) continue;
            for (var j = i * i; j < limit; j += i) composite[j] = true;
        }

        var primes = new BitSet(limit);
        for (var i = 2; i < limit; i++) primes[i] = !composite[i];
        return primes;
    }

    private int WordOf(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Capacity);
        return index >> 6;
    }

    private void Combine(BitSet other, Func<ulong, ulong, ulong> op)
    {
        if (other.Capacity != Capacity) throw new ArgumentException("Bit sets must have the same capacity.", nameof(other));
        for (var i = 0; i < _words.Length; i++) _words[i] = op(_words[i], other._words[i]);
    }
}
