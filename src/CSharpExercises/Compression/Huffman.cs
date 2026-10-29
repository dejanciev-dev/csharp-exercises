namespace CSharpExercises.Compression;

/// <summary>
/// Huffman coding: frequent bytes get short bit codes, rare ones long codes, and no code is a prefix of another, so the
/// bit stream decodes without separators. The codes are canonical: only each byte's code length is stored, and both
/// sides rebuild the same codes from the lengths, so the header is a few bytes per symbol instead of the whole tree.
/// </summary>
/// <remarks>
/// Format: original length (varint), symbol count (varint), (symbol, code length) pairs, then the codes MSB first,
/// padded with zero bits to a whole byte. For inputs up to int.MaxValue bytes no code is longer than about 45 bits,
/// so codes fit in a ulong.
/// </remarks>
public static class Huffman
{
    private const int MaxCodeLength = 63;

    /// <summary>The code each byte gets, as text like "010", for looking at what the algorithm chose.</summary>
    public static Dictionary<byte, string> Codes(ReadOnlySpan<byte> data) =>
        Canonical(CodeLengths(data)).ToDictionary(
            c => c.Symbol,
            c => Convert.ToString((long)c.Code, 2).PadLeft(c.Length, '0'));

    public static byte[] Compress(ReadOnlySpan<byte> data)
    {
        var codes = Canonical(CodeLengths(data));
        var bySymbol = new (ulong Code, int Length)[256];
        foreach (var c in codes) bySymbol[c.Symbol] = (c.Code, c.Length);

        var output = new List<byte>();
        Span<byte> varint = stackalloc byte[Varint.MaxBytes];
        output.AddRange(varint[..Varint.Write(varint, (ulong)data.Length)]);
        output.AddRange(varint[..Varint.Write(varint, (ulong)codes.Length)]);
        foreach (var c in codes)
        {
            output.Add(c.Symbol);
            output.Add((byte)c.Length);
        }

        var writer = new BitWriter(output);
        foreach (var b in data) writer.Write(bySymbol[b].Code, bySymbol[b].Length);
        writer.Flush();

        return [.. output];
    }

    public static byte[] Decompress(ReadOnlySpan<byte> compressed)
    {
        if (!Varint.TryRead(compressed, out var originalLength, out var read)) throw new InvalidDataException("Missing length.");
        compressed = compressed[read..];
        if (!Varint.TryRead(compressed, out var symbolCount, out read) || symbolCount > 256)
            throw new InvalidDataException("Missing or invalid symbol count.");
        compressed = compressed[read..];

        if (compressed.Length < (int)symbolCount * 2) throw new InvalidDataException("Code table is truncated.");
        var lengths = new List<(byte Symbol, int Length)>();
        var seen = new bool[256];
        for (var i = 0; i < (int)symbolCount; i++)
        {
            var (symbol, length) = (compressed[2 * i], compressed[2 * i + 1]);
            if (length is 0 or > MaxCodeLength) throw new InvalidDataException($"Invalid code length {length}.");
            if (seen[symbol]) throw new InvalidDataException($"Symbol {symbol} appears twice.");
            seen[symbol] = true;
            lengths.Add((symbol, length));
        }
        var bits = compressed[((int)symbolCount * 2)..];

        // Every symbol costs at least one bit, so a length beyond the bits we have is corrupt: don't allocate for it.
        if (originalLength > (ulong)bits.Length * 8) throw new InvalidDataException("Length exceeds the encoded data.");
        if (originalLength > 0 && lengths.Count == 0) throw new InvalidDataException("No codes for a non-empty input.");

        var codes = Canonical(lengths);
        var maxLength = codes.Length == 0 ? 0 : codes[^1].Length;

        // Canonical codes of one length are consecutive numbers, so per length we need only the first code, how many
        // there are, and where they start in the sorted table.
        var firstCode = new ulong[maxLength + 1];
        var firstIndex = new int[maxLength + 1];
        var count = new int[maxLength + 1];
        for (var i = 0; i < codes.Length; i++)
        {
            var length = codes[i].Length;
            if (count[length]++ == 0)
            {
                firstCode[length] = codes[i].Code;
                firstIndex[length] = i;
            }
        }

        var output = new byte[originalLength];
        var reader = new BitReader(bits);
        for (var i = 0; i < output.Length; i++)
        {
            var code = 0UL;
            for (var length = 1; ; length++)
            {
                if (length > maxLength) throw new InvalidDataException($"Invalid code at byte {i}.");
                code = (code << 1) | reader.ReadBit();

                // Unsigned subtraction: a code below firstCode wraps to a huge number and fails the check too.
                var offset = code - firstCode[length];
                if (offset < (ulong)count[length])
                {
                    output[i] = codes[firstIndex[length] + (int)offset].Symbol;
                    break;
                }
            }
        }
        return output;
    }

    private static List<(byte Symbol, int Length)> CodeLengths(ReadOnlySpan<byte> data)
    {
        var frequencies = new long[256];
        foreach (var b in data) frequencies[b]++;

        // Ties are broken by insertion order, so the same input always produces the same tree.
        var queue = new PriorityQueue<Node, (long Weight, int Order)>();
        var order = 0;
        for (var s = 0; s < 256; s++)
        {
            if (frequencies[s] > 0) queue.Enqueue(new Leaf((byte)s, frequencies[s]), (frequencies[s], order++));
        }

        if (queue.Count == 0) return [];
        if (queue.Count == 1) return [(((Leaf)queue.Dequeue()).Symbol, 1)]; // a lone symbol still needs one bit

        // Repeatedly merge the two lightest subtrees: the rarest symbols end up deepest, with the longest codes.
        while (queue.Count > 1)
        {
            var (a, b) = (queue.Dequeue(), queue.Dequeue());
            var branch = new Branch(a, b);
            queue.Enqueue(branch, (branch.Weight, order++));
        }

        var lengths = new List<(byte, int)>();
        var stack = new Stack<(Node Node, int Depth)>([(queue.Dequeue(), 0)]);
        while (stack.TryPop(out var item))
        {
            switch (item.Node)
            {
                case Leaf leaf: lengths.Add((leaf.Symbol, item.Depth)); break;
                case Branch branch:
                    stack.Push((branch.Left, item.Depth + 1));
                    stack.Push((branch.Right, item.Depth + 1));
                    break;
            }
        }
        return lengths;
    }

    /// <summary>
    /// Assigns codes in order of (length, symbol): each code is the previous one plus one, shifted left when the length
    /// grows. Lengths that cannot form a prefix-free code (corrupt input) make a code overflow its length.
    /// </summary>
    private static (byte Symbol, int Length, ulong Code)[] Canonical(IEnumerable<(byte Symbol, int Length)> lengths)
    {
        var sorted = lengths.OrderBy(x => x.Length).ThenBy(x => x.Symbol).ToArray();
        var codes = new (byte, int, ulong)[sorted.Length];

        var code = 0UL;
        var previousLength = sorted.Length == 0 ? 0 : sorted[0].Length;
        for (var i = 0; i < sorted.Length; i++)
        {
            var (symbol, length) = sorted[i];
            code <<= length - previousLength;
            if (code >= 1UL << length) throw new InvalidDataException("Code lengths do not form a prefix-free code.");

            codes[i] = (symbol, length, code++);
            previousLength = length;
        }
        return codes;
    }

    private abstract record Node(long Weight);
    private sealed record Leaf(byte Symbol, long Weight) : Node(Weight);
    private sealed record Branch(Node Left, Node Right) : Node(Left.Weight + Right.Weight);

    private sealed class BitWriter(List<byte> output)
    {
        private int _current;
        private int _filled;

        public void Write(ulong code, int length)
        {
            for (var i = length - 1; i >= 0; i--)
            {
                _current = (_current << 1) | (int)((code >> i) & 1);
                if (++_filled == 8)
                {
                    output.Add((byte)_current);
                    _current = _filled = 0;
                }
            }
        }

        public void Flush()
        {
            if (_filled > 0) output.Add((byte)(_current << (8 - _filled)));
            _current = _filled = 0;
        }
    }

    private ref struct BitReader(ReadOnlySpan<byte> bytes)
    {
        private readonly ReadOnlySpan<byte> _bytes = bytes;
        private long _position;

        public ulong ReadBit()
        {
            if (_position >= _bytes.Length * 8L) throw new InvalidDataException("Compressed data ended early.");
            var bit = (_bytes[(int)(_position >> 3)] >> (7 - (int)(_position & 7))) & 1;
            _position++;
            return (ulong)bit;
        }
    }
}
