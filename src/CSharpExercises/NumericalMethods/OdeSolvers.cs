namespace CSharpExercises.NumericalMethods;

/// <summary>
/// Solving differential equations dy/dt = f(t, y) step by step, as physics engines and simulations do. Euler's method
/// follows the slope at the start of each step (error ∝ h) and steadily gains energy on an orbit or spring. Runge–Kutta 4
/// samples the slope four times per step (error ∝ h⁴). Semi-implicit (symplectic) Euler costs the same as Euler but
/// conserves energy far better, which is why game physics uses it.
/// </summary>
public static class OdeSolvers
{
    public delegate double[] Derivative(double t, double[] y);

    public static double[] Euler(Derivative f, double[] y0, double t0, double t1, int steps)
    {
        var h = (t1 - t0) / steps;
        var y = y0.ToArray();
        for (var i = 0; i < steps; i++) y = Add(y, f(t0 + i * h, y), h);
        return y;
    }

    public static double[] RungeKutta4(Derivative f, double[] y0, double t0, double t1, int steps)
    {
        var h = (t1 - t0) / steps;
        var y = y0.ToArray();
        for (var i = 0; i < steps; i++)
        {
            var t = t0 + i * h;
            var k1 = f(t, y);
            var k2 = f(t + h / 2, Add(y, k1, h / 2));
            var k3 = f(t + h / 2, Add(y, k2, h / 2));
            var k4 = f(t + h, Add(y, k3, h));
            for (var j = 0; j < y.Length; j++) y[j] += h / 6 * (k1[j] + 2 * k2[j] + 2 * k3[j] + k4[j]);
        }
        return y;
    }

    /// <summary>For a spring x'' = -ω²x: update the velocity first, then move with the new velocity.</summary>
    public static (double Position, double Velocity) SemiImplicitEulerSpring(double x, double v, double omega, double dt, int steps)
    {
        for (var i = 0; i < steps; i++)
        {
            v -= omega * omega * x * dt;
            x += v * dt;
        }
        return (x, v);
    }

    /// <summary>Energy of a unit-mass spring, which should stay constant: ½v² + ½ω²x².</summary>
    public static double SpringEnergy(double x, double v, double omega) => 0.5 * v * v + 0.5 * omega * omega * x * x;

    public static Derivative Spring(double omega) => (_, y) => [y[1], -omega * omega * y[0]];

    private static double[] Add(double[] y, double[] dy, double scale) => y.Select((v, i) => v + scale * dy[i]).ToArray();
}
