using CSharpExercises.Patterns;
using Microsoft.AspNetCore.Http.HttpResults;
using static CSharpExercises.Api.Endpoints.Validation;

namespace CSharpExercises.Api.Endpoints;

public record ShapeRequest(string? Description);

public record ShapeResponse(string Type, double Area, double Perimeter);

public static class ShapeEndpoints
{
    public static IEndpointRouteBuilder MapShapeEndpoints(this IEndpointRouteBuilder app)
    {
        var shapes = app.MapGroup("/api/shapes").WithTags("Shapes");

        // The ShapeFactory comes from dependency injection (registered in Program.cs).
        shapes.MapGet("/", (ShapeFactory factory) => TypedResults.Ok(factory.Names.Order()))
            .WithSummary("Names of the shapes the factory can create");
        shapes.MapPost("/", Create)
            .WithSummary("Create a shape from text such as \"circle 2\" or \"rectangle 3 4\"");
        return app;
    }

    private static Results<Ok<ShapeResponse>, ValidationProblem> Create(ShapeRequest request, ShapeFactory factory)
    {
        if (string.IsNullOrWhiteSpace(request.Description)) return Invalid("description", "A description is required.");

        try
        {
            var shape = factory.Create(request.Description);
            return TypedResults.Ok(new ShapeResponse(
                shape.GetType().Name,
                Math.Round(shape.Area, 4),
                Math.Round(shape.Perimeter, 4)));
        }
        catch (ArgumentException ex)
        {
            // Unknown shape, wrong number of values, or a non-positive size.
            // ArgumentException appends " (Parameter 'x')" to its message, which means nothing to an API caller.
            var message = ex.ParamName is null ? ex.Message : ex.Message.Replace($" (Parameter '{ex.ParamName}')", "");
            return Invalid("description", message);
        }
    }
}
