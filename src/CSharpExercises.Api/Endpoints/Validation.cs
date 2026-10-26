using Microsoft.AspNetCore.Http.HttpResults;

namespace CSharpExercises.Api.Endpoints;

internal static class Validation
{
    /// <summary>A 400 response in the standard ProblemDetails format, naming the field that was wrong.</summary>
    public static ValidationProblem Invalid(string field, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });
}
