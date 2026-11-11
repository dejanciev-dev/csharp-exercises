namespace CSharpExercises.Astronomy;

/// <summary>
/// Where a planet is on its elliptical orbit. Time gives the mean anomaly M (an angle growing steadily), but the
/// planet speeds up near the sun, so we need the eccentric anomaly E from Kepler's equation M = E - e sin E. It has no
/// closed form; Newton's method solves it in a few steps (starting from π for very eccentric orbits, where starting
/// at M can overshoot). Kepler's third law gives the period: T² = a³ with T in years and a in astronomical units.
/// </summary>
public static class Kepler
{
    public static double PeriodYears(double semiMajorAxisAu) => Math.Pow(semiMajorAxisAu, 1.5);

    public static (double E, int Iterations) EccentricAnomaly(double meanAnomaly, double eccentricity, double tolerance = 1e-12)
    {
        if (eccentricity is < 0 or >= 1) throw new ArgumentOutOfRangeException(nameof(eccentricity), "Ellipses only: 0 ≤ e < 1.");

        var m = meanAnomaly % (2 * Math.PI);
        if (m < 0) m += 2 * Math.PI;
        var e = eccentricity < 0.8 ? m : Math.PI;
        for (var i = 1; i <= 50; i++)
        {
            var step = (e - eccentricity * Math.Sin(e) - m) / (1 - eccentricity * Math.Cos(e));
            e -= step;
            if (Math.Abs(step) < tolerance) return (e, i);
        }
        throw new InvalidOperationException("Kepler's equation did not converge.");
    }

    public static double TrueAnomaly(double eccentricAnomaly, double eccentricity) =>
        2 * Math.Atan2(Math.Sqrt(1 + eccentricity) * Math.Sin(eccentricAnomaly / 2),
                       Math.Sqrt(1 - eccentricity) * Math.Cos(eccentricAnomaly / 2));

    /// <summary>Position in the orbital plane (AU, sun at the origin, perihelion on +x) some years after perihelion.</summary>
    public static (double X, double Y, double Distance) Position(double semiMajorAxisAu, double eccentricity, double yearsSincePerihelion)
    {
        var meanAnomaly = 2 * Math.PI * yearsSincePerihelion / PeriodYears(semiMajorAxisAu);
        var (e, _) = EccentricAnomaly(meanAnomaly, eccentricity);
        var x = semiMajorAxisAu * (Math.Cos(e) - eccentricity);
        var y = semiMajorAxisAu * Math.Sqrt(1 - eccentricity * eccentricity) * Math.Sin(e);
        return (x, y, semiMajorAxisAu * (1 - eccentricity * Math.Cos(e)));
    }
}
