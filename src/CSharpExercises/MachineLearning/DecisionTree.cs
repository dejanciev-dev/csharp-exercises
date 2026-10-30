using System.Text;

namespace CSharpExercises.MachineLearning;

/// <summary>
/// A decision tree classifier (CART): at each node, try every feature and threshold and keep the split that most
/// reduces Gini impurity (the chance two random samples from a node have different labels). Recurse until a node is
/// pure, too small or too deep; limiting depth is the main defence against memorising the training data. Unlike most
/// models, the result can be printed and read as rules.
/// </summary>
public sealed class DecisionTree
{
    private abstract record Node;
    private sealed record Leaf(string Label, int Samples) : Node;
    private sealed record Split(int Feature, double Threshold, Node Left, Node Right) : Node;

    private readonly Node _root;
    private readonly string[] _featureNames;

    public DecisionTree(IReadOnlyList<(double[] Features, string Label)> data, string[] featureNames, int maxDepth = 5, int minSamples = 2)
    {
        _featureNames = featureNames;
        _root = Build([.. data], 0, maxDepth, minSamples);
    }

    public string Predict(double[] features)
    {
        var node = _root;
        while (node is Split s) node = features[s.Feature] <= s.Threshold ? s.Left : s.Right;
        return ((Leaf)node).Label;
    }

    public int Depth => DepthOf(_root);

    public static double Gini(IEnumerable<string> labels)
    {
        var list = labels.ToList();
        if (list.Count == 0) return 0;
        return 1 - list.GroupBy(l => l).Sum(g => Math.Pow((double)g.Count() / list.Count, 2));
    }

    /// <summary>The tree as indented if/else rules.</summary>
    public string Describe()
    {
        var sb = new StringBuilder();
        void Write(Node node, string indent)
        {
            switch (node)
            {
                case Leaf leaf: sb.Append($"{indent}→ {leaf.Label}\n"); break;
                case Split s:
                    sb.Append($"{indent}if {_featureNames[s.Feature]} <= {s.Threshold:0.##}\n");
                    Write(s.Left, indent + "  ");
                    sb.Append($"{indent}else\n");
                    Write(s.Right, indent + "  ");
                    break;
            }
        }
        Write(_root, "");
        return sb.ToString();
    }

    private static Node Build(List<(double[] Features, string Label)> data, int depth, int maxDepth, int minSamples)
    {
        var majority = data.GroupBy(d => d.Label).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).First().Key;
        if (depth >= maxDepth || data.Count < minSamples || data.All(d => d.Label == data[0].Label)) return new Leaf(majority, data.Count);

        var parentGini = Gini(data.Select(d => d.Label));
        (int Feature, double Threshold, double Gain)? best = null;

        for (var f = 0; f < data[0].Features.Length; f++)
        {
            var values = data.Select(d => d.Features[f]).Distinct().Order().ToArray();
            for (var i = 0; i + 1 < values.Length; i++)
            {
                var threshold = (values[i] + values[i + 1]) / 2; // split between neighbouring values
                var left = data.Where(d => d.Features[f] <= threshold).Select(d => d.Label).ToList();
                var right = data.Where(d => d.Features[f] > threshold).Select(d => d.Label).ToList();
                var weighted = (left.Count * Gini(left) + right.Count * Gini(right)) / data.Count;
                var gain = parentGini - weighted;
                if (best is null || gain > best.Value.Gain) best = (f, threshold, gain);
            }
        }

        if (best is not { Gain: > 0 } b) return new Leaf(majority, data.Count);
        return new Split(b.Feature, b.Threshold,
            Build(data.Where(d => d.Features[b.Feature] <= b.Threshold).ToList(), depth + 1, maxDepth, minSamples),
            Build(data.Where(d => d.Features[b.Feature] > b.Threshold).ToList(), depth + 1, maxDepth, minSamples));
    }

    private static int DepthOf(Node node) => node is Split s ? 1 + Math.Max(DepthOf(s.Left), DepthOf(s.Right)) : 0;
}
