using AlQalam.Shared.Results;
using Xunit;

namespace AlQalam.Tests.Shared;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_ShouldCreateFailedResult()
    {
        var error = Error.Validation("User.InvalidEmail", "The email format is invalid.");
        var result = Result.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        Assert.Equal("User.InvalidEmail", result.Error.Code);
        Assert.Equal("The email format is invalid.", result.Error.Description);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public void GenericSuccess_ShouldContainValue()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericFailure_AccessingValue_ShouldThrowInvalidOperationException()
    {
        var error = Error.NotFound("User.NotFound", "User does not exist.");
        var result = Result<string>.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void ImplicitConversion_FromValue_ShouldReturnSuccess()
    {
        Result<string> result = "test-value";

        Assert.True(result.IsSuccess);
        Assert.Equal("test-value", result.Value);
    }

    [Fact]
    public void ImplicitConversion_FromError_ShouldReturnFailure()
    {
        var error = Error.Conflict("User.EmailConflict", "Email is already in use.");
        Result<string> result = error;

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }

    [Theory]
    [InlineData(ErrorType.Failure)]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Conflict)]
    [InlineData(ErrorType.Unauthorized)]
    [InlineData(ErrorType.Forbidden)]
    public void ErrorFactories_ShouldSetCorrectErrorTypes(ErrorType errorType)
    {
        var error = errorType switch
        {
            ErrorType.Failure => Error.Failure("code", "desc"),
            ErrorType.Validation => Error.Validation("code", "desc"),
            ErrorType.NotFound => Error.NotFound("code", "desc"),
            ErrorType.Conflict => Error.Conflict("code", "desc"),
            ErrorType.Unauthorized => Error.Unauthorized("code", "desc"),
            ErrorType.Forbidden => Error.Forbidden("code", "desc"),
            _ => throw new ArgumentOutOfRangeException(nameof(errorType))
        };

        Assert.Equal(errorType, error.Type);
    }
}
