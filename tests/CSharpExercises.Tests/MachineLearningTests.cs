using CSharpExercises.MachineLearning;

namespace CSharpExercises.Tests;

public class MachineLearningTests
{
    private static List<double[]> Blobs(Random random, params (double X, double Y)[] centers) =>
        centers.SelectMany(c => Enumerable.Range(0, 50).Select(_ => new[] { c.X + Gaussian(random), c.Y + Gaussian(random) })).ToList();

    private static double Gaussian(Random random) =>
        Math.Sqrt(-2 * Math.Log(1 - random.NextDouble())) * Math.Cos(2 * Math.PI * random.NextDouble());

    [Fact]
    public void KMeans_FindsWellSeparatedClusters()
    {
        var random = new Random(1);
        var points = Blobs(random, (0, 0), (10, 10), (0, 10));

        var result = KMeans.Fit(points, 3, random);

        // Each blob of 50 ends up in a single cluster.
        for (var blob = 0; blob < 3; blob++) Assert.Single(result.Assignments.Skip(blob * 50).Take(50).Distinct());
        Assert.Equal(3, result.Assignments.Distinct().Count());
        Assert.Contains(result.Centers, c => Math.Abs(c[0] - 10) < 0.5 && Math.Abs(c[1] - 10) < 0.5);
    }

    [Fact]
    public void KMeans_InertiaElbow()
    {
        var random = new Random(2);
        var points = Blobs(random, (0, 0), (10, 10), (0, 10));

        var inertia = Enumerable.Range(1, 5).Select(k => KMeans.Fit(points, k, new Random(3)).Inertia).ToArray();

        Assert.True(inertia[1] < inertia[0] && inertia[2] < inertia[1]);
        Assert.True(inertia[1] - inertia[2] > 5 * (inertia[2] - inertia[3])); // the drop flattens after k = 3
    }

    [Fact]
    public void Knn_ClassifiesWithScaledFeatures()
    {
        // Height in cm and weight in tonnes: without scaling, height would decide everything.
        (double[], string)[] training =
        [
            ([150, 0.050], "light"), ([160, 0.055], "light"), ([170, 0.060], "light"),
            ([155, 0.120], "heavy"), ([165, 0.130], "heavy"), ([175, 0.125], "heavy"),
        ];
        var knn = new NearestNeighbors<string>(training, k: 3);

        Assert.Equal("heavy", knn.Predict([150, 0.128]));
        Assert.Equal("light", knn.Predict([175, 0.052]));
        Assert.Equal(1.0, knn.Accuracy(training));
    }

    [Fact]
    public void NaiveBayes_FiltersSpam()
    {
        var nb = new NaiveBayes();
        nb.Train("spam", "win money now, free prize, click now");
        nb.Train("spam", "free money, claim your prize today");
        nb.Train("spam", "cheap pills, win big, free offer");
        nb.Train("ham", "meeting moved to tuesday, see agenda");
        nb.Train("ham", "can you review my pull request today");
        nb.Train("ham", "lunch tomorrow? the usual place");

        Assert.Equal("spam", nb.Classify("claim your free prize"));
        Assert.Equal("ham", nb.Classify("review the agenda for tuesday"));
        Assert.True(nb.Probabilities("free free free money")["spam"] > 0.95);
        Assert.Equal(1, nb.Probabilities("completely unknown words").Values.Sum(), 9);
        Assert.Contains("free", nb.MostIndicative("spam", 3));
    }

    [Fact]
    public void Logistic_LearnsABoundary()
    {
        var random = new Random(4);
        var features = new List<double[]>();
        var labels = new List<bool>();
        for (var i = 0; i < 200; i++)
        {
            var (x, y) = (random.NextDouble() * 10, random.NextDouble() * 10);
            features.Add([x, y]);
            labels.Add(x + y > 10); // the true boundary
        }

        var model = new LogisticRegression();
        model.Fit(features, labels, learningRate: 0.1, epochs: 3000);

        Assert.True(model.LossHistory[^1] < model.LossHistory[0] / 3);
        Assert.True(model.Predict([9, 9]));
        Assert.False(model.Predict([1, 1]));
        Assert.InRange(model.Probability([5, 5]), 0.3, 0.7); // right on the boundary
        var (precision, recall) = model.Evaluate(features, labels);
        Assert.True(precision > 0.9 && recall > 0.9);
    }

    [Fact]
    public void DecisionTree_LearnsReadableRules()
    {
        // Play tennis? Features: temperature, humidity, wind.
        (double[], string)[] days =
        [
            ([30, 85, 0], "no"), ([27, 90, 1], "no"), ([28, 78, 0], "yes"), ([21, 96, 0], "yes"), ([20, 80, 0], "yes"),
            ([18, 70, 1], "no"), ([17, 65, 1], "yes"), ([22, 95, 0], "no"), ([21, 70, 0], "yes"), ([24, 80, 0], "yes"),
            ([24, 70, 1], "yes"), ([22, 90, 1], "no"), ([27, 75, 0], "yes"), ([21, 91, 1], "no"),
        ];
        var tree = new DecisionTree(days, ["temp", "humidity", "wind"], maxDepth: 4);

        Assert.True(days.Count(d => tree.Predict(d.Item1) == d.Item2) >= 12);
        Assert.InRange(tree.Depth, 1, 4);
        Assert.StartsWith("if ", tree.Describe());
        Assert.Equal(0.5, DecisionTree.Gini(["a", "b"]));
        Assert.Equal(0, DecisionTree.Gini(["a", "a"]));
    }

    [Fact]
    public void Perceptron_LearnsAndButNotXor()
    {
        (double[], bool)[] and = [([0, 0], false), ([0, 1], false), ([1, 0], false), ([1, 1], true)];
        (double[], bool)[] xor = [([0, 0], false), ([0, 1], true), ([1, 0], true), ([1, 1], false)];

        Assert.NotNull(new Perceptron(2).Train(and));
        Assert.Null(new Perceptron(2).Train(xor));
    }

    [Fact]
    public void HiddenLayer_LearnsXor()
    {
        (double[], double)[] xor = [([0, 0], 0), ([0, 1], 1), ([1, 0], 1), ([1, 1], 0)];
        var network = new TinyNeuralNetwork(2, 4, new Random(5));

        network.Train(xor, epochs: 10_000, rate: 0.5);

        Assert.All(xor, sample => Assert.Equal(sample.Item2, Math.Round(network.Forward(sample.Item1))));
    }
}
