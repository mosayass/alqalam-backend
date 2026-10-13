using AlQalam.Shared.Results;
using AlQalam.Users.Core.Abstractions;
using AlQalam.Users.Core.Commands.ConfirmEmail;
using Xunit;

namespace AlQalam.Tests.Modules.Users.Core;

public class ConfirmEmailHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithEmptyUserId_ReturnsValidation()
    {
        var fakeIdentity = new FakeIdentityService();
        var handler = new ConfirmEmailHandler(fakeIdentity);
        var command = new ConfirmEmailCommand(Guid.Empty, "some-token");

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Users.InvalidConfirmationToken", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_WithEmptyToken_ReturnsValidation(string emptyToken)
    {
        var fakeIdentity = new FakeIdentityService();
        var handler = new ConfirmEmailHandler(fakeIdentity);
        var command = new ConfirmEmailCommand(Guid.NewGuid(), emptyToken);

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Users.InvalidConfirmationToken", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_WhenValidToken_ReturnsSuccess()
    {
        var fakeIdentity = new FakeIdentityService();
        var handler = new ConfirmEmailHandler(fakeIdentity);
        var command = new ConfirmEmailCommand(Guid.NewGuid(), "valid-token");

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_WhenIdentityReturnsInvalidToken_ReturnsGenericValidation()
    {
        var fakeIdentity = new FakeIdentityService
        {
            FailureError = Error.Validation("Users.InvalidConfirmationToken", "Invalid or expired confirmation token.")
        };
        var handler = new ConfirmEmailHandler(fakeIdentity);
        var command = new ConfirmEmailCommand(Guid.NewGuid(), "bad-token");

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Users.InvalidConfirmationToken", result.Error.Code);
    }

    private sealed class FakeIdentityService : IIdentityService
    {
        public Error? FailureError { get; set; }

        public Task<Result<Guid>> CreateAccountAsync(Guid userId, string email, string password, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<Guid>.Success(userId));

        public Task<Result<string>> GenerateEmailConfirmationTokenAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<string>.Success("token"));

        public Task<Result> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default)
        {
            if (FailureError is not null)
            {
                return Task.FromResult<Result>(FailureError.Value);
            }
            return Task.FromResult(Result.Success());
        }

        public Task<Result<Guid>> CheckCredentialsAsync(string email, string password, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<Guid>.Success(Guid.NewGuid()));
    }
}
