namespace ProductService.Middleware;

/// <summary>
/// Adds a unique ID to every request so we can trace it across services in the logs.
/// If the request already has an X-Correlation-ID header (set by the gateway), we reuse it.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
                         ?? Guid.NewGuid().ToString();

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
