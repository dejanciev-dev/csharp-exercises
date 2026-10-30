using CSharpExercises.NumericalMethods;

namespace CSharpExercises.Tests;

public class NumericalMethodsTests
{
    private static double F(double x) => x * x * x - 2 * x - 5; // Newton's own example, root ≈ 2.0945514815

    [Fact]
    public void Roots_AllMethodsAgreeAtDifferentSpeeds()
    {
        const double Root = 2.0945514815423265;

        var bisection = RootFinding.Bisection(F, 2, 3);
        var newton = RootFinding.Newton(F, x => 3 * x * x - 2, 2);
        var secant = RootFinding.Secant(F, 2, 3);

        Assert.Equal(Root, bisection.Value, 10);
        Assert.Equal(Root, newton.Value, 12);
        Assert.Equal(Root, secant.Value, 12);
        Assert.True(newton.Iterations < secant.Iterations && secant.Iterations < bisection.Iterations);
        Assert.Throws<ArgumentException>(() => RootFinding.Bisection(F, 3, 4));
    }

    [Fact]
    public void Roots_NewtonSqrtAndFailure()
    {
        Assert.Equal(Math.Sqrt(2), RootFinding.Sqrt(2), 14);
        Assert.Equal(1e-3, RootFinding.Sqrt(1e-6), 14);
        Assert.Throws<InvalidOperationException>(() => RootFinding.Newton(x => x * x + 1, x => 2 * x, 0)); // no real root, flat start
    }

    [Fact]
    public void Integration_SimpsonBeatsTrapezoid()
    {
        var trapezoid = Integration.Trapezoid(Math.Sin, 0, Math.PI, 10);
        var simpson = Integration.Simpson(Math.Sin, 0, Math.PI, 10);

        Assert.InRange(Math.Abs(trapezoid - 2), 1e-3, 1e-1);
        Assert.InRange(Math.Abs(simpson - 2), 0, 1e-3);
        Assert.Equal(1.0 / 3, Integration.Simpson(x => x * x, 0, 1, 2), 15); // exact for cubics
        Assert.Throws<ArgumentException>(() => Integration.Simpson(Math.Sin, 0, 1, 3));
    }

    [Fact]
    public void Integration_AdaptiveConcentratesOnHardRegions()
    {
        // √x has an infinite slope at 0; the exact integral over [0, 1] is 2/3.
        var (value, evaluations) = Integration.AdaptiveSimpson(Math.Sqrt, 0, 1, 1e-10);

        Assert.Equal(2.0 / 3, value, 9);
        Assert.True(evaluations < 2000);
        Assert.Equal(2, Integration.MonteCarlo(Math.Sin, 0, Math.PI, 200_000, new Random(1)), 1);
    }

    [Fact]
    public void Interpolation_LinearLagrangeAndSpline()
    {
        (double, double)[] points = [(0, 0), (1, 1), (2, 4), (3, 9)];

        Assert.Equal(2.5, Interpolation.Linear(points, 1.5));
        Assert.Equal(9, Interpolation.Linear(points, 10)); // clamped
        Assert.Equal(2.25, Interpolation.Lagrange(points, 1.5), 12); // exact: the data is a parabola

        var spline = Interpolation.CubicSpline(points);
        Assert.All(points, p => Assert.Equal(p.Item2, spline(p.Item1), 12));
        Assert.InRange(spline(1.5), 2.1, 2.4);
    }

    [Fact]
    public void Interpolation_RungePhenomenon()
    {
        static double Runge(double x) => 1 / (1 + 25 * x * x);
        var points = Enumerable.Range(0, 15).Select(i => -1 + 2.0 * i / 14).Select(x => (x, Runge(x))).ToArray();
        var spline = Interpolation.CubicSpline(points);

        const double X = 0.96;
        var polynomialError = Math.Abs(Interpolation.Lagrange(points, X) - Runge(X));
        var splineError = Math.Abs(spline(X) - Runge(X));

        Assert.True(polynomialError > 1);    // wild oscillation near the edges
        Assert.True(splineError < 0.01);
    }

    [Fact]
    public void GradientDescent_FindsMinimaAndFitsALine()
    {
        var bowl = GradientDescent.Minimize(p => Math.Pow(p[0] - 3, 2) + Math.Pow(p[1] + 1, 2), null, [0, 0], learningRate: 0.1);
        Assert.True(bowl.Converged);
        Assert.Equal(3, bowl.Minimum[0], 6);
        Assert.Equal(-1, bowl.Minimum[1], 6);

        var (w, b) = GradientDescent.FitLine([(1, 3), (2, 5), (3, 7), (4, 9)]);
        Assert.Equal(2, w, 4);
        Assert.Equal(1, b, 4);

        var diverged = GradientDescent.Minimize(p => p[0] * p[0], null, [1], learningRate: 1.5);
        Assert.False(diverged.Converged); // a step too large overshoots further every time
    }

    [Fact]
    public void GradientDescent_MomentumHelpsInTheBananaValley()
    {
        var plain = GradientDescent.Minimize(GradientDescent.Rosenbrock, null, [-1.2, 1], learningRate: 0.001, maxIterations: 20_000);
        var withMomentum = GradientDescent.Minimize(GradientDescent.Rosenbrock, null, [-1.2, 1], learningRate: 0.001, momentum: 0.9, maxIterations: 20_000);

        Assert.True(withMomentum.Value < plain.Value);
        Assert.Equal(1, withMomentum.Minimum[0], 2);
    }

    [Fact]
    public void Ode_RungeKuttaIsAccurateEulerDrifts()
    {
        // dy/dt = -y, y(0) = 1, so y(1) = 1/e.
        OdeSolvers.Derivative decay = (_, y) => [-y[0]];
        var euler = OdeSolvers.Euler(decay, [1], 0, 1, 10)[0];
        var rk4 = OdeSolvers.RungeKutta4(decay, [1], 0, 1, 10)[0];

        Assert.InRange(Math.Abs(euler - 1 / Math.E), 0.01, 0.03);
        Assert.InRange(Math.Abs(rk4 - 1 / Math.E), 0, 1e-6);
    }

    [Fact]
    public void Ode_SymplecticEulerConservesEnergy()
    {
        const double Omega = 2 * Math.PI; // one oscillation per second
        const int Steps = 10_000;
        const double Dt = 0.001;          // 10 seconds
        var start = OdeSolvers.SpringEnergy(1, 0, Omega);

        var explicitEnd = OdeSolvers.Euler(OdeSolvers.Spring(Omega), [1, 0], 0, Steps * Dt, Steps);
        var (x, v) = OdeSolvers.SemiImplicitEulerSpring(1, 0, Omega, Dt, Steps);

        Assert.True(OdeSolvers.SpringEnergy(explicitEnd[0], explicitEnd[1], Omega) > 1.3 * start); // gains energy
        Assert.InRange(OdeSolvers.SpringEnergy(x, v, Omega), 0.97 * start, 1.03 * start);
    }

    [Fact]
    public void Annealing_ImprovesOnGreedyTours()
    {
        var random = new Random(1);
        var cities = Enumerable.Range(0, 100).Select(_ => (random.NextDouble() * 100, random.NextDouble() * 100)).ToArray();

        var greedy = SimulatedAnnealing.TourLength(cities, SimulatedAnnealing.NearestNeighbourTour(cities));
        var (tour, length) = SimulatedAnnealing.SolveTsp(cities, new Random(2));

        Assert.Equal(100, tour.Distinct().Count());
        Assert.Equal(SimulatedAnnealing.TourLength(cities, tour), length, 6);
        Assert.True(length < 0.95 * greedy);
    }
}
