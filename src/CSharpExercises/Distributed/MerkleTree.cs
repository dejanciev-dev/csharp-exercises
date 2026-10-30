using System.Security.Cryptography;
using System.Text;

namespace CSharpExercises.Distributed;

/// <summary>
/// A Merkle tree hashes data blocks, then hashes pairs of hashes up to a single root. Two replicas compare roots; if
/// they differ, they compare children and descend only into differing branches, finding the changed blocks in
/// O(log n) exchanges (Cassandra anti-entropy, Git, BitTorrent). A proof that a block is included needs only the
/// sibling hashes on its path to the root.
/// </summary>
public sealed class MerkleTree
{
    private readonly byte[][][] _levels; // _levels[0] = leaf hashes, last level = [root]

    public MerkleTree(IReadOnlyList<string> blocks)
    {
        if (blocks.Count == 0) throw new ArgumentException("Need at least one block.", nameof(blocks));
        var levels = new List<byte[][]> { blocks.Select(b => Hash([0x00], Encoding.UTF8.GetBytes(b))).ToArray() };

        while (levels[^1].Length > 1)
        {
            var below = levels[^1];
            var level = new byte[(below.Length + 1) / 2][];
            for (var i = 0; i < level.Length; i++)
            {
                var right = 2 * i + 1 < below.Length ? below[2 * i + 1] : below[2 * i]; // odd one out pairs with itself
                level[i] = Hash([0x01], below[2 * i], right);
            }
            levels.Add(level);
        }
        _levels = [.. levels];
    }

    public string Root => Convert.ToHexString(_levels[^1][0]);
    public int LeafCount => _levels[0].Length;

    /// <summary>Indices of blocks that differ, descending only into subtrees whose hashes disagree.</summary>
    public IReadOnlyList<int> Diff(MerkleTree other, out int comparisons)
    {
        if (other.LeafCount != LeafCount) throw new ArgumentException("Trees must have the same number of blocks.", nameof(other));
        var differing = new List<int>();
        var count = 0;

        void Visit(int level, int index)
        {
            count++;
            if (_levels[level][index].AsSpan().SequenceEqual(other._levels[level][index])) return;
            if (level == 0)
            {
                differing.Add(index);
                return;
            }
            Visit(level - 1, 2 * index);
            if (2 * index + 1 < _levels[level - 1].Length) Visit(level - 1, 2 * index + 1);
        }

        Visit(_levels.Length - 1, 0);
        comparisons = count;
        return differing;
    }

    /// <summary>The sibling hashes from a leaf up to the root, with which side each one is on.</summary>
    public IReadOnlyList<(byte[] Hash, bool IsLeft)> Proof(int leaf)
    {
        var proof = new List<(byte[], bool)>();
        var index = leaf;
        for (var level = 0; level < _levels.Length - 1; level++)
        {
            var sibling = index ^ 1;
            var hash = sibling < _levels[level].Length ? _levels[level][sibling] : _levels[level][index];
            proof.Add((hash, sibling < index));
            index /= 2;
        }
        return proof;
    }

    /// <summary>Checks a block against a trusted root using only the proof, without the rest of the data.</summary>
    public static bool Verify(string block, IReadOnlyList<(byte[] Hash, bool IsLeft)> proof, string root)
    {
        var hash = Hash([0x00], Encoding.UTF8.GetBytes(block));
        foreach (var (sibling, isLeft) in proof) hash = isLeft ? Hash([0x01], sibling, hash) : Hash([0x01], hash, sibling);
        return Convert.ToHexString(hash) == root;
    }

    /// <summary>Different prefixes for leaves and inner nodes stop a leaf from being passed off as an inner node.</summary>
    private static byte[] Hash(params byte[][] parts) => SHA256.HashData(parts.SelectMany(p => p).ToArray());
}
