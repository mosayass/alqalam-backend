using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AlQalam.Host.Errors;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "An unhandled exception occurred while processing request {Path}",
            httpContext.Request.Path);

        if (exception is BadHttpRequestException badHttpRequestException)
        {
            var badRequestProblem = new ProblemDetails
            {
                Status = badHttpRequestException.StatusCode,
                Title = "Invalid request.",
                Detail = badHttpRequestException.Message,
                Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1",
                Instance = httpContext.Request.Path
            };

            httpContext.Response.StatusCode = badHttpRequestException.StatusCode;
            await httpContext.Response.WriteAsJsonAsync(badRequestProblem, cancellationToken);
            return true;
        }

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1",
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
