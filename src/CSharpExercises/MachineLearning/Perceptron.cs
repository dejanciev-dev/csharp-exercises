namespace CSharpExercises.MachineLearning;

/// <summary>
/// The perceptron (1958), the ancestor of neural networks: a weighted sum and a threshold, nudged toward each example it
/// gets wrong. It's guaranteed to converge when the classes can be separated by a line, and never converges when they
/// can't, famously XOR. A tiny two-layer network with a hidden layer, trained by backpropagation, learns XOR easily,
/// which is the whole reason hidden layers exist.
/// </summary>
public sealed class Perceptron(int inputs)
{
    private readonly double[] _weights = new double[inputs];
    private double _bias;

    /// <returns>The number of epochs until no mistakes, or null if it never got there.</returns>
    public int? Train(IReadOnlyList<(double[] X, bool Y)> data, int maxEpochs = 1000, double rate = 0.1)
    {
        for (var epoch = 1; epoch <= maxEpochs; epoch++)
        {
            var mistakes = 0;
            foreach (var (x, y) in data)
            {
                if (Predict(x) == y) continue;
                mistakes++;
                var direction = y ? 1 : -1;
                for (var i = 0; i < x.Length; i++) _weights[i] += rate * direction * x[i];
                _bias += rate * direction;
            }
            if (mistakes == 0) return epoch;
        }
        return null;
    }

    public bool Predict(double[] x) => _weights.Zip(x, (w, v) => w * v).Sum() + _bias > 0;
}

/// <summary>A 2-input, n-hidden, 1-output network with sigmoid units, trained by plain backpropagation.</summary>
public sealed class TinyNeuralNetwork
{
    private readonly double[,] _w1;
    private readonly double[] _b1;
    private readonly double[] _w2;
    private double _b2;

    public TinyNeuralNetwork(int inputs, int hidden, Random random)
    {
        _w1 = new double[hidden, inputs];
        _b1 = new double[hidden];
        _w2 = new double[hidden];
        for (var h = 0; h < hidden; h++)
        {
            for (var i = 0; i < inputs; i++) _w1[h, i] = random.NextDouble() * 2 - 1;
            _w2[h] = random.NextDouble() * 2 - 1;
        }
    }

    public double Forward(double[] x) => Run(x).Output;

    public void Train(IReadOnlyList<(double[] X, double Y)> data, int epochs, double rate)
    {
        for (var epoch = 0; epoch < epochs; epoch++)
            foreach (var (x, y) in data)
            {
                var (hidden, output) = Run(x);
                var outputDelta = (output - y) * output * (1 - output); // derivative of the squared error through the sigmoid
                for (var h = 0; h < _w2.Length; h++)
                {
                    var hiddenDelta = outputDelta * _w2[h] * hidden[h] * (1 - hidden[h]);
                    _w2[h] -= rate * outputDelta * hidden[h];
                    for (var i = 0; i < x.Length; i++) _w1[h, i] -= rate * hiddenDelta * x[i];
                    _b1[h] -= rate * hiddenDelta;
                }
                _b2 -= rate * outputDelta;
            }
    }

    private (double[] Hidden, double Output) Run(double[] x)
    {
        var hidden = new double[_w2.Length];
        for (var h = 0; h < hidden.Length; h++)
        {
            var sum = _b1[h];
            for (var i = 0; i < x.Length; i++) sum += _w1[h, i] * x[i];
            hidden[h] = LogisticRegression.Sigmoid(sum);
        }
        return (hidden, LogisticRegression.Sigmoid(hidden.Select((v, h) => v * _w2[h]).Sum() + _b2));
    }
}
