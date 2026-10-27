using CSharpExercises.ErrorHandling;

namespace CSharpExercises.Tests;

public class ErrorHandlingTests
{
    private static readonly Error NotFound = new("not_found", "Not found.");

    [Fact]
    public void Result_MapTransformsSuccessAndSkipsFailure()
    {
        Result<int> success = 21;
        Result<int> failure = NotFound;

        Assert.Equal(42, success.Map(x => x * 2).Value);
        Assert.Equal([NotFound], failure.Map(x => x * 2).Errors);
    }

    [Fact]
    public void Result_BindStopsAtTheFirstFailure()
    {
        var secondStepRan = false;
        Result<int> failure = NotFound;

        var result = failure.Bind(x =>
        {
            secondStepRan = true;
            return Result<int>.Success(x + 1);
        });

        Assert.False(result.IsSuccess);
        Assert.False(secondStepRan);
    }

    [Fact]
    public void Result_ValueOfAFailureThrows()
    {
        Result<string> failure = NotFound;

        var ex = Assert.Throws<InvalidOperationException>(() => failure.Value);
        Assert.Contains("Not found.", ex.Message);
    }

    [Fact]
    public void Result_FailureNeedsAnError()
    {
        Assert.Throws<ArgumentException>(() => Result<int>.Failure());
    }

    [Fact]
    public void Result_MatchHandlesBothOutcomes()
    {
        static string Describe(Result<int> r) => r.Match(v => $"got {v}", errors => $"{errors.Count} error(s)");

        Assert.Equal("got 7", Describe(7));
        Assert.Equal("1 error(s)", Describe(NotFound));
    }

    [Fact]
    public void Result_TryTurnsExceptionsIntoFailures()
    {
        var result = Result.Try(() => int.Parse("abc"), ex => new Error("parse", ex.GetType().Name));

        Assert.Equal("FormatException", Assert.Single(result.Errors).Message);
    }

    [Fact]
    public void ValidateFailFast_ReturnsSignupForValidInput()
    {
        var result = SignupValidator.ValidateFailFast(" Ana@Example.com ", "30", "s3cret-pass");

        Assert.True(result.IsSuccess);
        Assert.Equal(new Signup("ana@example.com", 30, "s3cret-pass"), result.Value);
    }

    [Fact]
    public void ValidateFailFast_ReportsOnlyTheFirstError()
    {
        var result = SignupValidator.ValidateFailFast("not-an-email", "abc", "short");

        Assert.Equal("email.invalid", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public void ValidateAll_ReportsEveryInvalidField()
    {
        var result = SignupValidator.ValidateAll("", "200", "password");

        Assert.Equal(["email.required", "age.range", "password.digit"], result.Errors.Select(e => e.Code));
    }

    [Theory]
    [InlineData("12", "age.range")]
    [InlineData("twelve", "age.invalid")]
    [InlineData(null, "age.invalid")]
    public void ParseAge_ExplainsWhatIsWrong(string? input, string expectedCode)
    {
        Assert.Equal(expectedCode, Assert.Single(SignupValidator.ParseAge(input).Errors).Code);
    }

    [Theory]
    [InlineData("a@b.co", true)]
    [InlineData("@example.com", false)]
    [InlineData("a@@example.com", false)]
    [InlineData("a@example", false)]
    [InlineData("a@.com", false)]
    [InlineData("a@example.", false)]
    public void ParseEmail_AcceptsOnlyPlausibleAddresses(string input, bool valid)
    {
        Assert.Equal(valid, SignupValidator.ParseEmail(input).IsSuccess);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CrossFieldRule_RunsOnlyOnceEachFieldIsValid(bool failFast)
    {
        var validate = failFast
            ? (Func<string?, string?, string?, Result<Signup>>)SignupValidator.ValidateFailFast
            : SignupValidator.ValidateAll;

        Assert.Equal("password.contains_email",
            Assert.Single(validate("ana@example.com", "30", "ANA12345").Errors).Code);
        Assert.Equal("email.invalid",
            Assert.Single(validate("ana", "30", "ana12345").Errors).Code);
    }

    [Fact]
    public async Task PlainWhenAll_SurfacesOnlyTheFirstException()
    {
        Task<int>[] tasks = [FailAsync<int>("one"), FailAsync<int>("two")];

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Task.WhenAll(tasks));

        Assert.Equal("one", ex.Message);
    }

    [Fact]
    public async Task WhenAllOrAggregate_ReportsEveryFailure()
    {
        Task<int>[] tasks = [Task.FromResult(1), FailAsync<int>("one"), FailAsync<int>("two")];

        var ex = await Assert.ThrowsAsync<AggregateException>(() => ExceptionHandling.WhenAllOrAggregateAsync(tasks));

        Assert.Equal(["one", "two"], ex.InnerExceptions.Select(e => e.Message));
    }

    [Fact]
    public async Task WhenAllOrAggregate_KeepsASingleFailureUnwrapped()
    {
        Task<int>[] tasks = [Task.FromResult(1), FailAsync<int>("only")];

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => ExceptionHandling.WhenAllOrAggregateAsync(tasks));

        Assert.Equal("only", ex.Message);
    }

    [Fact]
    public async Task WhenAllOrAggregate_ReturnsResultsWhenNothingFails()
    {
        var results = await ExceptionHandling.WhenAllOrAggregateAsync([Task.FromResult(1), Task.FromResult(2)]);

        Assert.Equal([1, 2], results);
    }

    [Fact]
    public void RunAll_RunsEveryStepEvenAfterAFailure()
    {
        List<string> ran = [];

        var ex = Assert.Throws<AggregateException>(() => ExceptionHandling.RunAll(
            () => ran.Add("close file"),
            () => throw new IOException("disk gone"),
            () => ran.Add("release lock"),
            () => throw new TimeoutException("network")));

        Assert.Equal(["close file", "release lock"], ran);
        Assert.Collection(ex.InnerExceptions,
            e => Assert.IsType<IOException>(e),
            e => Assert.IsType<TimeoutException>(e));
    }

    [Fact]
    public void RunAll_RethrowsASingleFailureWithItsOriginalStackTrace()
    {
        var ex = Assert.Throws<IOException>(() => ExceptionHandling.RunAll(() => { }, ThrowFromHelper));

        Assert.Contains(nameof(ThrowFromHelper), ex.StackTrace);
    }

    private static void ThrowFromHelper() => throw new IOException("disk gone");

    private static async Task<T> FailAsync<T>(string message)
    {
        await Task.Yield();
        throw new InvalidOperationException(message);
    }
}
