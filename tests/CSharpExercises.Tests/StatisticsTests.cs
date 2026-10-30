using CSharpExercises.Statistics;

namespace CSharpExercises.Tests;

public class StatisticsTests
{
    [Fact]
    public void RunningStats_MatchesTheTextbookValues()
    {
        var stats = new RunningStats();
        foreach (var v in new double[] { 2, 4, 4, 4, 5, 5, 7, 9 }) stats.Add(v);

        Assert.Equal(5, stats.Mean);
        Assert.Equal(4, stats.Variance);
        Assert.Equal(2, stats.StandardDeviation);
        Assert.Equal(32.0 / 7, stats.SampleVariance, 12);
        Assert.Equal((2.0, 9.0), (stats.Min, stats.Max));
    }

    [Fact]
    public void RunningStats_StaysAccurateWhereTheNaiveFormulaFails()
    {
        double[] values = [1e9 + 4, 1e9 + 7, 1e9 + 13, 1e9 + 16]; // variance 22.5
        var stats = new RunningStats();
        foreach (var v in values) stats.Add(v);

        Assert.Equal(22.5, stats.Variance, 6);
        Assert.NotEqual(22.5, RunningStats.NaiveVariance(values), 1);
    }

    [Fact]
    public void RunningStats_MergesPartialResults()
    {
        var random = new Random(1);
        var values = Enumerable.Range(0, 1000).Select(_ => random.NextDouble() * 100).ToArray();
        RunningStats all = new(), left = new(), right = new();
        foreach (var v in values) all.Add(v);
        foreach (var v in values[..300]) left.Add(v);
        foreach (var v in values[300..]) right.Add(v);

        var merged = RunningStats.Merge(left, right);

        Assert.Equal(all.Count, merged.Count);
        Assert.Equal(all.Mean, merged.Mean, 9);
        Assert.Equal(all.Variance, merged.Variance, 9);
        Assert.Equal(all.Max, merged.Max);
    }

    [Fact]
    public void StreamingMedian_TracksTheMiddle()
    {
        var median = new StreamingMedian();
        var expected = new[] { 5, 3.5, 3, 3.5, 3 };
        double[] input = [5, 2, 3, 4, 1];

        for (var i = 0; i < input.Length; i++)
        {
            median.Add(input[i]);
            Assert.Equal(expected[i], median.Median);
        }
        Assert.Throws<InvalidOperationException>(() => new StreamingMedian().Median);
    }

    [Fact]
    public void Percentiles_NearestRankAndInterpolated()
    {
        double[] values = [15, 20, 35, 40, 50];

        Assert.Equal(20, Percentiles.NearestRank(values, 30));
        Assert.Equal(35, Percentiles.NearestRank(values, 50));
        Assert.Equal(15, Percentiles.NearestRank(values, 0));
        Assert.Equal(50, Percentiles.NearestRank(values, 100));
        Assert.Equal(29, Percentiles.Interpolated(values, 40), 9);
        Assert.Equal(35, Percentiles.Interpolated(values, 50));
    }

    [Fact]
    public void Percentiles_QuickSelectMatchesSorting()
    {
        var random = new Random(2);
        var values = Enumerable.Range(0, 301).Select(_ => (double)random.Next(100)).ToArray();
        var sorted = values.Order().ToArray();

        for (var k = 0; k < values.Length; k += 7) Assert.Equal(sorted[k], Percentiles.QuickSelect(values, k, random));

        var latencies = Enumerable.Range(1, 1000).Select(i => (double)i).ToArray();
        Assert.Equal((500.0, 900.0, 990.0), Percentiles.Summary(latencies));
    }

    [Fact]
    public void HyperLogLog_EstimatesWithinAFewPercent()
    {
        var hll = new HyperLogLog();
        for (var i = 0; i < 100_000; i++) hll.Add($"user-{i % 50_000}"); // every user twice

        Assert.InRange(hll.Estimate(), 47_000, 53_000); // ±6%, about four standard errors
        Assert.Equal(4096, hll.SizeInBytes);
    }

    [Fact]
    public void HyperLogLog_SmallCountsAndMerging()
    {
        var a = new HyperLogLog();
        var b = new HyperLogLog();
        for (var i = 0; i < 100; i++) a.Add($"x{i}");
        for (var i = 50; i < 150; i++) b.Add($"x{i}");

        Assert.InRange(a.Estimate(), 97, 103);
        a.Merge(b);
        Assert.InRange(a.Estimate(), 145, 155);
        Assert.Equal(0, new HyperLogLog().Estimate());
    }

    [Fact]
    public void CountMin_NeverUnderestimates()
    {
        var sketch = new CountMinSketch(epsilon: 0.01);
        var random = new Random(3);
        var exact = new Dictionary<string, long>();
        for (var i = 0; i < 20_000; i++)
        {
            var key = $"k{(int)Math.Pow(random.Next(1, 1000), 1.5) % 2000}";
            sketch.Add(key);
            exact[key] = exact.GetValueOrDefault(key) + 1;
        }

        foreach (var (key, count) in exact)
        {
            var estimate = sketch.Estimate(key);
            Assert.True(estimate >= count);
            Assert.True(estimate <= count + 0.01 * sketch.Total * 3);
        }
    }

    [Fact]
    public void CountMin_FindsHeavyHitters()
    {
        var random = new Random(4);
        var stream = Enumerable.Repeat("cats", 500)
            .Concat(Enumerable.Repeat("dogs", 300))
            .Concat(Enumerable.Range(0, 2000).Select(i => $"rare-{i}"))
            .Concat(Enumerable.Repeat("birds", 200))
            .OrderBy(_ => random.Next());

        var top = CountMinSketch.TopK(stream, 3);

        Assert.Equal(["cats", "dogs", "birds"], top.Select(t => t.Key));
    }

    [Fact]
    public void Regression_FitsALine()
    {
        var fit = LinearRegression.Fit([(1, 3), (2, 5), (3, 7), (4, 9)]);

        Assert.Equal(2, fit.Slope, 12);
        Assert.Equal(1, fit.Intercept, 12);
        Assert.Equal(1, fit.RSquared, 12);
        Assert.Equal(21, fit.Predict(10), 12);
    }

    [Fact]
    public void Regression_NoisyDataAndCorrelation()
    {
        var fit = LinearRegression.Fit([(1, 2), (2, 4), (3, 5), (4, 4), (5, 5)]);

        Assert.Equal(0.6, fit.Slope, 12);
        Assert.Equal(2.2, fit.Intercept, 12);
        Assert.Equal(0.6, fit.RSquared, 12);
        Assert.True(LinearRegression.Correlation([(1, 5), (2, 3), (3, 1)]) < -0.99);
        Assert.Throws<ArgumentException>(() => LinearRegression.Fit([(1, 1), (1, 2)]));
    }

    [Fact]
    public void MovingAverages_SimpleAndExponential()
    {
        Assert.Equal([2.0, 3, 4], MovingAverages.Simple([1, 2, 3, 4, 5], 3));
        Assert.Equal([10.0, 15, 17.5], MovingAverages.Exponential([10, 20, 20], 0.5));
    }

    [Fact]
    public void DecayingAverage_HalvesTheOldWeightPerHalfLife()
    {
        var start = DateTimeOffset.UnixEpoch;
        var average = new DecayingAverage(TimeSpan.FromMinutes(1));

        average.Add(0, start);
        average.Add(100, start.AddMinutes(1));
        Assert.Equal(50, average.Value, 9);

        average.Add(100, start.AddMinutes(3)); // two half-lives: 50 keeps a quarter of its weight
        Assert.Equal(87.5, average.Value, 9);
    }
}
