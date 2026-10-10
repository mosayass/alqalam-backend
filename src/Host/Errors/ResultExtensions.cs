using AlQalam.Shared.Results;

namespace AlQalam.Host.Errors;

public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok();
        }

        return MapErrorToProblemDetails(result.Error);
    }

    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            return onSuccess != null ? onSuccess(result.Value) : Results.Ok(result.Value);
        }

        return MapErrorToProblemDetails(result.Error);
    }

    private static IResult MapErrorToProblemDetails(Error error)
    {
        var (statusCode, type) = error.Type switch
        {
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5"),
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.10"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.2"),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.4"),
            _ => (StatusCodes.Status400BadRequest, "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1")
        };

        return Results.Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Description,
            type: type);
    }
}
