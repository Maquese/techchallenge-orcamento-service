using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Application.Models.Responses;
using Domain.Exceptions;

namespace OrcamentoService.Middleware;

 [ExcludeFromCodeCoverage]
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            await HandleExceptionAsync(context, ex, StatusCodes.Status400BadRequest, "domain_error");
        }
        catch (ArgumentException ex)
        {
            await HandleExceptionAsync(context, ex, StatusCodes.Status400BadRequest, "validation_error");
        }
        catch (InvalidOperationException ex)
        {
            await HandleExceptionAsync(context, ex, StatusCodes.Status400BadRequest, "invalid_operation");
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, StatusCodes.Status500InternalServerError, "unexpected_error");
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, int statusCode, string errorType)
    {
        var correlationId = GetCorrelationId(context);

        using (_logger.BeginScope(
            "correlation_id={CorrelationId} request_id={RequestId} trace_id={TraceId} event_type={EventType} operation={Operation} http_method={HttpMethod} http_path={HttpPath} status_code={StatusCode} error_type={ErrorType} exception_type={ExceptionType}",
            correlationId,
            context.TraceIdentifier,
            Activity.Current?.TraceId.ToString(),
            "integration_error",
            "request_pipeline",
            context.Request.Method,
            context.Request.Path.ToString(),
            statusCode,
            errorType,
            exception.GetType().Name))
        {
            _logger.LogError(
                new EventId(1002, "RequestFailure"),
                exception,
                "Falha no processamento da requisição. Método: {HttpMethod}, Path: {HttpPath}, StatusCode: {StatusCode}, ErrorType: {ErrorType}",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                errorType);
        }

        await WriteResponseAsync(context, statusCode, false, exception.Message, null);
    }

    private static string GetCorrelationId(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(correlationId)
            ? context.TraceIdentifier
            : correlationId;
    }

    private static async Task WriteResponseAsync(HttpContext context, int statusCode, bool success, string message, object? data)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new BaseResponse
        {
            Success = success,
            Message = message,
            Data = data
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}
