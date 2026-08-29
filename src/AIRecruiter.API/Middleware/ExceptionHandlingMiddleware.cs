using AIRecruiter.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Middleware;

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
        catch (AppException ex)
        {
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = ex.StatusCode;

            if (ex is ExternalServiceUnavailableException { RetryAfterSeconds: not null } svcEx)
            {
                context.Response.Headers.RetryAfter = svcEx.RetryAfterSeconds!.Value.ToString();
            }

            var problem = new ProblemDetails
            {
                Status = ex.StatusCode,
                Title = ex.ErrorCode,
                Detail = ex.Message,
            };
            problem.Extensions["errorCode"] = ex.ErrorCode;

            await context.Response.WriteAsJsonAsync(problem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = 500;

            var problem = new ProblemDetails
            {
                Status = 500,
                Title = "INTERNAL_ERROR",
                Detail = "An unexpected error occurred.",
            };
            problem.Extensions["errorCode"] = "INTERNAL_ERROR";

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
