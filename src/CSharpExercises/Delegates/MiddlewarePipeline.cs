namespace CSharpExercises.Delegates;

public sealed class RequestContext(string path)
{
    public string Path => path;
    public int StatusCode { get; set; } = 200;
    public string? Body { get; set; }
    public List<string> Log { get; } = [];
}

public delegate Task RequestHandler(RequestContext context);

/// <summary>
/// How ASP.NET Core middleware works underneath: each middleware is a function that takes the <em>next</em> handler
/// and returns a new handler wrapping it. Building the pipeline folds them together from the end backwards.
/// </summary>
public sealed class PipelineBuilder
{
    private readonly List<Func<RequestHandler, RequestHandler>> _middleware = [];

    /// <summary>Adds a middleware in its raw form: given the next handler, return a handler.</summary>
    public PipelineBuilder Use(Func<RequestHandler, RequestHandler> middleware)
    {
        _middleware.Add(middleware);
        return this;
    }

    /// <summary>The friendlier form: a function of the context and a callback to continue down the pipeline.</summary>
    public PipelineBuilder Use(Func<RequestContext, Func<Task>, Task> middleware) =>
        Use(next => context => middleware(context, () => next(context)));

    /// <summary>
    /// Composes everything into one handler. <paramref name="endpoint"/> runs last; a request that reaches the end
    /// with no endpoint gets a 404.
    /// </summary>
    public RequestHandler Build(RequestHandler? endpoint = null)
    {
        RequestHandler app = endpoint ?? (context =>
        {
            context.StatusCode = 404;
            return Task.CompletedTask;
        });

        // Wrap from the last middleware to the first, so the first one added is the outermost and runs first.
        for (var i = _middleware.Count - 1; i >= 0; i--) app = _middleware[i](app);

        return app;
    }
}

public static class Middleware
{
    /// <summary>Logs on the way in and on the way out: code after <c>next()</c> runs as the response travels back.</summary>
    public static PipelineBuilder UseLogging(this PipelineBuilder builder) =>
        builder.Use(async (context, next) =>
        {
            context.Log.Add($"-> {context.Path}");
            await next();
            context.Log.Add($"<- {context.StatusCode}");
        });

    /// <summary>Turns an exception from anywhere further down into a 500 response.</summary>
    public static PipelineBuilder UseErrorHandling(this PipelineBuilder builder) =>
        builder.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (Exception ex)
            {
                context.StatusCode = 500;
                context.Body = $"Error: {ex.Message}";
            }
        });

    /// <summary>Short-circuits: blocked paths never reach the rest of the pipeline.</summary>
    public static PipelineBuilder UseBlockList(this PipelineBuilder builder, params IReadOnlyCollection<string> blocked) =>
        builder.Use((context, next) =>
        {
            if (!blocked.Contains(context.Path)) return next();

            context.StatusCode = 403;
            return Task.CompletedTask;
        });
}
