namespace ApiGateway.Middleware;

/// <summary>
/// Adds a unique ID to every incoming request so we can trace it across all services.
/// If the client already sends an X-Correlation-ID, we keep that same ID
/// and pass it down to AuthService, ProductService, etc.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
                         ?? Guid.NewGuid().ToString();

        // make sure YARP forwards this header to downstream services
        context.Request.Headers[HeaderName] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        var logger = context.RequestServices.GetRequiredService<ILogger<CorrelationIdMiddleware>>();
        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
            await next(context);
    }
}
