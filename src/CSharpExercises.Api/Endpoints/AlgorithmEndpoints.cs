using CSharpExercises.Algorithms;
using Microsoft.AspNetCore.Http.HttpResults;
using static CSharpExercises.Api.Endpoints.Validation;

namespace CSharpExercises.Api.Endpoints;

public record SortRequest(string? Algorithm, int[]? Numbers);

public record SortResponse(string Algorithm, int Count, int[] Sorted);

public record Edge(string? From, string? To, int Weight);

public record ShortestPathRequest(Edge[]? Edges, string? From, string? To);

public record ShortestPathResponse(int Distance, IReadOnlyList<string> Path);

public record CoinChangeResponse(int Amount, int[] Coins, int? MinCoins, long Ways);

public static class AlgorithmEndpoints
{
    // Input limits keep one request from tying up the server: bubble sort is O(n²),
    // and the coin and route tables grow with the input.
    public const int MaxSortItems = 100_000;
    public const int MaxBubbleSortItems = 5_000;
    public const int MaxEdges = 10_000;
    public const int MaxAmount = 100_000;
    public const int MaxCoinTypes = 100;

    private static readonly Dictionary<string, Func<int[], int[]>> SortAlgorithms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bubble"] = Sorting.BubbleSort,
        ["merge"] = Sorting.MergeSort,
        ["quick"] = Sorting.QuickSort
    };

    public static IEndpointRouteBuilder MapSortingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/sort", Sort)
            .WithTags("Algorithms")
            .WithSummary("Sort numbers with bubble, merge or quick sort");
        return app;
    }

    public static IEndpointRouteBuilder MapRouteEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/routes/shortest", ShortestPath)
            .WithTags("Algorithms")
            .WithSummary("Cheapest route through a weighted, one-way graph (Dijkstra)");
        return app;
    }

    public static IEndpointRouteBuilder MapCoinEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/coins", CoinChange)
            .WithTags("Algorithms")
            .WithSummary("Fewest coins and number of combinations for an amount (dynamic programming)");
        return app;
    }

    private static Results<Ok<SortResponse>, ValidationProblem> Sort(SortRequest request)
    {
        if (request.Algorithm is null || !SortAlgorithms.TryGetValue(request.Algorithm, out var sort))
        {
            return Invalid("algorithm", $"Use one of: {string.Join(", ", SortAlgorithms.Keys)}.");
        }

        if (request.Numbers is null) return Invalid("numbers", "Numbers are required.");

        var limit = request.Algorithm.Equals("bubble", StringComparison.OrdinalIgnoreCase) ? MaxBubbleSortItems : MaxSortItems;
        if (request.Numbers.Length > limit) return Invalid("numbers", $"{request.Algorithm} sort accepts at most {limit:N0} numbers.");

        return TypedResults.Ok(new SortResponse(request.Algorithm.ToLowerInvariant(), request.Numbers.Length, sort(request.Numbers)));
    }

    private static Results<Ok<ShortestPathResponse>, ProblemHttpResult, ValidationProblem> ShortestPath(ShortestPathRequest request)
    {
        if (request.Edges is null || request.Edges.Length == 0) return Invalid("edges", "At least one edge is required.");
        if (request.Edges.Length > MaxEdges) return Invalid("edges", $"At most {MaxEdges:N0} edges are allowed.");
        if (request.Edges.Any(e => string.IsNullOrWhiteSpace(e.From) || string.IsNullOrWhiteSpace(e.To)))
        {
            return Invalid("edges", "Every edge needs a 'from' and a 'to'.");
        }

        if (request.Edges.Any(e => e.Weight < 0)) return Invalid("edges", "Weights must not be negative.");
        if (string.IsNullOrWhiteSpace(request.From)) return Invalid("from", "A start vertex is required.");
        if (string.IsNullOrWhiteSpace(request.To)) return Invalid("to", "A destination vertex is required.");

        var edges = request.Edges.Select(e => (e.From!, e.To!, e.Weight));
        var result = Dijkstra.ShortestPath(edges, request.From, request.To);

        return result is { } route
            ? TypedResults.Ok(new ShortestPathResponse(route.Distance, route.Path))
            : TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "No route",
                detail: $"'{request.To}' cannot be reached from '{request.From}'.");
    }

    private static Results<Ok<CoinChangeResponse>, ValidationProblem> CoinChange(int amount, int[] coins)
    {
        if (amount is < 0 or > MaxAmount) return Invalid("amount", $"Amount must be between 0 and {MaxAmount:N0}.");
        if (coins.Length is 0 or > MaxCoinTypes) return Invalid("coins", $"Give between 1 and {MaxCoinTypes} coin values.");
        if (coins.Any(c => c <= 0)) return Invalid("coins", "Coin values must be positive.");

        long ways;
        try
        {
            ways = DynamicProgramming.CountWays(coins, amount);
        }
        catch (OverflowException)
        {
            return Invalid("amount", "There are too many combinations to count; try a smaller amount or fewer coins.");
        }

        var min = DynamicProgramming.MinCoins(coins, amount);
        return TypedResults.Ok(new CoinChangeResponse(amount, coins, min < 0 ? null : min, ways));
    }
}
