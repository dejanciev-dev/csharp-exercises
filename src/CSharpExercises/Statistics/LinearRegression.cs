namespace CSharpExercises.Statistics;

/// <summary>
/// Ordinary least squares: the straight line y = intercept + slope · x that minimises the sum of squared vertical
/// errors. R² says how much of the variation in y the line explains (1 = perfectly, 0 = no better than the mean).
/// </summary>
public sealed record LinearRegression(double Slope, double Intercept, double RSquared)
{
    public static LinearRegression Fit(IReadOnlyList<(double X, double Y)> points)
    {
        if (points.Count < 2) throw new ArgumentException("Need at least two points.", nameof(points));

        var meanX = points.Average(p => p.X);
        var meanY = points.Average(p => p.Y);

        // Centre the data first: the same cancellation problem as with variance otherwise.
        double sxy = 0, sxx = 0, syy = 0;
        foreach (var (x, y) in points)
        {
            sxy += (x - meanX) * (y - meanY);
            sxx += (x - meanX) * (x - meanX);
            syy += (y - meanY) * (y - meanY);
        }
        if (sxx == 0) throw new ArgumentException("All x values are equal: the slope is undefined.", nameof(points));

        var slope = sxy / sxx;
        var rSquared = syy == 0 ? 1 : sxy * sxy / (sxx * syy);
        return new LinearRegression(slope, meanY - slope * meanX, rSquared);
    }

    public double Predict(double x) => Intercept + Slope * x;

    /// <summary>Pearson correlation: the sign of the slope with the magnitude √R².</summary>
    public static double Correlation(IReadOnlyList<(double X, double Y)> points)
    {
        var fit = Fit(points);
        return Math.Sign(fit.Slope) * Math.Sqrt(fit.RSquared);
    }
}
