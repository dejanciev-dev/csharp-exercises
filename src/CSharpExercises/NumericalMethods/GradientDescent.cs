namespace CSharpExercises.NumericalMethods;

/// <summary>
/// Minimising a function by repeatedly stepping downhill along the negative gradient: the engine of machine learning.
/// The learning rate matters: too small crawls, too large overshoots and diverges. Momentum keeps a running velocity so
/// the search rolls through narrow valleys instead of zig-zagging across them. Gradients can be computed numerically
/// with central differences when no formula is available.
/// </summary>
public static class GradientDescent
{
    public sealed record Result(double[] Minimum, double Value, int Iterations, bool Converged);

    public static Result Minimize(
        Func<double[], double> f, Func<double[], double[]>? gradient, double[] start,
        double learningRate = 0.01, double momentum = 0, double tolerance = 1e-10, int maxIterations = 100_000)
    {
        gradient ??= x => NumericalGradient(f, x);
        var x = start.ToArray();
        var velocity = new double[x.Length];

        for (var i = 1; i <= maxIterations; i++)
        {
            var g = gradient(x);
            var stepSize = 0.0;
            for (var k = 0; k < x.Length; k++)
            {
                velocity[k] = momentum * velocity[k] - learningRate * g[k];
                x[k] += velocity[k];
                stepSize += velocity[k] * velocity[k];
            }
            if (x.Any(v => !double.IsFinite(v))) return new Result(x, double.NaN, i, false); // diverged
            if (Math.Sqrt(stepSize) < tolerance) return new Result(x, f(x), i, true);
        }
        return new Result(x, f(x), maxIterations, false);
    }

    /// <summary>
    /// ∂f/∂xₖ ≈ (f(x + h·eₖ) - f(x - h·eₖ)) / 2h. The step scales with |xₖ|: a fixed h vanishes next to a large x
    /// (x + h == x in floating point), which would report a zero gradient and fake convergence.
    /// </summary>
    public static double[] NumericalGradient(Func<double[], double> f, double[] x, double relativeStep = 1e-6)
    {
        var gradient = new double[x.Length];
        for (var k = 0; k < x.Length; k++)
        {
            var h = relativeStep * Math.Max(1, Math.Abs(x[k]));
            var (plus, minus) = (x.ToArray(), x.ToArray());
            plus[k] += h;
            minus[k] -= h;
            gradient[k] = (f(plus) - f(minus)) / (2 * h);
        }
        return gradient;
    }

    /// <summary>Fits y = w·x + b by minimising the mean squared error, the "hello world" of training a model.</summary>
    public static (double Weight, double Bias) FitLine(IReadOnlyList<(double X, double Y)> data, double learningRate = 0.01)
    {
        double Loss(double[] p) => data.Average(d => Math.Pow(p[0] * d.X + p[1] - d.Y, 2));
        double[] Gradient(double[] p) =>
        [
            data.Average(d => 2 * (p[0] * d.X + p[1] - d.Y) * d.X),
            data.Average(d => 2 * (p[0] * d.X + p[1] - d.Y)),
        ];
        var result = Minimize(Loss, Gradient, [0, 0], learningRate, momentum: 0.9);
        return (result.Minimum[0], result.Minimum[1]);
    }

    /// <summary>The Rosenbrock "banana" function: a long curved valley that makes plain gradient descent crawl.</summary>
    public static double Rosenbrock(double[] p) => Math.Pow(1 - p[0], 2) + 100 * Math.Pow(p[1] - p[0] * p[0], 2);
}
