namespace CSharpExercises.MachineLearning;

/// <summary>
/// Logistic regression: a linear score squashed by the sigmoid into a probability, trained by gradient descent on the
/// cross-entropy loss (whose gradient is simply (prediction - label) · features). L2 regularisation keeps weights small
/// so the model doesn't become overconfident on separable data. The decision boundary is where the score is 0.
/// </summary>
public sealed class LogisticRegression
{
    public double[] Weights { get; private set; } = [];
    public double Bias { get; private set; }

    public IReadOnlyList<double> LossHistory => _loss;
    private readonly List<double> _loss = [];

    public void Fit(IReadOnlyList<double[]> features, IReadOnlyList<bool> labels, double learningRate = 0.1, int epochs = 2000, double l2 = 0.001)
    {
        var dims = features[0].Length;
        Weights = new double[dims];
        Bias = 0;
        var n = features.Count;

        for (var epoch = 0; epoch < epochs; epoch++)
        {
            var gradW = new double[dims];
            var gradB = 0.0;
            var loss = 0.0;
            for (var i = 0; i < n; i++)
            {
                var p = Probability(features[i]);
                var y = labels[i] ? 1.0 : 0.0;
                var error = p - y;
                for (var d = 0; d < dims; d++) gradW[d] += error * features[i][d] / n;
                gradB += error / n;
                loss -= (y * Math.Log(p + 1e-12) + (1 - y) * Math.Log(1 - p + 1e-12)) / n;
            }
            for (var d = 0; d < dims; d++) Weights[d] -= learningRate * (gradW[d] + l2 * Weights[d]);
            Bias -= learningRate * gradB;
            _loss.Add(loss);
        }
    }

    public double Probability(double[] x) => Sigmoid(Weights.Zip(x, (w, v) => w * v).Sum() + Bias);

    public bool Predict(double[] x, double threshold = 0.5) => Probability(x) >= threshold;

    public static double Sigmoid(double z) => 1 / (1 + Math.Exp(-z));

    /// <summary>Precision (of those flagged, how many were right) and recall (of the real positives, how many were found).</summary>
    public (double Precision, double Recall) Evaluate(IReadOnlyList<double[]> features, IReadOnlyList<bool> labels, double threshold = 0.5)
    {
        int tp = 0, fp = 0, fn = 0;
        for (var i = 0; i < features.Count; i++)
        {
            var predicted = Predict(features[i], threshold);
            if (predicted && labels[i]) tp++;
            else if (predicted) fp++;
            else if (labels[i]) fn++;
        }
        return (tp + fp == 0 ? 1 : (double)tp / (tp + fp), tp + fn == 0 ? 1 : (double)tp / (tp + fn));
    }
}
