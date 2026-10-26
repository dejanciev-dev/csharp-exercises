using CSharpExercises.Algorithms;
using CSharpExercises.Strings;
using Microsoft.AspNetCore.Http.HttpResults;
using static CSharpExercises.Api.Endpoints.Validation;

namespace CSharpExercises.Api.Endpoints;

public record EditDistanceResponse(string Source, string Target, int Distance);

public record CommonSubsequenceResponse(string A, string B, string Subsequence, int Length);

public record PalindromeResponse(string Text, bool IsPalindrome);

public static class TextEndpoints
{
    /// <summary>Edit distance and LCS build an (a × b) table, so both inputs are capped.</summary>
    public const int MaxLength = 1_000;

    public static IEndpointRouteBuilder MapTextEndpoints(this IEndpointRouteBuilder app)
    {
        var text = app.MapGroup("/api/text").WithTags("Text");

        text.MapGet("/edit-distance", EditDistance).WithSummary("Fewest single-character edits between two strings");
        text.MapGet("/lcs", CommonSubsequence).WithSummary("Longest common subsequence of two strings");
        text.MapGet("/palindrome", Palindrome).WithSummary("Whether text reads the same backwards, ignoring case and punctuation");
        return app;
    }

    private static Results<Ok<EditDistanceResponse>, ValidationProblem> EditDistance(string source, string target)
    {
        if (source.Length > MaxLength) return TooLong(nameof(source));
        if (target.Length > MaxLength) return TooLong(nameof(target));

        return TypedResults.Ok(new EditDistanceResponse(source, target, DynamicProgramming.EditDistance(source, target)));
    }

    private static Results<Ok<CommonSubsequenceResponse>, ValidationProblem> CommonSubsequence(string a, string b)
    {
        if (a.Length > MaxLength) return TooLong(nameof(a));
        if (b.Length > MaxLength) return TooLong(nameof(b));

        var lcs = DynamicProgramming.LongestCommonSubsequence(a, b);
        return TypedResults.Ok(new CommonSubsequenceResponse(a, b, lcs, lcs.Length));
    }

    private static Results<Ok<PalindromeResponse>, ValidationProblem> Palindrome(string text)
    {
        if (text.Length > MaxLength) return TooLong(nameof(text));

        return TypedResults.Ok(new PalindromeResponse(text, StringExercises.IsPalindrome(text)));
    }

    private static ValidationProblem TooLong(string field) =>
        Invalid(field, $"At most {MaxLength:N0} characters are allowed.");
}
