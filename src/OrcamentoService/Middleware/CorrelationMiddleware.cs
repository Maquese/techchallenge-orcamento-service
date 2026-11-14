using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace OrcamentoService.Middleware;


[ExcludeFromCodeCoverage]
public class CorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationMiddleware> _logger;

    public CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        context.TraceIdentifier = correlationId;
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        context.Items["CorrelationId"] = correlationId;

        if (Activity.Current == null)
        {
            var activity = new Activity("AutoReparaAPI.Request");
            activity.SetTag("correlation_id", correlationId);
            activity.SetTag("http.method", context.Request.Method);
            activity.SetTag("http.path", context.Request.Path.ToString());
            activity.Start();
        }
        else
        {
            Activity.Current?.SetTag("correlation_id", correlationId);
        }

        using (_logger.BeginScope(
            "correlation_id={CorrelationId} request_id={RequestId} trace_id={TraceId} http_method={HttpMethod} http_path={HttpPath}",
            correlationId,
            context.TraceIdentifier,
            Activity.Current?.TraceId.ToString() ?? string.Empty,
            context.Request.Method,
            context.Request.Path.ToString()))
        {
            _logger.LogInformation(
                "Requisição iniciada {@RequestStarted}",
                new
                {
                    CorrelationId = correlationId,
                    RequestId = context.TraceIdentifier,
                    TraceId = Activity.Current?.TraceId.ToString() ?? string.Empty,
                    HttpMethod = context.Request.Method,
                    HttpPath = context.Request.Path.ToString()
                });
            await _next(context);
            _logger.LogInformation(
                "Requisição finalizada {@RequestFinished}",
                new
                {
                    CorrelationId = correlationId,
                    RequestId = context.TraceIdentifier,
                    TraceId = Activity.Current?.TraceId.ToString() ?? string.Empty,
                    StatusCode = context.Response.StatusCode,
                    HttpMethod = context.Request.Method,
                    HttpPath = context.Request.Path.ToString()
                });
        }
    }
}