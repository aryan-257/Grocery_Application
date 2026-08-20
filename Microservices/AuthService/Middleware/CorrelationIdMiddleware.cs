namespace AuthService.Middleware;

/// <summary>
/// Assigns a unique correlation ID to every request.
/// If the caller already sends an <c>X-Correlation-ID</c> header, that value is reused
/// so you can trace a single request across multiple services.
/// The ID is added to both the response header and the log scope.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
                         ?? Guid.NewGuid().ToString();

        // Echo it back so clients can use it for support tickets / debugging
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
