using CSharpExercises.Randomness;

namespace CSharpExercises.Tests;

public class RandomnessTests
{
    [Fact]
    public void Xoshiro_IsReproducibleAndUniform()
    {
        var a = new Xoshiro256(42);
        var b = new Xoshiro256(42);
        Assert.Equal(Enumerable.Range(0, 10).Select(_ => a.NextUInt64()), Enumerable.Range(0, 10).Select(_ => b.NextUInt64()));
        Assert.NotEqual(new Xoshiro256(1).NextUInt64(), new Xoshiro256(2).NextUInt64());

        var rng = new Xoshiro256(7);
        var counts = new int[6];
        for (var i = 0; i < 60_000; i++) counts[rng.Next(1, 7) - 1]++;
        Assert.All(counts, c => Assert.InRange(c, 9_500, 10_500));

        var doubles = Enumerable.Range(0, 10_000).Select(_ => rng.NextDouble()).ToArray();
        Assert.All(doubles, d => Assert.InRange(d, 0, 0.9999999999));
        Assert.InRange(doubles.Average(), 0.49, 0.51);
    }

    [Fact]
    public void FisherYates_IsUniformButNaiveIsNot()
    {
        var random = new Random(1);
        var fair = new Dictionary<string, int>();
        var naive = new Dictionary<string, int>();

        for (var i = 0; i < 60_000; i++)
        {
            int[] a = [1, 2, 3], b = [1, 2, 3];
            Shuffling.Shuffle<int>(a, random);
            Shuffling.NaiveShuffle<int>(b, random);
            fair[string.Concat(a)] = fair.GetValueOrDefault(string.Concat(a)) + 1;
            naive[string.Concat(b)] = naive.GetValueOrDefault(string.Concat(b)) + 1;
        }

        // 6 orders, 10,000 each when fair. The naive version's 27 swap sequences can't split evenly: 4/27 or 5/27.
        Assert.All(fair.Values, c => Assert.InRange(c, 9_500, 10_500));
        Assert.True(naive.Values.Max() - naive.Values.Min() > 1_500);
    }

    [Fact]
    public void SampleWithoutReplacement_GivesDistinctValues()
    {
        var sample = Shuffling.SampleWithoutReplacement(1_000_000, 100, new Random(2));

        Assert.Equal(100, sample.Distinct().Count());
        Assert.All(sample, v => Assert.InRange(v, 0, 999_999));
        Assert.Equal(10, Shuffling.SampleWithoutReplacement(10, 10, new Random(3)).Distinct().Count());
    }

    [Fact]
    public void Reservoir_SamplesEveryItemEqually()
    {
        var random = new Random(4);
        var counts = new int[10];
        for (var trial = 0; trial < 20_000; trial++)
            foreach (var item in ReservoirSampler<int>.SampleFrom(Enumerable.Range(0, 10), 3, random))
                counts[item]++;

        // Each item should be picked 3/10 of the time: 6,000 times.
        Assert.All(counts, c => Assert.InRange(c, 5_600, 6_400));
    }

    [Fact]
    public void WeightedReservoir_FavoursHeavyItems()
    {
        var random = new Random(5);
        var heavy = 0;
        for (var trial = 0; trial < 2_000; trial++)
        {
            var sample = WeightedReservoir.Sample([("light", 1.0), ("heavy", 9.0), ("zero", 0.0)], 1, random);
            if (sample[0] == "heavy") heavy++;
            Assert.NotEqual("zero", sample[0]);
        }
        Assert.InRange(heavy, 1_700, 1_900); // 90%
    }

    [Fact]
    public void AliasTable_MatchesTheWeights()
    {
        var table = new AliasTable<string>([("common", 70), ("rare", 25), ("legendary", 5), ("never", 0)]);
        var random = new Random(6);

        var counts = Enumerable.Range(0, 100_000).Select(_ => table.Next(random)).CountBy(x => x).ToDictionary();

        Assert.InRange(counts["common"], 69_000, 71_000);
        Assert.InRange(counts["rare"], 24_000, 26_000);
        Assert.InRange(counts["legendary"], 4_500, 5_500);
        Assert.False(counts.ContainsKey("never"));
    }

    [Fact]
    public void MonteCarlo_ConvergesOnKnownAnswers()
    {
        var random = new Random(8);

        Assert.InRange(MonteCarlo.EstimatePi(200_000, random), 3.13, 3.155);
        Assert.InRange(MonteCarlo.SharedBirthdayExact(23), 0.507, 0.508);
        Assert.InRange(MonteCarlo.SharedBirthdayChance(23, 20_000, random), 0.49, 0.525);
        Assert.InRange(MonteCarlo.MontyHallWinRate(switchDoors: true, 30_000, random), 0.65, 0.68);
        Assert.InRange(MonteCarlo.MontyHallWinRate(switchDoors: false, 30_000, random), 0.32, 0.35);
    }
}
