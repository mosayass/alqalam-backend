using AlQalam.Host.Errors;
using AlQalam.Shared.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Xunit;

namespace AlQalam.Tests.Host;

public class ResultExtensionsTests
{
    [Fact]
    public void ToHttpResult_OnSuccess_ReturnsOk()
    {
        var result = Result.Success();
        var httpResult = result.ToHttpResult();

        Assert.IsType<Ok>(httpResult);
    }

    [Fact]
    public void ToHttpResult_GenericOnSuccess_ReturnsOkWithValue()
    {
        var result = Result<string>.Success("hello");
        var httpResult = result.ToHttpResult();

        var okResult = Assert.IsType<Ok<string>>(httpResult);
        Assert.Equal("hello", okResult.Value);
    }

    [Theory]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorType.Failure, StatusCodes.Status400BadRequest)]
    public void ToHttpResult_OnFailure_MapsToProblemDetailsWithCorrectStatusCode(ErrorType errorType, int expectedStatusCode)
    {
        var error = new Error("Test.Error", "A test error description", errorType);
        var result = Result.Failure(error);

        var httpResult = result.ToHttpResult();
        var problemResult = Assert.IsType<ProblemHttpResult>(httpResult);

        Assert.Equal(expectedStatusCode, problemResult.StatusCode);
        Assert.Equal("Test.Error", problemResult.ProblemDetails.Title);
        Assert.Equal("A test error description", problemResult.ProblemDetails.Detail);
    }
}
