namespace ApiGateway.Middleware;

/// <summary>
/// Middleware that assigns a unique Correlation ID to every incoming request.
/// If the request already has an X-Correlation-ID header (forwarded from another service),
/// that value is reused. Otherwise a new GUID is generated.
/// The ID is added to the response header and to the logging scope so it appears in all logs.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string Header = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        // Reuse existing ID if present, otherwise generate a new one
        var correlationId = context.Request.Headers[Header].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        // Make it available on the response so clients can trace requests
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[Header] = correlationId;
            return Task.CompletedTask;
        });

        // Add to logging scope — appears in every log line for this request
        var logger = context.RequestServices.GetRequiredService<ILogger<CorrelationIdMiddleware>>();
        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }
}
