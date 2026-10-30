using System.Text;

namespace CSharpExercises.Collections;

/// <summary>
/// A rope stores a long string as a binary tree of short pieces. Inserting or deleting in the middle of a huge text
/// copies only O(log n) nodes instead of the whole string, and every edit returns a new rope that shares most of its
/// structure with the old one, so undo history is cheap. Text editors use ropes or similar piece tables.
/// </summary>
public abstract class Rope
{
    private const int LeafLimit = 64;

    public abstract int Length { get; }
    public abstract int Depth { get; }
    public abstract char this[int index] { get; }

    public static Rope Empty { get; } = new Leaf("");

    public static Rope From(string text) =>
        text.Length <= LeafLimit ? new Leaf(text) : Join(From(text[..(text.Length / 2)]), From(text[(text.Length / 2)..]));

    public static Rope Join(Rope left, Rope right) =>
        left.Length == 0 ? right
        : right.Length == 0 ? left
        : left is Leaf a && right is Leaf b && a.Length + b.Length <= LeafLimit ? new Leaf(a.Text + b.Text)
        : new Branch(left, right);

    public abstract (Rope Left, Rope Right) Split(int index);

    public Rope Insert(int index, string text)
    {
        var (left, right) = Split(index);
        return Join(Join(left, From(text)), right);
    }

    public Rope Delete(int start, int count)
    {
        var (left, rest) = Split(start);
        var (_, right) = rest.Split(count);
        return Join(left, right);
    }

    public string Substring(int start, int count) => Split(start).Right.Split(count).Left.ToString();

    /// <summary>Rebuilds a balanced tree after many edits have made it lopsided.</summary>
    public Rope Rebalance() => From(ToString());

    public override string ToString()
    {
        var sb = new StringBuilder(Length);
        AppendTo(sb);
        return sb.ToString();
    }

    protected abstract void AppendTo(StringBuilder sb);

    private sealed class Leaf(string text) : Rope
    {
        public string Text { get; } = text;
        public override int Length => Text.Length;
        public override int Depth => 0;
        public override char this[int index] => Text[index];

        public override (Rope Left, Rope Right) Split(int index)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)index, (uint)Length, nameof(index));
            return (new Leaf(Text[..index]), new Leaf(Text[index..]));
        }

        protected override void AppendTo(StringBuilder sb) => sb.Append(Text);
    }

    private sealed class Branch(Rope left, Rope right) : Rope
    {
        public override int Length { get; } = left.Length + right.Length;
        public override int Depth { get; } = 1 + Math.Max(left.Depth, right.Depth);

        public override char this[int index] => index < left.Length ? left[index] : right[index - left.Length];

        public override (Rope Left, Rope Right) Split(int index)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)index, (uint)Length, nameof(index));
            if (index <= left.Length)
            {
                var (a, b) = left.Split(index);
                return (a, Join(b, right)); // the untouched right subtree is shared, not copied
            }
            var (c, d) = right.Split(index - left.Length);
            return (Join(left, c), d);
        }

        protected override void AppendTo(StringBuilder sb)
        {
            left.AppendTo(sb);
            right.AppendTo(sb);
        }
    }
}
