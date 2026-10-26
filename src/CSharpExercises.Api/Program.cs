using CSharpExercises.Api.Endpoints;
using CSharpExercises.Api.RateLimiting;
using CSharpExercises.Patterns;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Exercises from the library, wired with dependency injection.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ShapeFactory>();
builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection(RateLimitOptions.SectionName));
builder.Services.AddSingleton<ClientRateLimiter>();

var app = builder.Build();

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    // Binding errors (e.g. a missing query parameter) surface as BadHttpRequestException in Development;
    // report them as the 400 they are rather than a 500.
    StatusCodeSelector = ex => ex is BadHttpRequestException bad ? bad.StatusCode : StatusCodes.Status500InternalServerError
});
app.UseStatusCodePages();
app.UseMiddleware<RateLimitMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => new
{
    Name = "C# Exercises API",
    Source = "https://github.com/dejanciev-dev/csharp-exercises",
    Endpoints = new[]
    {
        "POST /api/sort",
        "POST /api/routes/shortest",
        "GET  /api/coins?amount=11&coins=1&coins=2&coins=5",
        "GET  /api/text/edit-distance?source=kitten&target=sitting",
        "GET  /api/text/lcs?a=AGGTAB&b=GXTXAYB",
        "GET  /api/text/palindrome?text=racecar",
        "GET  /api/shapes",
        "POST /api/shapes",
        "GET  /health"
    }
}).ExcludeFromDescription();

app.MapGet("/health", () => TypedResults.Ok(new { Status = "ok" })).WithTags("Health");

app.MapSortingEndpoints();
app.MapRouteEndpoints();
app.MapCoinEndpoints();
app.MapTextEndpoints();
app.MapShapeEndpoints();

app.Run();

// Makes the auto-generated Program class visible to WebApplicationFactory<Program> in the integration tests.
public partial class Program;
