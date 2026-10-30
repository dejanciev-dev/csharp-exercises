using System.Linq.Expressions;
using System.Net;
using CSharpExercises.Testing;

namespace CSharpExercises.Tests;

public class TestingToolsTests
{
    [Fact]
    public void Snapshot_FirstRunFailsThenApprovedOutputPasses()
    {
        var directory = Path.Combine(Path.GetTempPath(), "snapshots-" + Guid.NewGuid());
        try
        {
            var snapshots = new SnapshotTesting(directory);
            const string report = "Total: 3\nItems:\n- a\n- b\n- c";

            var first = Assert.Throws<SnapshotMismatchException>(() => snapshots.Verify(report, "report"));
            Assert.Contains("No approved snapshot", first.Message);

            snapshots.Approve("report");
            snapshots.Verify(report.Replace("\n", "\r\n"), "report"); // line endings don't matter

            var changed = Assert.Throws<SnapshotMismatchException>(() => snapshots.Verify(report.Replace("- b", "- B"), "report"));
            Assert.Contains("line 4: - - b", changed.Message);
            Assert.Contains("line 4: + - B", changed.Message);
            Assert.True(File.Exists(Path.Combine(directory, "report.received.txt")));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task FakeHttp_AnswersAndRecords()
    {
        var handler = new FakeHttpHandler().When(HttpMethod.Get, "/weather/Oslo", HttpStatusCode.OK, "-3");
        var client = new WeatherClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.test") });

        Assert.Equal("-3", await client.TemperatureAsync("Oslo"));
        Assert.Equal((HttpMethod.Get, "https://api.test/weather/Oslo", (string?)null), handler.Requests.Single());
        await Assert.ThrowsAsync<HttpRequestException>(() => client.TemperatureAsync("Atlantis")); // 404 from no rule
    }

    [Fact]
    public async Task FakeHttp_SimulatesTransientFailures()
    {
        var handler = new FakeHttpHandler().Sequence("/weather/Rome", HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
        var client = new WeatherClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.test") });

        await client.TemperatureAsync("Rome");

        Assert.Equal(3, handler.Requests.Count); // retried twice
        var giveUp = new FakeHttpHandler().Sequence("/weather/Rome", HttpStatusCode.ServiceUnavailable);
        await Assert.ThrowsAsync<HttpRequestException>(() => new WeatherClient(new HttpClient(giveUp) { BaseAddress = new Uri("https://api.test") }).TemperatureAsync("Rome"));
    }

    [Fact]
    public void FileSystem_FakeRunsTheSameCodeWithoutDisk()
    {
        var fs = new InMemoryFileSystem();
        var rotator = new LogRotator(fs, "logs", keep: 2);
        rotator.Append("2026-10-28", "a");
        rotator.Append("2026-10-29", "b");
        rotator.Append("2026-10-30", "c");
        rotator.Append("2026-10-30", "d");

        var removed = rotator.Rotate();

        Assert.Equal(["logs/2026-10-28.log"], removed);
        Assert.Equal("c\nd\n", fs.ReadAllText(@"logs\2026-10-30.log")); // separators normalised
        Assert.Throws<FileNotFoundException>(() => fs.ReadAllText("logs/2026-10-28.log"));
    }

    [Fact]
    public void FileSystem_InjectsFailures()
    {
        var fs = new InMemoryFileSystem();
        fs.Lock("logs/2026-10-30.log");

        Assert.Throws<IOException>(() => new LogRotator(fs, "logs", 2).Append("2026-10-30", "x"));
    }

    [Fact]
    public void Mutation_WeakTestsLetMutantsSurvive()
    {
        // Clamp-ish function: returns a + b, but never more than 100.
        Expression<Func<int, int, int>> capSum = (a, b) => a + b > 100 ? 100 : a + b;

        Func<Func<int, int, int>, bool>[] weak = [f => f(1, 2) == 3];
        Func<Func<int, int, int>, bool>[] strong = [f => f(1, 2) == 3, f => f(90, 20) == 100, f => f(50, 50) == 100, f => f(60, 40) == 100, f => f(0, 0) == 0];

        var weakResults = MutationTesting.Run(capSum, weak);
        var strongResults = MutationTesting.Run(capSum, strong);

        Assert.Equal(3, weakResults.Count); // two + and one >
        Assert.Contains(weakResults, m => !m.Killed && m.Description.StartsWith("GreaterThan")); // no test near the boundary
        // "> 100" → ">= 100" can never be caught: at exactly 100 both return 100. An equivalent mutant, not a missing test.
        Assert.Equal("GreaterThan", Assert.Single(strongResults, m => !m.Killed).Description.Split(" ")[0]);
        Assert.True(MutationTesting.Score(weakResults) < MutationTesting.Score(strongResults));
    }

    [Fact]
    public void Mutation_RequiresPassingTestsFirst()
    {
        Expression<Func<int, int, int>> add = (a, b) => a + b;
        Assert.Throws<InvalidOperationException>(() => MutationTesting.Run(add, [f => f(1, 1) == 3]));
    }
}
